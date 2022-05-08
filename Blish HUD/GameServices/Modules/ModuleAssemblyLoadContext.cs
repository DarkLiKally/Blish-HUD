using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Blish_HUD.Modules;

public class ModuleAssemblyLoadContext : AssemblyLoadContext {

    private static readonly Logger Logger = Logger.GetLogger<ModuleAssemblyLoadContext>();

    private readonly ModuleManager _moduleManager;

    private Assembly _moduleAssembly;

    private bool _forceAllowDependency = false;

    /// <summary>
    /// Indicates that the modules assembly has been loaded into memory.
    /// </summary>
    public bool AssemblyLoaded => _moduleAssembly != null;

    public ModuleAssemblyLoadContext(ModuleManager moduleManager) : base(isCollectible: true) {
        _moduleManager = moduleManager;
        
        Unloading += OnUnloading;
    }

    public void LoadModule(string packagePath, ModuleParameters moduleParams) {
        if (_moduleAssembly != null) {
            return;
        }

        ComposeModuleFromDataReader(packagePath, moduleParams);
    }

    private void ComposeModuleFromDataReader(string dllName, ModuleParameters parameters) {
        if (_moduleAssembly == null) {
            try {
                if (!_moduleManager.DataReader.FileExists(dllName)) {
                    Logger.Warn("Module {module} does not contain assembly DLL {dll}", _moduleManager.Manifest.GetDetailedName(), dllName);
                    return;
                }

                _moduleAssembly = LoadPackagedAssembly(dllName);

                if (_moduleAssembly == null) {
                    Logger.Warn("Module {module} failed to load assembly DLL {dll}.", _moduleManager.Manifest.GetDetailedName(), dllName);
                    return;
                }
            } catch (ReflectionTypeLoadException ex) {
                Logger.Warn(ex, "Module {module} failed to load due to a type exception. Ensure that you are using the correct version of the Module", _moduleManager.Manifest.GetDetailedName());
                return;
            } catch (BadImageFormatException ex) {
                Logger.Warn(ex, "Module {module} failed to load.  Check that it is a compiled module.", _moduleManager.Manifest.GetDetailedName());
                return;
            } catch (Exception ex) {
                Logger.Warn(ex, "Module {module} failed to load due to an unexpected error.", _moduleManager.Manifest.GetDetailedName());
                return;
            }
        }

        var catalog   = new AssemblyCatalog(_moduleAssembly);
        var container = new CompositionContainer(catalog);

        container.ComposeExportedValue("ModuleParameters", parameters);

        _forceAllowDependency = true;

        try {
            container.SatisfyImportsOnce(_moduleManager);
        } catch (CompositionException ex) {
            Logger.Warn(ex, "Module {module} failed to be composed.", _moduleManager.Manifest.GetDetailedName());
        } catch (FileNotFoundException ex) {
            Logger.Warn(ex, "Module {module} failed to load a dependency.", _moduleManager.Manifest.GetDetailedName());
        } catch (ReflectionTypeLoadException ex) {
            Logger.Warn(ex, "Module {module} failed to load because it depended on something not available in this version.  Ensure you are using the correct module and Blish HUD versions.", _moduleManager.Manifest.GetDetailedName());
        }

        _forceAllowDependency = false;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        var libHandle      = IntPtr.Zero;
        var assemblyStream = _moduleManager.DataReader.GetFileStream(unmanagedDllName);

        if (assemblyStream == null) {
            if (!NativeLibrary.TryLoad(unmanagedDllName, typeof(ModuleAssemblyLoadContext).Assembly, null, out libHandle) && _moduleAssembly != null)
                NativeLibrary.TryLoad(unmanagedDllName, _moduleAssembly, null, out libHandle);
            return libHandle;
        }

        var isNativeAssembly = !IsManagedAssembly(assemblyStream);

        if (!isNativeAssembly) {
            return libHandle;
        }

        assemblyStream.Position = 0;

        var searchPath   = Path.Combine(GameService.Module.NativeAssemblyDirectory);
        var assemblyPath = Path.Combine(searchPath, unmanagedDllName);

        if (!Directory.Exists(searchPath)) {
            // TODO checksum test 
            using var fileStream = new FileStream(assemblyPath, FileMode.Create, FileAccess.Write);

            assemblyStream.CopyTo(fileStream);
            fileStream.Flush();

        }

        NativeLibrary.TryLoad(assemblyPath, out libHandle);
        return libHandle;
    }

