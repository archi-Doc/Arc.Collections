// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arc.Collections;

/// <summary>
/// Provides a thread-safe, bounded queue for multiple producers and consumers.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
/// <remarks>
/// Capacity is rounded up to a power of two, between two and <see cref="MaximumCapacity"/>.
/// Enqueue attempts can fail when the queue is full; concurrent operations may retry or wait.
/// <see cref="Count" /> is an estimate during concurrent access.
/// </remarks>
public sealed class CircularQueue<T>
{
    /// <summary>
    /// The largest capacity the queue can be created with.
    /// </summary>
    public const int MaximumCapacity = 1 << 30;

    private readonly Slot[] slotArray;
    private readonly int slotsMask;
    private PaddedHeadAndTail headAndTail;

    /// <summary>Initializes a new instance of the <see cref="CircularQueue{T}"/> class.</summary>
    /// <param name="capacity">The requested capacity, rounded up to a power of two
    /// and clamped between 2 and <see cref="MaximumCapacity"/>.</param>
    public CircularQueue(int capacity)
    {
        if (capacity < 2)
        {
            // A single slot cannot distinguish a published item from the next enqueue lap.
            capacity = 2;
        }
        else if (capacity >= MaximumCapacity)
        {
            capacity = MaximumCapacity;
        }
        else
        {
            capacity = 1 << (32 - BitOperations.LeadingZeroCount((uint)capacity - 1));
        }

        var array = new Slot[capacity];
        for (var i = 0; i < array.Length; i++)
        {
            array[i].SequenceNumber = i;
        }

        this.slotArray = array;
        this.slotsMask = capacity - 1;
    }

    /// <summary>Gets the number of elements this queue can store.</summary>
    public int Capacity => this.slotArray.Length;

    /// <summary>
    /// Gets an approximate number of elements currently contained in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            var head = Volatile.Read(ref this.headAndTail.Head);
            var tail = Volatile.Read(ref this.headAndTail.Tail);
            var count = unchecked(tail - head);

            if ((uint)count > (uint)this.Capacity)
            {
                return count < 0 ? 0 : this.Capacity;
            }

            return count;
        }
    }

    /// <summary>
    /// Tries to dequeue an element from the circular queue.
    /// </summary>
    /// <param name="item">The dequeued item, if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if an item was successfully dequeued; otherwise, <see langword="false"/>.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        var array = this.slotArray;
        var currentHead = Volatile.Read(ref this.headAndTail.Head);
        while (true)
        {
            var slotsIndex = currentHead & this.slotsMask;
            var sequenceNumber = Volatile.Read(ref array[slotsIndex].SequenceNumber);
            var diff = unchecked(sequenceNumber - (currentHead + 1));
            if (diff == 0)
            {
                var witnessed = Interlocked.CompareExchange(ref this.headAndTail.Head, unchecked(currentHead + 1), currentHead);
                if (witnessed == currentHead)
                {// The slot is now owned by this thread.
                    item = array[slotsIndex].Item!;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        array[slotsIndex].Item = default;
                    }

                    // Release the slot for the next enqueue lap.
                    Volatile.Write(ref array[slotsIndex].SequenceNumber, unchecked(currentHead + array.Length));
                    return true;
                }

                // CAS failed: its return value is the freshest head, so reuse it instead of re-reading.
                currentHead = witnessed;
                continue;
            }
            else if (diff < 0)
            {// The slot appears empty; confirm against the tail to distinguish "empty" from "an enqueuer is mid-publish".
                var currentTail = Volatile.Read(ref this.headAndTail.Tail);
                if (unchecked(currentTail - currentHead) <= 0)
                {
                    item = default;
                    return false;
                }
            }

            currentHead = Volatile.Read(ref this.headAndTail.Head);
        }
    }

    /// <summary>
    /// Tries to enqueue an element into the queue.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully enqueued; otherwise, <see langword="false"/> (the queue is full,
    /// or a concurrent dequeue of the target slot is still in flight).
    /// </returns>
    public bool TryEnqueue(T item)
    {
        var array = this.slotArray;
        var currentTail = Volatile.Read(ref this.headAndTail.Tail);
        while (true)
        {
            var slotsIndex = currentTail & this.slotsMask;
            var sequenceNumber = Volatile.Read(ref array[slotsIndex].SequenceNumber);
            var diff = unchecked(sequenceNumber - currentTail);
            if (diff == 0)
            {
                var witnessed = Interlocked.CompareExchange(ref this.headAndTail.Tail, unchecked(currentTail + 1), currentTail);
                if (witnessed == currentTail)
                {// The slot is now owned by this thread.
                    array[slotsIndex].Item = item;

                    // Publish the item (release) so that dequeuers observe a fully written Item.
                    Volatile.Write(ref array[slotsIndex].SequenceNumber, unchecked(currentTail + 1));
                    return true;
                }

                // CAS failed: its return value is the freshest tail, so reuse it instead of re-reading.
                currentTail = witnessed;
                continue;
            }
            else if (diff < 0)
            {// The slot is still occupied by a previous lap: the queue is full.
                return false;
            }

            currentTail = Volatile.Read(ref this.headAndTail.Tail);
        }
    }

    [DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
    private struct Slot
    {
        public T? Item;
        public int SequenceNumber;
    }
}

[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
[StructLayout(LayoutKind.Explicit, Size = 3 * CacheLineSize)] // padding before/between/after fields
internal struct PaddedHeadAndTail
{
    // 128 unconditionally: ARM64 (e.g. Apple Silicon) uses 128-byte cache lines, and modern x64 CPUs
    // prefetch cache lines in 128-byte pairs (adjacent-line prefetcher), so 64 bytes is not enough
    // to prevent false sharing there either. TARGET_ARM64 is a runtime-repo define and is never set
    // in ordinary projects, so a #if on it silently picks the wrong value on ARM64.
    public const int CacheLineSize = 128;

    [FieldOffset(1 * CacheLineSize)]
    public int Head;

    [FieldOffset(2 * CacheLineSize)]
    public int Tail;
}
