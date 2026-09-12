// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections.HotMethod;

/// <summary>
/// Marks specialized tree-search implementations.
/// </summary>
public interface IHotTreeMethod
{
}

/// <summary>
/// Defines specialized searches in ordered map trees.
/// </summary>
/// <typeparam name="TKey">The key to be processed.</typeparam>
/// <typeparam name="TValue">The value to be processed.</typeparam>
public interface IHotTreeMethod<TKey, TValue> : IHotTreeMethod
{
    /// <summary>
    /// Searches a tree for the specified key.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>Comparison: -1 => left, 0 and Node is not null => found, 1 => right.
    /// Node: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Comparison, OrderedMap<TKey, TValue>.Node? Node) SearchNode(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>Comparison: -1 => left, 0 and Node is not null => found, 1 => right.
    /// Node: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Comparison, OrderedMultiMap<TKey, TValue>.Node? Node) SearchNode(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>Comparison: -1 =&gt; left, 0 and Node is not null =&gt; found, 1 =&gt; right.<br/>
    /// Node: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Comparison, OrderedMap<TKey, TValue>.Node? Node) SearchNodeReverse(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>Comparison: -1 =&gt; left, 0 and Node is not null =&gt; found, 1 =&gt; right.<br/>
    /// Node: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Comparison, OrderedMultiMap<TKey, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    // UnorderedMap<TKey, TValue>.Node? SearchHashtable(UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);

    // (UnorderedMap<TKey, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);
}
