// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;

namespace Arc;

/// <summary>
/// Exposes version information for the entry assembly or a selected loaded assembly.
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
    /// Updates version information from the first loaded assembly whose name contains the specified text.
    /// </summary>
    /// <param name="partialAssemblyName">A case-sensitive, ordinal substring of the assembly's simple name.</param>
    /// <remarks>Leaves the current information unchanged if no matching assembly is loaded.</remarks>
    public static void SetAssembly(string partialAssemblyName)
    {
        ArgumentNullException.ThrowIfNull(partialAssemblyName);

        foreach (var x in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Assembly.GetName() is used instead of ManifestModule.Name, since the latter is not available in single-file/Native AOT apps.
            if (x.GetName().Name?.Contains(partialAssemblyName, StringComparison.Ordinal) == true)
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
            BuildVersion = version.Build;
        }

        VersionString = $"{MajorVersion}.{MinorVersion}.{BuildVersion}";
        EncodedVersion = (MajorVersion << 24) + (MinorVersion << 16) + (BuildVersion << 8);
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
    public static int BuildVersion { get; private set; }

    /// <summary>
    /// Gets the version as an integer, encoded as (Major &lt;&lt; 24) + (Minor &lt;&lt; 16) + (BuildVersion &lt;&lt; 8).
    /// </summary>
    public static int EncodedVersion { get; private set; }
}
