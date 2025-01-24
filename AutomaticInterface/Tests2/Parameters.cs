namespace Tests2;

public class Parameters
{
    [Test]
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

        var generateCode = Common.GenerateCode(code);
        await Verify(generateCode);
    }
}
