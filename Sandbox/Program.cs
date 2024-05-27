using System;
using Arc.Collections;

namespace Sandbox;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        var rentArray = BytePool.Default.Rent(10);
    }
}
