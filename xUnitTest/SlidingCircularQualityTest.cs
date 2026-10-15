// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class SlidingCircularQualityTest
{
    private const int MaxPosition = 0x7FFFFFFF;

    #region SlidingList

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(6)]
    public void SlidingListCopyToRejectsOutOfRangeArrayIndex(int arrayIndex)
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        var array = new string[5];

        // ICollection<T>.CopyTo specifies ArgumentOutOfRangeException for an invalid arrayIndex.
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(array, arrayIndex));
        Assert.Equal("arrayIndex", exception.ParamName);
        Assert.All(array, item => Assert.Null(item));

        // An empty list validates the index too.
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingList<string>(4).CopyTo(array, arrayIndex));
    }

    [Fact]
    public void SlidingListCopyToTooSmallThrowsArgumentException()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        list.Add("b");

        var exception = Assert.Throws<ArgumentException>(() => list.CopyTo(new string[3], 2));
        Assert.Equal("array", exception.ParamName);
    }

    [Fact]
    public void SlidingListCopyToWithNoLiveElementLeavesTheArrayUntouched()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        list.Add("b");
        Assert.True(list.TryRemoveAt(1)); // a hole after a live element
        Assert.True(list.TryRemoveAt(0)); // removing the head slides over the hole

        var array = new[] { "x", "y" };
        list.CopyTo(array, 0);
        list.CopyTo(array, 2); // arrayIndex == array.Length is valid for an empty source

        Assert.Equal(new[] { "x", "y" }, array);
    }

    [Fact]
    public void SlidingListCopyToSkipsHolesInAWrappedWindow()
    {
        var list = new SlidingList<string>(4);
        for (var i = 0; i < 4; i++)
        {
            list.Add("a" + i);
        }

        Assert.True(list.TryRemoveAt(0));
        Assert.True(list.TryRemoveAt(1));
        list.Add("b0"); // stored in the wrapped part of the buffer
        list.Add("b1");
        Assert.True(list.TryRemoveAt(3)); // a hole before the wrap point

        var array = new string[5];
        list.CopyTo(array, 1);

        Assert.Equal(new[] { null, "a2", "b0", "b1", null }, array);
    }

    [Fact]
    public void SlidingListCollectionAddAppendsAndDropsWhenFull()
    {
        var list = new SlidingList<string>(2);
        ICollection<string> collection = list;

        collection.Add("a");
        collection.Add("b");
        collection.Add("dropped"); // no free slot: silently ignored

        Assert.Equal(2, collection.Count);
        Assert.Equal(new[] { "a", "b" }, list.ToArray());
        Assert.Equal(-1, list.IndexOf("dropped"));
        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    [Fact]
    public void SlidingListNullItemExceptionsNameTheItemParameter()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");

        // Insert and ICollection<T>.Add take 'item', not 'value'.
        var insert = Assert.Throws<ArgumentNullException>(() => list.Insert(list.StartPosition, null!));
        Assert.Equal("item", insert.ParamName);
        var add = Assert.Throws<ArgumentNullException>(() => ((ICollection<string>)list).Add(null!));
        Assert.Equal("item", add.ParamName);

        // The null check comes first, even for a position outside the window.
        Assert.Equal("item", Assert.Throws<ArgumentNullException>(() => list.Insert(-1, null!)).ParamName);

        // Members whose parameter is named 'value' keep that name.
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => list.Add(null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => list.TrySet(0, null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => list[0] = null!).ParamName);

        Assert.Equal(new[] { "a" }, list.ToArray());
        Assert.Equal(1, list.UsedSlotCount);
    }

    [Fact]
    public void SlidingListClearFreesHolesWithoutSliding()
    {
        var list = new SlidingList<string>(2);
        list.Add("a");
        var b = list.Add("b");
        Assert.True(list.TryRemoveAt(b)); // a hole at the tail: no slide
        Assert.False(list.CanAdd);
        Assert.Equal(0, list.Slide()); // the head is live, so Slide cannot free the hole

        list.Clear();

        Assert.True(list.CanAdd);
        Assert.Equal(0, list.UsedSlotCount);
        Assert.Equal(2, list.StartPosition); // the former EndPosition
        Assert.Equal(2, list.Add("c"));
    }

    [Fact]
    public void SlidingListAddUsesEndPositionWhenOnlyHolesRemain()
    {
        var list = new SlidingList<string>(4);
        Assert.True(list.TrySet(1, "x"));
        Assert.True(list.TryRemoveAt(1)); // not the head: slots 0 and 1 stay as holes

        var liveCount = ((ICollection<string>)list).Count;
        Assert.Equal(0, liveCount);
        Assert.Equal(2, list.UsedSlotCount);
        Assert.Equal(0, list.StartPosition);
        Assert.Equal(2, list.Add("y")); // EndPosition, not StartPosition

        Assert.True(list.TryRemoveAt(2));
        Assert.Equal(3, list.UsedSlotCount);
        Assert.Equal(3, list.Slide()); // no live element: Slide reclaims every hole
        Assert.Equal(0, list.UsedSlotCount);
        Assert.Equal(3, list.StartPosition);
        Assert.Equal(0, list.Slide());
        Assert.Equal(list.StartPosition, list.Add("z"));
    }

    [Fact]
    public void SlidingListRemoveAtStartPositionSlidesInsteadOfLeavingAHole()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        list.Add("b");
        list.Add("c");

        Assert.True(list.Remove("b")); // a hole in the middle
        Assert.Equal(0, list.StartPosition);
        Assert.Equal(3, list.UsedSlotCount);

        Assert.True(list.Remove("a")); // the head: the window slides over it and the hole after it
        Assert.Equal(2, list.StartPosition);
        Assert.Equal(1, list.UsedSlotCount);
        Assert.Equal(new[] { "c" }, list.ToArray());
        Assert.False(list.Remove("a"));
        Assert.False(list.Remove(null!));
    }

    [Fact]
    public void SlidingListReadOnlyCountExcludesHoles()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        list.Add("b");
        list.Add("c");
        Assert.True(list.TryRemoveAt(1));

        IReadOnlyCollection<string> readOnly = list;
        Assert.Equal(2, readOnly.Count);
        Assert.Equal(((ICollection<string>)list).Count, readOnly.Count);
        Assert.Equal(3, list.UsedSlotCount);
    }

    [Fact]
    public void SlidingListNonGenericEnumerationSkipsHoles()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        list.Add("b");
        list.Add("c");
        Assert.True(list.TryRemoveAt(1));

        var items = new List<object>();
        foreach (var item in (IEnumerable)list)
        {
            items.Add(item);
        }

        Assert.Equal(new object[] { "a", "c" }, items);
    }

    [Fact]
    public void SlidingListEnumeratorResetAfterModificationThrows()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        IEnumerator<string> enumerator = ((IEnumerable<string>)list).GetEnumerator();
        Assert.True(enumerator.MoveNext());

        list.Add("b");

        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void SlidingListEnumeratorStaysFinishedAfterTheEnd()
    {
        var list = new SlidingList<string>(2);
        list.Add("a");
        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        enumerator.Dispose();
    }

    [Fact]
    public void SlidingListResizeKeepsEnumeratorValidOnlyWhenCapacityIsUnchanged()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        var enumerator = list.GetEnumerator();

        Assert.True(list.Resize(4)); // no-op: the enumerator stays valid
        Assert.True(enumerator.MoveNext());

        Assert.True(list.Resize(8));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void SlidingListGetFirstOrDefaultReclaimsAWindowMadeOnlyOfHoles()
    {
        var list = new SlidingList<string>(4);
        var start = list.StartPosition;

        // TrySet beyond EndPosition creates leading holes; removing a non-head element does not slide.
        Assert.True(list.TrySet(start + 3, "last"));
        Assert.True(list.TryRemoveAt(start + 3));
        Assert.Equal(4, list.UsedSlotCount);
        Assert.False(list.CanAdd);

        Assert.Null(list.GetFirstOrDefault());

        Assert.Equal(0, list.UsedSlotCount);
        Assert.Equal(start + 4, list.StartPosition);
        Assert.True(list.CanAdd);
        Assert.Equal(start + 4, list.Add("next"));
        Assert.Equal("next", list.GetFirstOrDefault());
    }

    [Fact]
    public void SlidingListGetFirstOrDefaultSkipsLeadingHoles()
    {
        var list = new SlidingList<string>(4);
        Assert.True(list.TrySet(2, "c"));
        Assert.Equal(0, list.StartPosition);

        Assert.Equal("c", list.GetFirstOrDefault());
        Assert.Equal(2, list.StartPosition);
        Assert.Equal(1, list.UsedSlotCount);
    }

    [Fact]
    public void SlidingListPositionsWrapAcrossTheBufferAndThePositionLimit()
    {
        // The window starts two positions before the 31-bit limit and at the last physical slot.
        var list = new SlidingList<string>(4);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(list.TryRemoveAt(list.Add("advance")));
        }

        typeof(SlidingList<string>)
            .GetField("startPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(list, MaxPosition - 1);

        var positions = new int[4];
        for (var i = 0; i < 4; i++)
        {
            positions[i] = list.Add("v" + i);
        }

        Assert.Equal(new[] { MaxPosition - 1, MaxPosition, 0, 1 }, positions);
        Assert.Equal(-1, list.Add("full"));
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal("v" + i, list[positions[i]]);
        }

        Assert.True(list.TryRemoveAt(positions[2]));
        Assert.Null(list.GetOrDefault(positions[2]));
        Assert.True(list.TrySet(positions[2], "w2"));
        Assert.Equal(positions[3], list.IndexOf("v3"));
        Assert.Equal(new[] { "v0", "v1", "w2", "v3" }, list.ToArray());

        Assert.True(list.Resize(6));
        Assert.Equal(MaxPosition - 1, list.StartPosition);
        Assert.Equal(2, list.EndPosition);
        Assert.Equal("w2", list.GetOrDefault(0));
        Assert.Equal(2, list.Add("v4"));
    }

    [Fact]
    public void SlidingListStructEnumerationAndPositionOperationsDoNotAllocate()
    {
        var list = new SlidingList<string>(8);
        var value = "value";
        var total = 0;
        var allocated = AllocationHelper.Measure(() => total = Exercise(list, value));

        Assert.Equal(400, total);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static int Exercise(SlidingList<string> list, string value)
        {
            var total = 0;
            for (var round = 0; round < 100; round++)
            {
                var p0 = list.Add(value);
                var p1 = list.Add(value);
                list.Add(value);
                list.TryRemoveAt(p1);
                foreach (var item in list)
                {
                    total += item.Length > 0 ? 1 : 0;
                }

                total += list.IndexOf(value) == p0 ? 1 : 0;
                total += list.GetFirstOrDefault() is null ? 0 : 1;
                list.Clear();
            }

            return total;
        }
    }

    #endregion

    #region CircularQueue

    [Fact]
    public void CircularQueueHandlesHeadAndTailOverflow()
    {
        var queue = new CircularQueue<int>(4);
        SetPositions(queue, int.MaxValue - 5);

        var next = 0;
        var expected = 0;
        for (var round = 0; round < 6; round++)
        {
            for (var i = 0; i < queue.Capacity; i++)
            {
                Assert.True(queue.TryEnqueue(next++));
            }

            Assert.False(queue.TryEnqueue(-1));
            Assert.Equal(queue.Capacity, queue.Count);

            // Dequeue part of the batch so that head and tail straddle the overflow at different times.
            for (var i = 0; i < 3; i++)
            {
                Assert.True(queue.TryDequeue(out var value));
                Assert.Equal(expected++, value);
            }

            Assert.Equal(1, queue.Count);
            Assert.True(queue.TryDequeue(out var last));
            Assert.Equal(expected++, last);
            Assert.False(queue.TryDequeue(out _));
            Assert.Equal(0, queue.Count);
        }

        var (head, tail) = GetHeadAndTail(queue);
        Assert.True(head < 0); // both counters wrapped past int.MaxValue
        Assert.Equal(head, tail);
    }

    [Theory]
    [InlineData(5, 0)] // a snapshot where head ran ahead of tail
    [InlineData(0, 5)] // a snapshot where tail ran more than a lap ahead of head
    [InlineData(int.MaxValue, int.MinValue + 4)] // the same, across the overflow
    public void CircularQueueCountStaysWithinCapacityForTornSnapshots(int head, int tail)
    {
        var queue = new CircularQueue<int>(4);
        SetHeadAndTail(queue, head, tail);

        var count = queue.Count;

        Assert.InRange(count, 0, queue.Capacity);
        Assert.Equal(unchecked(tail - head) < 0 ? 0 : queue.Capacity, count);
    }

    [Fact]
    public void CircularQueueClearsSlotsOfValueTypesContainingReferences()
    {
        var queue = new CircularQueue<KeyValuePair<int, string>>(2);
        Assert.True(queue.TryEnqueue(new(1, "one")));
        Assert.True(queue.TryEnqueue(new(2, "two")));

        Assert.True(queue.TryDequeue(out var first));
        Assert.Equal(new KeyValuePair<int, string>(1, "one"), first);

        var slots = (Array)typeof(CircularQueue<KeyValuePair<int, string>>)
            .GetField("slotArray", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(queue)!;
        var itemField = slots.GetType().GetElementType()!.GetField("Item")!;
        Assert.Equal(default(KeyValuePair<int, string>), itemField.GetValue(slots.GetValue(0)));
        Assert.Equal(new KeyValuePair<int, string>(2, "two"), itemField.GetValue(slots.GetValue(1)));
    }

    [Fact]
    public void CircularQueueOperationsDoNotAllocate()
    {
        var queue = new CircularQueue<string>(4);
        var value = "value";
        var total = 0;
        var allocated = AllocationHelper.Measure(() => total = Exercise(queue, value));

        Assert.Equal(500, total);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static int Exercise(CircularQueue<string> queue, string value)
        {
            var total = 0;
            for (var round = 0; round < 100; round++)
            {
                while (queue.TryEnqueue(value))
                {
                    total++;
                }

                while (queue.TryDequeue(out var item))
                {
                    total += ReferenceEquals(item, value) ? 0 : 1000;
                }

                total += queue.Count + 1;
            }

            return total;
        }
    }

    private static FieldInfo HeadAndTailField<T>()
        => typeof(CircularQueue<T>).GetField("headAndTail", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static (int Head, int Tail) GetHeadAndTail<T>(CircularQueue<T> queue)
    {
        var boxed = HeadAndTailField<T>().GetValue(queue)!;
        var type = boxed.GetType();
        return ((int)type.GetField("Head")!.GetValue(boxed)!, (int)type.GetField("Tail")!.GetValue(boxed)!);
    }

    private static void SetHeadAndTail<T>(CircularQueue<T> queue, int head, int tail)
    {
        var field = HeadAndTailField<T>();
        var boxed = field.GetValue(queue)!;
        var type = boxed.GetType();
        type.GetField("Head")!.SetValue(boxed, head);
        type.GetField("Tail")!.SetValue(boxed, tail);
        field.SetValue(queue, boxed);
    }

    /// <summary>
    /// Moves an empty queue to <paramref name="position"/>, as if that many items had passed through it,
    /// so that the 32-bit counter overflow can be tested without performing 2^31 operations.
    /// </summary>
    private static void SetPositions<T>(CircularQueue<T> queue, int position)
    {
        SetHeadAndTail(queue, position, position);

        var slots = (Array)typeof(CircularQueue<T>)
            .GetField("slotArray", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(queue)!;
        var sequenceField = slots.GetType().GetElementType()!.GetField("SequenceNumber")!;
        var mask = slots.Length - 1;
        for (var i = 0; i < slots.Length; i++)
        {
            // Each slot expects the first position at or after 'position' that maps to it.
            var expected = unchecked(position + ((i - position) & mask));
            var slot = slots.GetValue(i)!;
            sequenceField.SetValue(slot, expected);
            slots.SetValue(slot, i);
        }
    }

    #endregion
}