    protected override Assembly Load(AssemblyName name) {
        if (_moduleManager.Enabled || _forceAllowDependency) {

            string assemblyPath = $"{name.Name}.dll";

            if (!Equals(name.CultureInfo, CultureInfo.InvariantCulture)) {
                return GetResourceAssembly(name, assemblyPath);
            }

            if (!_moduleManager.DataReader.FileExists(assemblyPath))
                return null;

            Logger.Debug("Requested dependency {dependency} ({assemblyName}) was found by module {module}.", name.Name, assemblyPath, _moduleManager.Manifest.GetDetailedName());

            try {
                return LoadPackagedAssembly(assemblyPath);
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to load dependency {dependency} for {module}.", name.Name, _moduleManager.Manifest.GetDetailedName());
            }
        }

        return null;
    }

    private void OnUnloading(AssemblyLoadContext obj) {
        Unloading -= OnUnloading;
        _moduleAssembly = null;
        
        // TODO do we want to delete all dlls on unload we've exported?
        
        var searchPath   = Path.Combine(GameService.Module.NativeAssemblyDirectory);

        foreach (string file in Directory.EnumerateFiles(searchPath)) {
            File.Delete(file);
        }
    }

    private Assembly LoadPackagedAssembly(string assemblyPath) {
        string symbolsPath = assemblyPath.Replace(".dll", ".pdb");

        var assemblyStream = _moduleManager.DataReader.GetFileStream(assemblyPath);
        var symbolStream   = _moduleManager.DataReader.GetFileStream(symbolsPath);

        return LoadFromStream(assemblyStream, symbolStream);
    }

    private Assembly GetResourceAssembly(AssemblyName resourceDetails, string assemblyPath) {
        // English is default — ignore it
        if (!string.Equals(resourceDetails.CultureInfo?.TwoLetterISOLanguageName, "en")) {
            // Non-English resource to be loaded
            assemblyPath = $"{resourceDetails.CultureName}/{assemblyPath}";

            if (_moduleManager.DataReader.FileExists(assemblyPath)) {
                try {
                    return LoadPackagedAssembly(assemblyPath);
                } catch (Exception ex) {
                    Logger.Debug(ex, "Failed to load resource {dependency} for {module}.", resourceDetails.FullName, _moduleManager.Manifest.GetDetailedName());
                }
            } else {
                Logger.Debug("Resource assembly {dependency} for {module} could not be found.", resourceDetails.FullName, _moduleManager.Manifest.GetDetailedName());
            }
        }

        return null;
    }

    private static bool IsManagedAssembly(Stream fileStream) {
        using var binaryReader = new BinaryReader(fileStream);

        if (fileStream.Length < 64) {
            return false;
        }

        //PE Header starts @ 0x3C (60). Its a 4 byte header.
        fileStream.Position = 0x3C;
        uint peHeaderPointer = binaryReader.ReadUInt32();

        if (peHeaderPointer == 0) {
            peHeaderPointer = 0x80;
        }

        // Ensure there is at least enough room for the following structures:
        //     24 byte PE Signature & Header
        //     28 byte Standard Fields         (24 bytes for PE32+)
        //     68 byte NT Fields               (88 bytes for PE32+)
        // >= 128 byte Data Dictionary Table
        if (peHeaderPointer > fileStream.Length - 256) {
            return false;
        }

        // Check the PE signature.  Should equal 'PE\0\0'.
        fileStream.Position = peHeaderPointer;
        uint peHeaderSignature = binaryReader.ReadUInt32();

        if (peHeaderSignature != 0x00004550) {
            return false;
        }

        // skip over the PEHeader fields
        fileStream.Position += 20;

        const ushort PE32     = 0x10b;
        const ushort PE32Plus = 0x20b;

        // Read PE magic number from Standard Fields to determine format.
        var peFormat = binaryReader.ReadUInt16();

        if (peFormat != PE32 && peFormat != PE32Plus) {
            return false;
        }

        // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
        // When this is non-zero then the file contains CLI data otherwise not.
        ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
        fileStream.Position = dataDictionaryStart;

        uint cliHeaderRva = binaryReader.ReadUInt32();

        if (cliHeaderRva == 0) {
            return false;
        }

        return true;
    }

}