// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Benchmark;

public readonly struct Identifier : IEquatable<Identifier>, IComparable<Identifier>
{
    public Identifier()
    {
        this.Id0 = 0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
    }

    public Identifier(int id0)
    {
        this.Id0 = (ulong)id0;
        this.Id1 = 0;
        this.Id2 = 0;
        this.Id3 = 0;
    }

    public readonly ulong Id0;

    public readonly ulong Id1;

    public readonly ulong Id2;

    public readonly ulong Id3;

    public bool Equals(Identifier other)
    {
        return this.Id0 == other.Id0 && this.Id1 == other.Id1 && this.Id2 == other.Id2 && this.Id3 == other.Id3;
    }

    public override int GetHashCode() => (int)this.Id0; // HashCode.Combine(this.Id0, this.Id1, this.Id2, this.Id3);

    public int CompareTo(Identifier other)
    {
        if (this.Id0 > other.Id0)
        {
            return 1;
        }
        else if (this.Id0 < other.Id0)
        {
            return -1;
        }

        if (this.Id1 > other.Id1)
        {
            return 1;
        }
        else if (this.Id1 < other.Id1)
        {
            return -1;
        }

        if (this.Id2 > other.Id2)
        {
            return 1;
        }
        else if (this.Id2 < other.Id2)
        {
            return -1;
        }

        if (this.Id3 > other.Id3)
        {
            return 1;
        }
        else if (this.Id3 < other.Id3)
        {
            return -1;
        }

        return 0;
    }
}
