namespace Tests.TypeResolutions;

public class TypeResolutions
{
    [Fact]
    public async Task WorksWithFileScopedNamespace()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            class DemoClass
            {
                public string Hello { get; set; }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithBracedNamespace()
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
    public async Task WorksWithUsingAliases()
    {
        const string code = """

            using AutomaticInterface;
            using TaskAlias = System.Threading.Tasks.Task;

            namespace AutomaticInterfaceExample;

            [GenerateAutomaticInterface]
            class DemoClass
            {
                public TaskAlias Hello { get; set; }
            }


            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithIdenticalTypeNames()
    {
        const string code = """

            using AutomaticInterface;
            namespace AutomaticInterfaceExample
            {
                namespace Models1 {
                    public class Model;
                }

                namespace Models2 {
                    public class Model;
                }

                [GenerateAutomaticInterface]
                public class ModelManager
                {
                    public Models1.Model GetModel1() => null!;
                    public Models2.Model GetModel2() => null!;
                }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithNestedUsings()
    {
        const string code = """

            using AutomaticInterface;
            namespace RootNamespace
            {
                namespace Models
                {
                    public class Model;
                }

                namespace ModelManager
                {
                    using Models;

                    [GenerateAutomaticInterface]
                    public class ModelManager
                    {
                        public Model GetModel() => null!;
                    }
                }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithShadowedGlobalNamespace()
    {
        const string code = """
            using AutomaticInterface;
            using Task = System.Threading.Tasks.Task;

            namespace AutomaticInterfaceExample
            {
                namespace System.Threading.Tasks
                {
                    public class Task;
                }

                [GenerateAutomaticInterface]
                public class DemoClass
                {
                    public Task GetTask() => null!;
                }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithGlobalUsing()
    {
        const string code = """
            global using GlobalNamespace;
            using AutomaticInterface;

            namespace GlobalNamespace
            {
                public class AClass;
            }

            namespace AutomaticInterfaceExample
            {
                [GenerateAutomaticInterface]
                public class DemoClass
                {
                    public AClass GetClass() => null!;
                }
            }
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
