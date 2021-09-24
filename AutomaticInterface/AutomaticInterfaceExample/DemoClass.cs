using AutomaticInterfaceAttribute;
using System;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Class Documentation will be copied
    /// </summary>
    [GenerateAutomaticInterface]
    class DemoClass: IDemoClass // Generics, including constraints are allowed, too.
    {
        /// <summary>
        /// Property Documentation will be copied
        /// </summary>
        public string Hello { get; set; }  // included, get and set are copied to the interface when public

        public string OnlyGet { get; } // included, get and set are copied to the interface when public

        /// <summary>
        /// Method Documentation will be copied
        /// </summary>
        public string AMethod(string x, string y) // included
        {
            return BMethod(x,y);
        }

        private string BMethod(string x, string y) // ignored because not public
        {
            return x + y;
        }

        public static string StaticProperty => "abc"; // static property, ignored
        public static string StaticMethod()  // static method, ignored
        {
            return "static";
        }

        /// <summary>
        /// event Documentation will be copied
        /// </summary>
        public event EventHandler ShapeChanged;  // included

        private event EventHandler ShapeChanged2; // ignored because not public

        private int[] arr = new int[100];
        public int this[int index] // currently ignored
        {
            get => arr[index];
            set => arr[index] = value;
        }
    }
}
