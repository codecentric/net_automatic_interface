using AutomaticInterfaceAttribute;

namespace Runner
{
    [GenerateAutomaticInterface]
    class DemoClass : IDemoClass
    {
        DemoClass demo = new DemoClass();
        IDemoClass demoInterface = demo;

        public string Hello { get; set; }
    }
}
