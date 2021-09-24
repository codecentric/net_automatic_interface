using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass: IDemoClass
    {
        public string Hello { get; set; }  // included, get and set are copied to the interface when public

        public string OnlyGet { get; } // included, get and set are copied to the interface when public

        public string AMethod(string x, string y) // included
        {
            return BMethod(x,y);
        }

        private string BMethod(string x, string y) // ignored because not public
        {
            return x + y;
        }

        public static string StaticProperty => "abc"; // static property, ignored
        public static string StaticMethod()  // static method, ignored
        {
            return "static";
       }
    }
}
