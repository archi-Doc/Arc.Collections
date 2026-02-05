// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arc.Collections;

/// <summary>
///  A thread-safe bounded circular queue.<br/>
///  While it can only perform simple <see cref="TryEnqueue(T)"/> and <see cref="TryDequeue(out T)"/> operations <br/>
///  and has restrictions such as the queue capacity being a power of 2, <br/>
///  it processes slightly faster than <see cref="System.Collections.Concurrent.ConcurrentQueue{T}"/> with a bounded limit.<br/>
///  Use it for caching and similar purposes.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
public sealed class CircularQueue<T>
{
    private const int MinimumCapacity = 1;
    private readonly Slot[] slotArray;
    private readonly int slotsMask;
    private PaddedHeadAndTail headAndTail;

    /// <summary>Initializes a new instance of the <see cref="CircularQueue{T}"/> class.</summary>
    /// <param name="capacity">The maximum number of elements the queue can contain (rounded up to the power of 2).</param>
    public CircularQueue(int capacity)
    {
        if (capacity < MinimumCapacity)
        {
            capacity = MinimumCapacity;
        }
        else
        {
            capacity = 1 << (32 - BitOperations.LeadingZeroCount((uint)capacity - 1));
        }

        this.slotArray = new Slot[capacity];
        this.slotsMask = capacity - 1;
        for (var i = 0; i < this.slotArray.Length; i++)
        {
            this.slotArray[i].SequenceNumber = i;
        }
    }

    /// <summary>Gets the number of elements this queue can store.</summary>
    public int Capacity => this.slotArray.Length;

    /// <summary>
    /// Tries to dequeue an element from the circular queue.
    /// </summary>
    /// <param name="item">The dequeued item, if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if an item was successfully dequeued; otherwise, <see langword="false"/>.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        var array = this.slotArray;
        while (true)
        {
            var currentHead = Volatile.Read(ref this.headAndTail.Head);
            var slotsIndex = currentHead & this.slotsMask;
            var sequenceNumber = Volatile.Read(ref array[slotsIndex].SequenceNumber);
            var diff = sequenceNumber - (currentHead + 1);
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref this.headAndTail.Head, currentHead + 1, currentHead) == currentHead)
                {
                    item = array[slotsIndex].Item!;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        array[slotsIndex].Item = default;
                    }

                    Volatile.Write(ref array[slotsIndex].SequenceNumber, currentHead + array.Length);
                    return true;
                }
            }
            else if (diff < 0)
            {
                int currentTail = Volatile.Read(ref this.headAndTail.Tail);
                if (currentTail - currentHead <= 0)
                {
                    item = default;
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Tries to enqueue an element into the queue.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully enqueued; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryEnqueue(T item)
    {
        var array = this.slotArray;
        while (true)
        {
            var currentTail = Volatile.Read(ref this.headAndTail.Tail);
            var slotsIndex = currentTail & this.slotsMask;
            var sequenceNumber = Volatile.Read(ref array[slotsIndex].SequenceNumber);
            var diff = sequenceNumber - currentTail;
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref this.headAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                {
                    array[slotsIndex].Item = item;
                    Volatile.Write(ref array[slotsIndex].SequenceNumber, currentTail + 1);
                    return true;
                }
            }
            else if (diff < 0)
            {
                return false;
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private struct Slot
    {
        public T? Item;
        public int SequenceNumber;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 3 * CacheLineSize)] // padding before/between/after fields
internal struct PaddedHeadAndTail
{
#if TARGET_ARM64
    public const int CacheLineSize = 128;
#else
    public const int CacheLineSize = 64;
#endif

    [FieldOffset(1 * CacheLineSize)]
    public int Head;

    [FieldOffset(2 * CacheLineSize)]
    public int Tail;
}
