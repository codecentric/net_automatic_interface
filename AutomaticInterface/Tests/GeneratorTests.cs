using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void TestNoAttribute()
        {
            // simply ignore
            var source = @"
class C { }
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error));
        }


        [Fact]
        public void GeneratesEmptyInterface()
        {

            var source = @"
using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass
    {
    }
}
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void GeneratesStringPropertyInterface()
        {

            var source = @"
using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass
    {
        public string Hello { get; set; }
    }
}
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void GeneratesStringPropertySetOnlyInterface()
        {

            var source = @"
using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass
    {
        string _hello;
        public string Hello { set => _hello = value; }
    }
}
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error), string.Join("\n", generatorDiagnostics));
        }

        [Fact]
        public void GeneratesStringPropertyGetOnlyInterface()
        {

            var source = @"
using AutomaticInterfaceAttribute;

namespace AutomaticInterfaceExample
{
    [GenerateAutomaticInterface]
    class DemoClass
    {
        public string Hello { get; }
    }
}
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error));
        }
    }
}
