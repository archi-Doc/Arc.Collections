﻿using System;
using Arc.Collections;

namespace Sandbox;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        var rentArray = BytePool.Default.Rent(10);
        var rentMemory = rentArray.AsMemory();
        rentArray.Return();

        rentArray = BytePool.Default.Rent(10);
        rentArray.IncrementAndShare();
        rentArray.Return();
        rentArray.Return();
    }
}
