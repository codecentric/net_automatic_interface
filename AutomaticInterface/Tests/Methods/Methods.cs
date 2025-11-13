namespace Tests.Methods;

public class Methods
{
    [Fact]
    public async Task WorksWithOptionalParameters()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                    public bool TryStartTransaction(
                        string file = "",
                        string member = "",
                        int line = 0,
                        bool notify = true)
                    {
            return true;
            }
            }


            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithParamsParameters()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(params int[] numbers)
                {
                }
            }


            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithOptionalNullParameters()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                    public bool TryStartTransaction(string data = null)
                    {
            return true;
            }
            }


            """;
        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMixedOptionalNullParameters()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                    public bool TryStartTransaction(int? param, int param2 = 0, string data = null)
                    {
            return true;
            }
            }


            """;
        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsPublicMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                              
                    public string Hello(){return "";}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsPublicTaskMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                   public async Task<string> Hello(){return "";}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsPublicWithParamsMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                              
                    public string Hello(string x){return x;}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsPublicWithParamsGenericMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    public string Hello(Task<string> x){return "";}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsPublicWithMultipleParamsMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    public string Hello(string x, int y, double z){return x;}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task IgnoresNotPublicMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    private string Hello(string x, int y, double z){return x;}
                    internal string Hello2(string x, int y, double z){return x;}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task IgnoresMembersAttributedWithIgnore()
    {
        const string code = """

            using AutomaticInterface;
            using System;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    [IgnoreAutomaticInterface] public string Hello1(string x, int y, double z){return x;}
                    [IgnoreAutomaticInterface] public string Hello2 { get; set; }
                    [IgnoreAutomaticInterface] public event EventHandler Hello3;
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsDescriptionFromMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {

                    /// <summary>
                    /// TEST
                    /// </summary>
                    /// <returns></returns>
                    public string Hello(string x){return x;}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task AddsMultiLineDescriptionFromMethodToInterface()
    {
        const string code = """

            using AutomaticInterface;
            using System.IO;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {

                    /**
                     * <summary>Hello World!</summary>
                     */
                    public string Hello(string x){return x;}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithGenericMethods()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                /// <inheritdoc />
                public string CMethod<T, T1, T2, T3, T4, T5>(string x, string y)
                    where T : class
                    where T1 : struct
                    where T3 : DemoClass
                    where T4 : IDemoClass
                    where T5 : new()
                {
                    return "Ok";
                }
            }


            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithGenericTypeConstraintsForMethods()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public IQueryable<T> AddFilter<T>(IQueryable<T> qry) where T : notnull => qry;
            }


            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesOverloadedMethodInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    public string AMethod(string x, string y)
                    {
                        return string.Empty;
                    }

                    public string AMethod(string x, string y, string crash)
                    {
                        return string.Empty;
                    }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithNullableReturn()
    {
        const string code = """

            using AutomaticInterface;
            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public string? AMethod(DemoClass x, string y)
                {
                    return "Ok";
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task BooleanWithNonNull()
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
                [GenerateAutomaticInterface]
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

    [Fact]
    public async Task WorksWithNullableGeneric()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public Task<string?> AMethodAsync(DemoClass x, string y)
                {
                    return Task.FromResult("Ok");
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithNullableGeneric2()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public string AMethod(Task<DemoClass?> x, string y)
                {
                    return "Ok";
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithReservedNames()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(int @event)
                {
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMethodOverrides()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public virtual bool AMethod() => true;
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public override bool AMethod() => true;
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMethodShadowing()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public bool AMethod() => true;
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public new bool AMethod() => true;
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithParameterDirectionOverloads()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(int val) {}
                
                public void AMethod(ref int val){}
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithGenericParameterOverloads()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(Func<Task<int>> getValue) {}
                
                public void AMethod(Func<Task<float>> getValue) {}
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithGenericParameterOverloadsWithIdenticalTypeNames()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {
                namespace Types1 {
                    public class Model;
                }

                namespace Types2 {
                    public class Model;
                }

                [GenerateAutomaticInterface]
                public class DemoClass
                {
                    public void AMethod(Func<Task<Types1.Model>> getValue) {}
                    
                    public void AMethod(Func<Task<Types2.Model>> getValue) {}
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMethodOutParameter()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(out int someOutParameter)
                {
                    someOutParameter = 1;
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMethodInParameter()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(in int someOutParameter)
                {
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithMethodRefParameter()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void AMethod(ref int someOutParameter)
                {
                }
            }

            """;
        await Verify(Infrastructure.GenerateCode(code));
    }
}
