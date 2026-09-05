// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arc;

/// <summary>
/// Invokes a handler once on process exit or a Windows console close event.
/// </summary>
/// <remarks>
/// Uses <see cref="AppDomain.ProcessExit" /> and, on Windows, CTRL_CLOSE_EVENT.
/// Only the first registered handler is used.
/// </remarks>
public static partial class AppCloseHandler
{
    private const int CtrlCloseEvent = 2; // CTRL_CLOSE_EVENT

    private static readonly object SyncObject = new();
    private static readonly ConsoleEventDelegate ConsoleEventHandler = ConsoleEventCallback;

    private static Action? handler;
    private static int handlerInvoked;

    /// <summary>
    /// Registers a handler for process exit and, on Windows, console close events.
    /// </summary>
    /// <param name="closeEventHandler">The action to execute when a close event occurs.</param>
    /// <remarks>Only the first handler is registered; subsequent calls are ignored.
    /// The handler is invoked at most once.</remarks>
    public static void Set(Action closeEventHandler)
    {
        ArgumentNullException.ThrowIfNull(closeEventHandler);

        lock (SyncObject)
        {
            if (handler is not null)
            {
                return;
            }

            handler = closeEventHandler;
            AppDomain.CurrentDomain.ProcessExit += ProcessExitCallback;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    _ = SetConsoleCtrlHandler(ConsoleEventHandler, true);
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
            }
        }
    }

    private static void ProcessExitCallback(object? sender, EventArgs e)
        => InvokeHandler();

    private static void InvokeHandler()
    {
        if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
        {
            Volatile.Read(ref handler)?.Invoke();
        }
    }

    private static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == CtrlCloseEvent)
        {
            InvokeHandler();
            return true;
        }

        return false;
    }

    private delegate bool ConsoleEventDelegate(int eventType);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, [MarshalAs(UnmanagedType.Bool)] bool add);
}
