using System;
using Arc.Collections;

namespace Sandbox;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        var map = new Utf16UnorderedMap<int>();
        map.TryAdd("abc", 123);

        foreach (var x in map)
        {
        }
    }
}
