using System;
using Arc.Collection;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var ol = new OrderedKeyValueList<int, int>();
            ol.Add(2, 0);
            ol.Add(3, 0);
            ol.Add(1, 1);
        }
    }
}
