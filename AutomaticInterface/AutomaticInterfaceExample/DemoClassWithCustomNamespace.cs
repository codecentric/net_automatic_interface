using System;
using AutomaticInterface;
using CustomNamespace;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Class Documentation will be copied
    /// </summary>
    [GenerateAutomaticInterface("CustomNamespace")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Demo class"
    )]
    public class DemoClass2 : IDemoClass2
    {
        /// <summary>
        /// This is a test method
        /// </summary>
        public void Test()
        {
        }
    }
}