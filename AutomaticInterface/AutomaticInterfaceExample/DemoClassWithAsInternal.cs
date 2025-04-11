using System;
using AutomaticInterface;

namespace AutomaticInterfaceExample
{
    /// <summary>
    /// Class Documentation will be copied
    /// </summary>
    [GenerateAutomaticInterface(asInternal: true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Demo class"
    )]
    public class DemoClass3 : IDemoClass3
    {
        /// <summary>
        /// This is a test method
        /// </summary>
        public void Test() { }
    }
}
