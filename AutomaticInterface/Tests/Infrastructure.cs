using AutomaticInterface;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tests;

public static class Infrastructure
{
    public static string GenerateCode(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTests",
            [syntaxTree],
            references,
            new(OutputKind.DynamicallyLinkedLibrary)
        );

        // defensive check to avoid errors in the source text, which can happen with manually written code
        var sourceDiagnostics = compilation.GetDiagnostics();
        var sourceErrors = sourceDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Where(x => x.Id != "CS0246" && x.Id != "CS0234") // missing references are ok
            .ToList();

        Assert.Empty(sourceErrors);

        var generator = new AutomaticInterfaceGenerator();

        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics
            );

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        Assert.Empty(errors);

        // The first syntax tree is the input code, the second two are the two generated attribute classes, and the rest is the generated code.
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            outputCompilation.SyntaxTrees.Skip(3)
        );
    }
}
