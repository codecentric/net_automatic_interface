using AutomaticInterfaceAttribute;

namespace TestNuget
{
    [GenerateAutomaticInterface]
    public class Test : ITest
    {
        public string GetString()
        {
            return "works";
        }
    }
}
