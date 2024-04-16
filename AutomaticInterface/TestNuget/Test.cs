using AutomaticInterface;

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
