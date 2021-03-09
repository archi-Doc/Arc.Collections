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

            var ss = new OrderedSet<int>();
            result = ss.Validate();
            ss.Add(10);
            result = ss.Validate();
            ss.Add(3);
            result = ss.Validate();
            ss.Add(4);
            result = ss.Validate();
            ss.Add(14);
            result = ss.Validate();
            ss.Add(24);
            result = ss.Validate();
            ss.Add(-4);
            result = ss.Validate();
            ss.Add(1);
            result = ss.Validate();
            ss.Remove(3);
            result = ss.Validate();
            ss.Remove(1);
            result = ss.Validate();
        }
    }
}
