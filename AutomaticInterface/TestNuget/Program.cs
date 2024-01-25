using System;

namespace TestNuget
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ITest test = new Test();

            Console.WriteLine(test.GetString());
        }
    }
}
