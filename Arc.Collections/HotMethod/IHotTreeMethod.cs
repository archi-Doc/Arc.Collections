// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections.HotMethod;

/// <summary>
/// Marks specialized tree-search implementations.
/// </summary>
public interface IHotTreeMethod
{
}

/// <summary>
/// Defines specialized key searches in the red-black trees of <see cref="OrderedMap{TKey, TValue}"/>
/// and <see cref="OrderedMultiMap{TKey, TValue}"/>.<br/>
/// Key comparisons match <see cref="System.Collections.Generic.Comparer{T}.Default"/> exactly.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public interface IHotTreeMethod<TKey, TValue> : IHotTreeMethod
{
    /// <summary>
    /// Searches an ascending tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search, or <see langword="null"/> for an empty tree.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// (0, <see langword="null"/>) if <paramref name="target"/> is <see langword="null"/>;
    /// (0, node) if a node with <paramref name="key"/> is found;
    /// otherwise, (-1 or 1, the last node visited), where -1 or 1 means that <paramref name="key"/>
    /// would be inserted as the left or right child of that node.
    /// </returns>
    (int Comparison, OrderedMap<TKey, TValue>.Node? Node) SearchNode(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches an ascending tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search, or <see langword="null"/> for an empty tree.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// (0, <see langword="null"/>) if <paramref name="target"/> is <see langword="null"/>;
    /// (0, node) if a node with <paramref name="key"/> is found;
    /// otherwise, (-1 or 1, the last node visited), where -1 or 1 means that <paramref name="key"/>
    /// would be inserted as the left or right child of that node.
    /// </returns>
    (int Comparison, OrderedMultiMap<TKey, TValue>.Node? Node) SearchNode(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered (descending) tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search, or <see langword="null"/> for an empty tree.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// (0, <see langword="null"/>) if <paramref name="target"/> is <see langword="null"/>;
    /// (0, node) if a node with <paramref name="key"/> is found;
    /// otherwise, (-1 or 1, the last node visited), where -1 or 1 means that <paramref name="key"/>
    /// would be inserted as the left or right child of that node.
    /// </returns>
    (int Comparison, OrderedMap<TKey, TValue>.Node? Node) SearchNodeReverse(OrderedMap<TKey, TValue>.Node? target, TKey key);

    /// <summary>
    /// Searches a reverse-ordered (descending) tree for the node with the specified key.
    /// </summary>
    /// <param name="target">The subtree root to search, or <see langword="null"/> for an empty tree.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// (0, <see langword="null"/>) if <paramref name="target"/> is <see langword="null"/>;
    /// (0, node) if a node with <paramref name="key"/> is found;
    /// otherwise, (-1 or 1, the last node visited), where -1 or 1 means that <paramref name="key"/>
    /// would be inserted as the left or right child of that node.
    /// </returns>
    (int Comparison, OrderedMultiMap<TKey, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<TKey, TValue>.Node? target, TKey key);

    // UnorderedMap<TKey, TValue>.Node? SearchHashtable(UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);

    // (UnorderedMap<TKey, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key);
}
