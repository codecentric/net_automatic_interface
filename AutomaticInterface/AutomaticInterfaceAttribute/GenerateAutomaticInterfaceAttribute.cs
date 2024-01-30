using System;

namespace AutomaticInterfaceAttribute
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateAutomaticInterfaceAttribute : Attribute
    {
        public GenerateAutomaticInterfaceAttribute(string namespaceName = "") { }
    }
}
