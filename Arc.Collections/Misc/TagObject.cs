// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Arc.Collections;

/// <summary>
/// Provides cached object instances for integer tags from 0 through 255.
/// </summary>
/// <remarks>
/// <see cref="FromTag(int)" /> reuses one instance per tag, avoiding boxing.
/// </remarks>
public sealed class TagObject
{
    /// <summary>
    /// Gets the exclusive upper bound of supported tag values.<br/>
    /// Valid tags are in the range <c>0</c> to <c>255</c>.
    /// </summary>
    public const int MaxTag = 256;

    /// <summary>
    /// Represents an invalid or unresolved tag value.
    /// </summary>
    public const int InvalidTag = -1;

    private static readonly TagObject[] TagObjects;

    static TagObject()
    {
        TagObjects = new TagObject[MaxTag];
        for (var i = 0; i < MaxTag; i++)
        {
            TagObjects[i] = new(i);
        }
    }

    /// <summary>
    /// Gets the cached <see cref="TagObject"/> instance for the specified tag.
    /// </summary>
    /// <param name="tag">The tag value to resolve.</param>
    /// <returns>The cached object associated with <paramref name="tag"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagObject FromTag(int tag)
        => TagObjects[tag];

    /// <summary>
    /// Extracts the tag value from an object created by <see cref="FromTag(int)"/>.
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <returns>
    /// The embedded tag value when <paramref name="obj"/> is a <see cref="TagObject"/>;
    /// otherwise, <c>-1</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToTag(object? obj)
        => obj is TagObject tagObject ? tagObject.Tag : InvalidTag;

    /// <summary>
    /// Gets the tag value represented by this cached instance.
    /// </summary>
    public int Tag { get; }

    private TagObject(int tag)
    {
        this.Tag = tag;
    }
}
