using System.CodeDom.Compiler;
using AutomaticInterfaceAttribute;
using System;

/// <summary>
/// Result of the generator
/// </summary>
namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Bla bla
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
        /// <summary>
        /// event Documentation will be copied
        /// </summary> 

        event System.EventHandler ShapeChanged;
    }
}