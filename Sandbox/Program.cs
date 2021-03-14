using System;
using Arc.Collection;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            bool result;
            Console.WriteLine("Hello World!");

            var os = new OrderedSet<int>();
            result = os.Validate();
            os.Add(10);
            result = os.Validate();
            os.Add(3);
            result = os.Validate();
            os.Add(4);
            result = os.Validate();
            os.Add(14);
            result = os.Validate();
            os.Add(24);
            result = os.Validate();
            os.Add(-4);
            result = os.Validate();
            os.Add(1);
            result = os.Validate();
            os.Remove(3);
            result = os.Validate();
            os.Remove(1);
            result = os.Validate();

            var ol = new OrderedKeyValueList<int, int>();
            ol.Add(2, 0);
            ol.Add(3, 0);
            ol.Add(1, 1);
        }
    }
}
