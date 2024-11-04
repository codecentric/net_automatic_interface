using System;
using System.Threading.Tasks;
using AutomaticInterface;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Class Documentation will be copied
    /// </summary>
    [GenerateAutomaticInterface]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Demo class"
    )]
    public class DemoClass : IDemoClass // Generics, including constraints are allowed, too.
    {
        /// <summary>
        /// Property Documentation will be copied
        /// </summary>
        public string Hello { get; set; } // included, get and set are copied to the interface when public

        public string OnlyGet { get; } // included, get and set are copied to the interface when public

        [IgnoreAutomaticInterface]
        public string? AnotherGet { get; } // ignored with help of attribute

        /// <summary>
        /// Method Documentation will be copied
        /// </summary>
        public string AMethod(string x, string y) // included
        {
            return BMethod(x, y);
        }

        private string BMethod(string x, string y) // ignored because not public
        {
            return x + y;
        }

        /// <summary>
        /// CMethod allows operations with multiple generic type parameters and string inputs.
        /// </summary>
        /// <typeparam name="T">The first generic type parameter which must be a class.</typeparam>
        /// <typeparam name="T1">The second generic type parameter which must be a structure.</typeparam>
        /// <typeparam name="T2">The third generic type parameter.</typeparam>
        /// <typeparam name="T3">The fourth generic type parameter which must be derived from DemoClass.</typeparam>
        /// <typeparam name="T4">The fifth generic type parameter which must implement IDemoClass.</typeparam>
        /// <param name="x">The optional first string input parameter.</param>
        /// <param name="y">The second string input parameter.</param>
        /// <return>Returns a string result.</return>
        public string CMethod<T, T1, T2, T3, T4>(string? x, string y) // included
            where T : class
            where T1 : struct
            where T3 : DemoClass
            where T4 : IDemoClass
        {
            return "Ok";
        }

        public Task<string> ASync(string x, string y)
        {
            return Task.FromResult("");
        }

        public static string StaticProperty => "abc"; // static property, ignored

        public static string StaticMethod() // static method, ignored
        {
            return "static" + DateTime.Now;
        }

        /// <summary>
        /// event Documentation will be copied
        /// </summary>
#pragma warning disable S3264 // Events should be invoked
        public event EventHandler ShapeChanged; // included
#pragma warning restore S3264 // Events should be invoked

#pragma warning disable S3264 // Events should be invoked
#pragma warning disable IDE0051 // Remove unused private members
        private event EventHandler ShapeChanged2; // ignored because not public
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore S3264 // Events should be invoked

        public event EventHandler? ShapeChangedNullable; // included

        public event EventHandler<string?> ShapeChangedNullable2; // included

        private readonly int[] arr = new int[100];

        public int this[int index] // currently ignored
        {
            get => arr[index];
            set => arr[index] = value;
        }
    }
}
