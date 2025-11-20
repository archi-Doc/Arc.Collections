// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Arc;

/// <summary>
/// Provides functionality to handle application close events.<br/>
/// The handler is invoked when the console window is closed, or when the process terminates (such as when exiting the Main function or receiving SIGINT).<br/>
/// Use this as an alternative to AppDomain.CurrentDomain.ProcessExit.
/// </summary>
public static class AppCloseHandler
{
    /// <summary>
    /// Registers a handler to be called when the application is closing.<br/>
    /// The handler is invoked when the console window is closed, or when the process terminates (such as when exiting the Main function or receiving SIGINT).
    /// </summary>
    /// <param name="closeEventHandler">The action to execute when a close event occurs.</param>
    public static void Set(Action closeEventHandler)
    {
        try
        {
            if (handler is null)
            {
                handler = closeEventHandler;
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    handler();
                };

                SetConsoleCtrlHandler(ConsoleEventCallback, true);
            }
        }
        catch
        {
        }
    }

    private static Action? handler;

    private static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == 2)
        {
            if (handler is not null)
            {
                handler();
            }

            return true;
        }

        return false;
    }

    private delegate bool ConsoleEventDelegate(int eventType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
}
