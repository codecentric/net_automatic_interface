using System;

namespace AutomaticInterfaceExample
{
    class Program
    {
        static void Main(string[] args)
        {

            DemoClass demo = new DemoClass();
            IDemoClass demoInterface = demo;

            Console.WriteLine("Hello World!");
        }
    }
}
