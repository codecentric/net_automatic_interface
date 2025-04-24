namespace Tests.Misc;

public class Misc
{
    [Fact]
    public async Task WorksWithOptionalStructParameters()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample;

                            public struct MyStruct
                            {
                                private int Bar;
                            }

                            [GenerateAutomaticInterface]
                            public class DemoClass
                            {
                                    public bool TryStartTransaction(MyStruct data = default(MyStruct))
                                    {
                            return true;
                            }
                            }


                            """;
        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesEmptyInterface()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {
                                                 }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CopiesDocumentationOfClassToInterface()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {
                                    public string Hello { get; private set; }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task DoesNotCopyCtorToToInterface()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {
                                    DemoClass(string x)
                                    {

                                    }

                                    public string Hello { get; private set; }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task DoesNotCopyStaticMethodsToInterface()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {
                                    public static string Hello => "abc"; // property

                                    public static string StaticMethod()  // method
                                    {
                                        return "static";
                                   }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task DoesNotCopyIndexerToInterface()
    {
        const string code = """

                            using AutomaticInterface;
                            using System;

                            namespace AutomaticInterfaceExample
                            {
                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {

                                    private int[] arr = new int[100];

                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                    public int this[int index] // currently ignored
                                    {
                                        get => arr[index];
                                        set => arr[index] = value;
                                    }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task FullExample()
    {
        const string code = """

                            using AutomaticInterface;
                            using System;

                            namespace AutomaticInterfaceExample
                            {
                                    /// <summary>
                                    /// Bla bla
                                    /// </summary>
                                [GenerateAutomaticInterface]
                                class DemoClass
                                {
                                    /// <summary>
                                    /// Property Documentation will be copied
                                    /// </summary>
                                    public string Hello { get; set; }  // included, get and set are copied to the interface when public

                                    public string OnlyGet { get; } // included, get and set are copied to the interface when public

                                    /// <summary>
                                    /// Method Documentation will be copied
                                    /// </summary>
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
                                            return "static" + DateTime.Now;
                                        }

                                    /// <summary>
                                    /// event Documentation will be copied
                                    /// </summary>

                                    public event EventHandler ShapeChanged;  // included

                                    private event EventHandler ShapeChanged2; // ignored because not public

                                    private readonly int[] arr = new int[100];

                                    public int this[int index] // currently ignored
                                    {
                                        get => arr[index];
                                        set => arr[index] = value;
                                    }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithNullableContext()
    {
        const string code = """

                            using AutomaticInterface;
                            namespace AutomaticInterfaceExample;
                            [GenerateAutomaticInterface]
                            public class DemoClass
                            {
                                public string AMethod(DemoClass? x, string y)
                                {
                                    return "Ok";
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CustomInterfaceAndNamespace()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                [GenerateAutomaticInterface("CustomNamespace", "ISpecialInterface")]
                                public class DemoClassWithCustomInterfaceName : ISpecialInterface
                                {
                                    /// <summary>
                                    /// This is a test method
                                    /// </summary>
                                    public void Test() { }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CustomInterfaceAndNamespaceParametersReversed()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                [GenerateAutomaticInterface(interfaceName: "ISpecialInterface", namespaceName: "CustomNamespace")]
                                public class DemoClassWithCustomInterfaceName : ISpecialInterface
                                {
                                    /// <summary>
                                    /// This is a test method
                                    /// </summary>
                                    public void Test() { }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CustomInterface()
    {
        const string code = """

                            using AutomaticInterface;

                            namespace AutomaticInterfaceExample
                            {
                                [GenerateAutomaticInterface(interfaceName: "ISpecialInterface")]
                                public class DemoClassWithCustomInterfaceName : ISpecialInterface
                                {
                                    /// <summary>
                                    /// This is a test method
                                    /// </summary>
                                    public void Test() { }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CustomNameSpace()
    {
        const string code = """

                            using AutomaticInterface;
                            using System;
                            using System.IO;
                            using System.Threading;
                            using System.Threading.Tasks;

                            namespace AutomaticInterfaceExample
                            {
                                /// <summary>
                                /// Bla bla
                                /// </summary>
                                [GenerateAutomaticInterface("CustomNameSpace")]
                                class DemoClass
                                {

                                   public async Task<Stream?> GetFinalDocumentsByIDFails(
                                                   string agreementID, 
                                                   string docType, 
                                                   bool amt = false , 
                                                   bool? attachSupportingDocuments = true, 
                                                   CancellationToken cancellationToken = default)
                                  {
                                      await Task.Delay(100);
                                      return default(Stream?);

                                  }
                                }
                            }

                            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
