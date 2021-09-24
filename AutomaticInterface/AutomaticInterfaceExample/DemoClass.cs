using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass: IDemoClass
    {
        public string Hello { get; set; }

        public string OnlyGet { get; }

        public static string StaticProperty => "abc"; // ignored
        public static string StaticMethod()  // method
        {
            return "static";
       }
    }
}
