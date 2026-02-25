// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;

namespace Arc;

/// <summary>
/// Provides helper methods and properties for retrieving and managing assembly version information.
/// </summary>
public static class VersionHelper
{
    static VersionHelper()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is not null)
        {
            Update(assembly);
        }
    }

    /// <summary>
    /// Sets the assembly to retrieve version information from, based on the specified assembly name.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to search for.</param>
    public static void SetAssembly(string assemblyName)
    {
        foreach (var x in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (x.ManifestModule.Name.Contains(assemblyName))
            {
                Update(x);
                break;
            }
        }
    }

    private static void Update(Assembly assembly)
    {
        var version = assembly.GetName()?.Version;
        if (version is not null)
        {
            MajorVersion = version.Major;
            MinorVersion = version.Minor;
            Build = version.Build;
        }

        VersionString = $"{MajorVersion}.{MinorVersion}.{Build}";
        VersionInt = (MajorVersion << 24) + (MinorVersion << 16) + (Build << 8);
    }

    /// <summary>
    /// Gets the version string in the format "Major.Minor.Build".
    /// </summary>
    public static string VersionString { get; private set; } = "0.0.0";

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public static int MajorVersion { get; private set; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public static int MinorVersion { get; private set; }

    /// <summary>
    /// Gets the build number.
    /// </summary>
    public static int Build { get; private set; }

    /// <summary>
    /// Gets the version as an integer, encoded as (Major &lt;&lt; 24) + (Minor &lt;&lt; 16) + (Build &lt;&lt; 8).
    /// </summary>
    public static int VersionInt { get; private set; }
}
