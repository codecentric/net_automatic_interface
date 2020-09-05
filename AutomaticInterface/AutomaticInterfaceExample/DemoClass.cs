using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass: IDemoClass
    {
        public string Hello { get; set; }
    }
}
