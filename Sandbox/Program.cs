using System;
using Arc.Collections;

namespace Sandbox;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        var rentArray = BytePool.Default.Rent(10);

        Console.WriteLine(CollectionHelper.CalculatePowerOfTwoCapacity(30));
        Console.WriteLine(CollectionHelper.CalculatePowerOfTwoCapacity(31));
        Console.WriteLine(CollectionHelper.CalculatePowerOfTwoCapacity(32));
        Console.WriteLine(CollectionHelper.CalculatePowerOfTwoCapacity(33));
    }
}
