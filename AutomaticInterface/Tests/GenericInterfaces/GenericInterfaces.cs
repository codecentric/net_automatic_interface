namespace Tests.GenericInterfaces;

public class GenericInterfaces
{
    [Fact]
    public async Task MakesGenericInterface()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample
            {
                /// <summary>
                /// Bla bla
                /// </summary>
                [GenerateAutomaticInterface]
                class DemoClass<T,U> where T:class
                {
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task MakesGenericInterfaceWithInterfaceTypeConstraints()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            public interface IDemoModel;

            /// <summary>
            /// Bla bla
            /// </summary>
            [GenerateAutomaticInterface]
            public class DemoClass<T, U, V>
                where T: class, IDemoModel 
                where U: struct, IDemoModel
                where V: notnull, IDemoModel;
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task MakesGenericInterfaceWithClassTypeConstraints()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            public class DemoModel;

            /// <summary>
            /// Bla bla
            /// </summary>
            [GenerateAutomaticInterface]
            public class DemoClass<T, U>
                where T: DemoModel;
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task MakesGenericInterfaceWithDependentTypeConstraints()
    {
        const string code = """

            using AutomaticInterface;

            namespace AutomaticInterfaceExample;

            /// <summary>
            /// Bla bla
            /// </summary>
            [GenerateAutomaticInterface]
            public class DemoClass<T, U, V>
                where T: U
                where V: List<T>;
            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
