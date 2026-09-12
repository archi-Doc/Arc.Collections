// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections.HotMethod;

/// <summary>
/// Marks specialized tree-search implementations.
/// </summary>
public interface IHotMethod2
{
}

/// <summary>
/// Defines specialized searches in ordered map trees.
/// </summary>
/// <typeparam name="TKey">The key to be processed.</typeparam>
/// <typeparam name="TValue">The value to be processed.</typeparam>
public interface IHotMethod2<TKey, TValue> : IHotMethod2
{
    /// <summary>
    /// Searches a tree for the specified key.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>cmp: -1 => left, 0 and leaf is not null => found, 1 => right.
    /// leaf: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The node to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>cmp: -1 => left, 0 and leaf is not null => found, 1 => right.
    /// leaf: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>cmp: -1 =&gt; left, 0 and leaf is not null =&gt; found, 1 =&gt; right.<br/>
    /// leaf: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>cmp: -1 =&gt; left, 0 and leaf is not null =&gt; found, 1 =&gt; right.<br/>
    /// leaf: the node with the specified key if found, or the nearest parent node if not found.</returns>
    (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    // UnorderedMap<TKey, TValue>.Node? SearchHashtable(UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);

    // (UnorderedMap<TKey, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);
}
