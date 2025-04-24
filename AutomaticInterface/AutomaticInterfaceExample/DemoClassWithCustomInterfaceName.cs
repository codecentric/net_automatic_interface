using AutomaticInterface;

namespace AutomaticInterfaceExample;

[GenerateAutomaticInterface(interfaceName: "ISpecialInterface")]
public class DemoClassWithCustomInterfaceName : ISpecialInterface
{
    /// <summary>
    /// This is a test method
    /// </summary>
    public void Test() { }
}