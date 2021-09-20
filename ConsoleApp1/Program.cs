// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Collections;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var array = new int[] { 2, 1, 3, };
            var os = new OrderedSet<int>(array);

            ConsoleWriteIEnumerable("Array:", array); // 2, 1, 3
            ConsoleWriteIEnumerable("OrderedSet:", os); // 1, 2, 3

            Console.WriteLine("Add 4, 0");
            os.Add(4);
            os.Add(0);
            ConsoleWriteIEnumerable("OrderedSet:", os); // 0, 1, 2, 3, 4

            static void ConsoleWriteIEnumerable<T>(string header, IEnumerable<T> e)
            {
                Console.WriteLine(string.Format("{0,-12}", header) + string.Join(", ", e));
            }
        }
    }
}
