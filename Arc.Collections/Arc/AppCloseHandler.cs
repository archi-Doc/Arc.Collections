// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arc;

/// <summary>
/// Provides functionality to handle application close events.<br/>
/// The handler is invoked when the console window is closed, or when the process terminates (such as when exiting the Main function or receiving SIGINT).<br/>
/// Use this as an alternative to AppDomain.CurrentDomain.ProcessExit.
/// </summary>
public static partial class AppCloseHandler
{
    private const int CtrlCloseEvent = 2; // CTRL_CLOSE_EVENT

    private static readonly object SyncObject = new();
    private static readonly ConsoleEventDelegate ConsoleEventHandler = ConsoleEventCallback;

    private static Action? handler;
    private static int handlerInvoked;

    /// <summary>
    /// Registers a handler to be called when the application is closing.<br/>
    /// The handler is invoked when the console window is closed, or when the process terminates (such as when exiting the Main function or receiving SIGINT).
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
