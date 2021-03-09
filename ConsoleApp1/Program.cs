// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collection;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var array = new int[] { 2, 1, 3, };
            var os = new OrderedSet<int>(array);

            Console.WriteLine(string.Format("{0,-12}", "Array: ") + string.Join(", ", array)); // 2, 1, 3
            Console.WriteLine(string.Format("{0,-12}", "OrderedSet: ") + string.Join(", ", os)); // 1, 2, 3

            Console.WriteLine("Add 4, 0");
            os.Add(4);
            os.Add(0);
            Console.WriteLine(string.Format("{0,-12}", "OrderedSet: ") + string.Join(", ", os)); // 0, 1, 2, 3, 4
        }
    }
}
