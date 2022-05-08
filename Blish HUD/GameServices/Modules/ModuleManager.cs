using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Blish_HUD.Content;

namespace Blish_HUD.Modules {

    public class ModuleManager : IDisposable {

        private static readonly Logger Logger = Logger.GetLogger<ModuleManager>();

        private static readonly List<string>              _dirtyNamespaces = new();
        private        ModuleAssemblyLoadContext _assemblyLoadContext;

        public event EventHandler<EventArgs> ModuleEnabled;
        public event EventHandler<EventArgs> ModuleDisabled;
        
        /// <summary>
        /// Used to indicate if a different version of the assembly has previously
        /// been loaded preventing us from loading another of a different version.
        /// </summary>
        public bool IsModuleAssemblyStateDirty { get; private set; }
        
        /// <summary>
        /// Indicates if the module is currently enabled.
        /// </summary>
        public bool Enabled { get; private set; }

        public bool DependenciesMet =>
            State.IgnoreDependencies
         || Manifest.Dependencies.TrueForAll(d => d.GetDependencyDetails().CheckResult == ModuleDependencyCheckResult.Available);

        public Manifest Manifest { get; }

        public ModuleState State { get; }

        public IDataReader DataReader { get; }

        [Import]
        public Module ModuleInstance { get; internal set; }

        public ModuleManager(Manifest manifest, ModuleState state, IDataReader dataReader) {
            this.Manifest   = manifest;
            this.State      = state;
            DataReader = dataReader;

            if (_dirtyNamespaces.Contains(this.Manifest.Namespace)) {
                this.IsModuleAssemblyStateDirty = true;
            }
        }

        public bool TryEnable() {
            if (this.Enabled                                             // We're already enabled.
             || this.IsModuleAssemblyStateDirty                          // User updated the module after the old assembly had already been enabled.
             || GameService.Module.ModuleIsExplicitlyIncompatible(this)) // Module is on the explicit "incompatibile" list.
                return false;

            var moduleParams = ModuleParameters.BuildFromManifest(this.Manifest, this);

            if (moduleParams != null) {
                string packagePath = this.Manifest.Package.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                         ? this.Manifest.Package
                                         : $"{this.Manifest.Package}.dll";

                if (DataReader.FileExists(packagePath)) {
                    _assemblyLoadContext = new ModuleAssemblyLoadContext(this);
                    _assemblyLoadContext.LoadModule(packagePath, moduleParams);

                    if (this.ModuleInstance != null) {
                        if (!_dirtyNamespaces.Contains(this.Manifest.Namespace)) {
                            _dirtyNamespaces.Add(this.Manifest.Namespace);
                        }

                        this.Enabled = true;

                        try {
                            this.ModuleInstance.DoInitialize();
                            this.ModuleInstance.DoLoad();

                            this.ModuleEnabled?.Invoke(this, EventArgs.Empty);
                        } catch (TypeLoadException ex) {
                            this.ModuleInstance = null;
                            this.Enabled        = false;
                            Logger.Error(ex, "Module {module} failed to load because it depended on a type which is not available in this version.  Ensure you are using the correct module and Blish HUD versions.", this.Manifest.GetDetailedName());
                        } catch (Exception ex) {
                            this.ModuleInstance = null;
                            this.Enabled        = false;
                            Logger.Error(ex, "Module {module} failed to load because of an unexpected error.", this.Manifest.GetDetailedName());
                        }
                    }
                } else {
                    Logger.Error($"Assembly '{packagePath}' could not be loaded from {DataReader.GetType().Name}.");
                }
            }

            this.State.Enabled = this.Enabled;
            GameService.Settings.Save();

            return this.Enabled;
        }

        public void Disable() {
            if (!this.Enabled) return;

            this.Enabled = false;

            try {
                this.ModuleInstance?.Dispose();
            } catch (Exception ex) {
                Logger.GetLogger(this.ModuleInstance != null ? this.ModuleInstance.GetType() : typeof(ModuleManager)).Error(ex, "Module {module} threw an exception while unloading.", this.Manifest.GetDetailedName());
                
                if (ApplicationSettings.Instance.DebugEnabled) {
                    // To assist in debugging modules
                    throw;
                }
            }

            this.ModuleInstance = null;
            
            this.ModuleDisabled?.Invoke(this, EventArgs.Empty);

            this.State.Enabled = this.Enabled;
            GameService.Settings.Save();
            
            _assemblyLoadContext.Unload();
            _assemblyLoadContext = null;
        }

        public void DeleteModule() {
            Disable();
            GameService.Module.UnregisterModule(this);
            DataReader.DeleteRoot();
        }

        public void Dispose() {
            Disable();

            GameService.Module.UnregisterModule(this);

            DataReader?.Dispose();
        }

    }

}