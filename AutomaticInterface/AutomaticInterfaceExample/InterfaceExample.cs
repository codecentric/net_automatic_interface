#nullable enable
using System;
using System.CodeDom.Compiler;
using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Result of the generator
    /// </summary>
    [GeneratedCode("AutomaticInterface", "")]
    public partial interface InterfaceExample // would be IDemoClass normally, changed to avoid naming problems
    {
        /// <summary>
        /// Property Documentation will be copied
        /// </summary>
        string Hello { get; set; }

        string OnlyGet { get; }

        /// <summary>
        /// Method Documentation will be copied
        /// </summary>
        string AMethod(string x, string y);

        string CMethod<T, T1, T2, T3, T4>(string? x, string y)
            where T : class
            where T1 : struct
            where T3 : DemoClass
            where T4 : IDemoClass;

        /// <summary>
        /// event Documentation will be copied
        /// </summary>
        event System.EventHandler ShapeChanged;
    }
}
#nullable restore
