﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections.HotMethod;

/// <summary>
/// A base interface for <see cref="IHotMethod2{TKey, TValue}"/> so that all generic implementations
/// can be detected by a common base type.
/// </summary>
public interface IHotMethod2
{
}

/// <summary>
/// The contract for processing methods of some specific type.
/// </summary>
/// <typeparam name="TKey">The key to be processed.</typeparam>
/// <typeparam name="TValue">The value to be processed.</typeparam>
public interface IHotMethod2<TKey, TValue> : IHotMethod2
{
    /// <summary>
    /// Searches a tree for the specific value.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The value to search for.</param>
    /// <returns>cmp: -1 => left, 0 and leaf is not null => found, 1 => right.
    /// leaf: the node with the specific value if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a tree for the node with the specific value.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The value to search for.</param>
    /// <returns>cmp: -1 => left, 0 and leaf is not null => found, 1 => right.
    /// leaf: the node with the specific value if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<TKey, TValue>.Node? target, TKey key);

    (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    // UnorderedMap<TKey, TValue>.Node? SearchHashtable(UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);

    // (UnorderedMap<TKey, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);
}
