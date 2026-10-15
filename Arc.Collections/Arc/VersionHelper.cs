// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;

namespace Arc;

/// <summary>
/// Exposes version information for the entry assembly or a selected loaded assembly.
/// </summary>
/// <remarks>
/// Values are read from the entry assembly on first use and stay 0.0.0 if it is unavailable.
/// Updates are not synchronized; call <see cref="SetAssembly(string)"/> during startup.
/// </remarks>
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
    /// <exception cref="ArgumentNullException"><paramref name="partialAssemblyName"/> is <see langword="null"/>.</exception>
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
        var version = assembly.GetName().Version;
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
    /// Gets the version as an integer, encoded as (Major &lt;&lt; 24) + (Minor &lt;&lt; 16) + (Build &lt;&lt; 8).
    /// Each component should be below 256, and Major below 128. Larger Minor or Build values overlap the next field,
    /// Major values from 128 to 255 make the result negative, and Major bits above the lowest 8 are lost.
    /// </summary>
    public static int EncodedVersion { get; private set; }
}
