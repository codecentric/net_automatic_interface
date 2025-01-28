namespace Tests.Enums;

public class Enums
{
    [Fact]
    public async Task WorksWithByteEnums()
    {
        const string code = """

            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public enum EnumWithByteType : byte { A = 1, B = 2, C = 3 };

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public void MethodWithDefaultParameter(EnumWithByteType a = EnumWithByteType.B) { }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithByteEnumsAsReturnType()
    {
        const string code = """

            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public enum EnumWithByteType : byte { A = 1, B = 2, C = 3 };

            [GenerateAutomaticInterface]
            public class DemoClass
            {
                public EnumWithByteType MethodWithDefaultParameter(EnumWithByteType a = EnumWithByteType.B) { return a; }
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }

    [Fact]
    public async Task WorksWithByteEnumsProperties()
    {
        const string code = """

            using AutomaticInterface;
            using System;

            namespace AutomaticInterfaceExample;

            public enum EnumWithByteType : byte { A = 1, B = 2, C = 3 };

            [GenerateAutomaticInterface]
            public class DemoClass
            {
               public EnumWithByteType SomeProperty { get; set; }  
            }

            """;

        await Verify(Infrastructure.GenerateCode(code));
    }
}
