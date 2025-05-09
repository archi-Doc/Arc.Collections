// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc;

/// <summary>
/// Defines a contract for retrieving conversion options.
/// </summary>
public interface IConversionOptions
{
    /// <summary>
    /// Retrieves a conversion option of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the option to retrieve.</typeparam>
    /// <returns>
    /// The option of the specified type, or <c>null</c> if an option of the specified type is not found.
    /// </returns>
    T? GetOption<T>();
}
