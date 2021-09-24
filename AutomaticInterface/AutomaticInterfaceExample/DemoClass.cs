using AutomaticInterfaceAttribute;
using System;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Class Documentation will be copied
    /// </summary>
    [GenerateAutomaticInterface]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Demo class")]
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
            return "static" + DateTime.Now;
        }

        /// <summary>
        /// event Documentation will be copied
        /// </summary>
#pragma warning disable S3264 // Events should be invoked
        public event EventHandler ShapeChanged;  // included
#pragma warning restore S3264 // Events should be invoked

#pragma warning disable S3264 // Events should be invoked
#pragma warning disable IDE0051 // Remove unused private members
        private event EventHandler ShapeChanged2; // ignored because not public
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore S3264 // Events should be invoked

        private readonly int[] arr = new int[100];

        public int this[int index] // currently ignored
        {
            get => arr[index];
            set => arr[index] = value;
        }
    }
}
