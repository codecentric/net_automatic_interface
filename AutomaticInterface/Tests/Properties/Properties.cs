namespace Tests.Properties;

public class Properties
{
    [Fact]
    public async Task WorksWithRef()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;
            [GenerateAutomaticInterface]
            public class DemoClass
            {
                private string _aProperty;
                public ref string AProperty => ref _aProperty;
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task OmitsPrivateSetPropertyInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    /// <inheritdoc />
                    public string Hello { get; private set; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task CopiesDocumentationOfPropertyToInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    /// <summary>
                    /// Bla bla
                    /// </summary>
                    public string Hello { get; private set; }
                                 }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task NullableProperty()
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
                    /// Bla bla
                    /// </summary>
                    public string? NullableProperty { get; set; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task NullableProperty2()
    {
        const string code = """

            using AutomaticInterface;
            using System;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample
            {
                    /// <summary>
                    /// Bla bla
                    /// </summary>
                [GenerateAutomaticInterface]
                class DemoClass
                {

                    /// <summary>
                    /// Bla bla
                    /// </summary>
                    public Task<string?> NullableProperty { get; set; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithNewKeyword()
    {
        const string code = """

            using AutomaticInterface;
            using System.Threading.Tasks;

            namespace AutomaticInterfaceExample;

            public abstract class FirstClass
            {
                public int AProperty { get; set; }
            }

            [GenerateAutomaticInterface]
            public partial class SecondClass : FirstClass
            {
                public new int AProperty { get; set; }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithPropertyShadowing()
    {
        const string code = """
            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public string SomeProperty { get; set; }
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public new string SomeProperty { get; set; }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithPropertyOverrides()
    {
        const string code = """
            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public virtual string SomeProperty { get; set; }
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public override string SomeProperty { get; set; }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesStringPropertyInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    public string Hello { get; set; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesStringPropertySetOnlyInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    private string x;
                    public string Hello { set => x = value; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesStringPropertyGetOnlyInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    private string x;
                    public string Hello { get; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task GeneratesInitPropertyInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {

                [GenerateAutomaticInterface]
                class DemoClass
                {
                    public string Hello { get; init; }
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
