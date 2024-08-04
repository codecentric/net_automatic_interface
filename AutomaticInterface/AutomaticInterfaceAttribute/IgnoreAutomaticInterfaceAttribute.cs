using System;

namespace AutomaticInterfaceAttribute
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class IgnoreAutomaticInterfaceAttribute : Attribute
    {
        public IgnoreAutomaticInterfaceAttribute() { }
    }
}
