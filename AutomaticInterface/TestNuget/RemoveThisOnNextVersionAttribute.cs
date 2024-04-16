using System;

namespace AutomaticInterface
{
    /// <summary>
    /// Use source generator to automatically create a Interface from this class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("This attribute is obsolete. Use generated attribute instead.", false)]
    internal sealed class GenerateAutomaticInterfaceAttribute : Attribute
    {
        internal GenerateAutomaticInterfaceAttribute(string namespaceName = "") { }
    }
}