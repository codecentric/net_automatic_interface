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
            .Where(x => x.Id != "CS0246") // missing references are ok
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

        return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString();
    }
}
