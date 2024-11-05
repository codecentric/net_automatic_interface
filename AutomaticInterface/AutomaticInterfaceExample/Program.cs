using System;

namespace AutomaticInterfaceExample
{
    public class Program
    {
        static void Main(string[] args)
        {
            DemoClass demo = new DemoClass();
            Console.WriteLine(DemoClass.StaticMethod());

            Console.WriteLine(DemoClass.StaticProperty);

            Console.WriteLine(demo.AMethod("A", "B"));

            IDemoClass demoInterface = demo;
            Console.WriteLine(demoInterface.AMethod("A", "B"));
            Console.WriteLine(
                demoInterface.CMethod<string, int, uint, DemoClass, DemoClass>("A", "B")
            );
            Console.WriteLine("Hello World!");
        }
    }
}
