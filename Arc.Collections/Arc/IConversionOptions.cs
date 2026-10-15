// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc;

/// <summary>
/// Provides typed options for parsing and formatting.
/// </summary>
public interface IConversionOptions
{
    /// <summary>
    /// Retrieves a conversion option of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the option to retrieve.</typeparam>
    /// <returns>
    /// The option of the specified type, or <see langword="null"/> if none is found.
    /// </returns>
    T? GetOption<T>()
        where T : class;
}
