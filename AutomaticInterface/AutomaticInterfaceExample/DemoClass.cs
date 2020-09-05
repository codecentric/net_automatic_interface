using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass
    {
        public string Hello { get; set; }
    }
}
