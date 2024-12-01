// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc;

/// <summary>
/// Provides a set of base constants used throughout the application.
/// </summary>
public class BaseConstants
{
    /// <summary>
    /// The threshold value that determines whether to use <see langword="stackalloc"/> or <see cref="System.Buffers.ArrayPool{T}"/> when allocating a span.
    /// </summary>
    public const int StackallocThreshold = 1024;
}
