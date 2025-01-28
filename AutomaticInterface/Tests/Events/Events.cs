namespace Tests.Events;

public class Events
{
    [Fact]
    public async Task CopiesEventsToInterface()
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
                    public event EventHandler ShapeChanged;  // included

                    private event EventHandler ShapeChanged2; // ignored because not public
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task NullableEvent()
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
                    public event EventHandler? ShapeChangedNullable;
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task NullableEvent2()
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
                    public event EventHandler<string?> ShapeChangedNullable;
                }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithEventOverrides()
    {
        const string code = """

            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public virtual event EventHandler AnEvent;
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public override event EventHandler AnEvent;
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithEventShadowing()
    {
        const string code = """

            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public class BaseClass
            {
                public event EventHandler AnEvent;
            }

            [GenerateAutomaticInterface]
            public class DemoClass : BaseClass
            {
                public new event EventHandler AnEvent;
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
