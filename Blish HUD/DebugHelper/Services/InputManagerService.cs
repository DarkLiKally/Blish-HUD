﻿using System;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Blish_HUD.DebugHelper.Models;
using TTimer = System.Timers.Timer;
using WFTimer = System.Windows.Forms.Timer;

namespace Blish_HUD.DebugHelper.Services; 

internal class InputManagerService : IDebugService, IDisposable {

    private const int PING_TIMEOUT_BEFORE_PAUSING_HOOKS = 50;

    private readonly IMessageService     messageService;
    private readonly MouseHookService    mouseHookService;
    private readonly KeyboardHookService keyboardHookService;
    private readonly TTimer              timeoutTimer = new(PING_TIMEOUT_BEFORE_PAUSING_HOOKS) { AutoReset = false };
    private          bool                stopRequested;
    private          bool                hookRequested;
    private          Thread?             thread;

    public InputManagerService(IMessageService messageService, MouseHookService mouseHookService, KeyboardHookService keyboardHookService) {
        this.messageService      =  messageService;
        this.mouseHookService    =  mouseHookService;
        this.keyboardHookService =  keyboardHookService;
        timeoutTimer.Elapsed     += HandleTimeout;
    }

    public void Start() {
        if (thread != null) return;

        messageService.Register<PingMessage>(HandlePing);
        timeoutTimer.Start();

        thread = new Thread(Loop);
        thread.Start();
    }

    public void Stop() {
        if (thread == null) return;

        timeoutTimer.Stop();
        messageService.Unregister<PingMessage>();

        stopRequested = true;
        thread.Join();

        stopRequested = false;
        thread        = null;
    }

    private void Loop() {
        using var timer = new WFTimer {
            Interval = 10
        };

        timer.Tick += (_, _) => {
            if (stopRequested) Application.ExitThread();
            if (!hookRequested) return;

            mouseHookService.Start();
            keyboardHookService.Start();
            hookRequested = false;
        };

        timer.Start();

        // Start the message loop
        Application.Run();
    }

    private void HandlePing(PingMessage message) {
        timeoutTimer.Stop();
        timeoutTimer.Start();
        hookRequested = true;
    }

    private void HandleTimeout(object sender, ElapsedEventArgs e) {
        mouseHookService.Stop();
        keyboardHookService.Stop();
    }

    #region IDisposable Support

    private bool isDisposed; // To detect redundant calls

    protected virtual void Dispose(bool isDisposing) {
        if (isDisposed) return;

        if (isDisposing) {
            Stop();
            timeoutTimer.Dispose();
        }

        isDisposed = true;
    }

    public void Dispose() { Dispose(true); }

    #endregion

}