using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using AutomaticInterfaceAttribute;
using AutomaticInterface;
using System.Reflection;

namespace Tests
{
    public static class GeneratorTestFactory
    {
        public static ImmutableArray<Diagnostic> RunGenerator(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8));

            //var parseOptions = TestOptions.Regular;
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Default);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateAutomaticInterfaceAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.2.2.0").Location)
        };

            Compilation compilation = CSharpCompilation.Create("testgenerator", new[] { syntaxTree }, references, compilationOptions).WithReferences(references);
            var diagnostics = compilation.GetDiagnostics();
            if (!VerifyDiagnostics(diagnostics, new[] { "CS0012", "CS0616", "CS0246" }))
            {
                // this will make the test fail, check the input source code!
                return diagnostics;
            }

            var generator = new AutomaticInterfaceGenerator();
            ISourceGenerator[] generators = { generator};            
            var driver = CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            return generatorDiagnostics;
        }

        public static bool VerifyDiagnostics(ImmutableArray<Diagnostic> actual, string[] expected)
        {
            return actual.Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.Id.ToString())
                    .All(id => expected.Contains(id));
        }
    }
}