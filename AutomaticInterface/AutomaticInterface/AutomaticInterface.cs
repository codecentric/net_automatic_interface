using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

[Generator]
public class AutomaticInterfaceGenerator : IIncrementalGenerator
{
    private const string DefaultAttributeName = "GenerateAutomaticInterface";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context
            .SyntaxProvider.CreateSyntaxProvider(CouldBeClassAsync, Transform)
            .Where(type => type is not null)
            .Collect();

        context.RegisterSourceOutput(classes, GenerateCode);
    }

    private void GenerateCode(
        SourceProductionContext context,
        ImmutableArray<ITypeSymbol?> enumerations
    )
    {
        if (enumerations.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var type in enumerations)
        {
            if (type is null)
            {
                continue;
            }

            var typeNamespace = type.ContainingNamespace.IsGlobalNamespace
                ? $"${Guid.NewGuid().ToString()}"
                : $"{type.ContainingNamespace}";

            var code = Builder.BuildInterfaceFor(type);

            context.AddSource(typeNamespace, code);
        }
    }

    private static ITypeSymbol? Transform(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        var attributeSyntax = (AttributeSyntax)context.Node;
        if (attributeSyntax.Parent?.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        var type =
            context.SemanticModel.GetDeclaredSymbol(
                classDeclaration,
                cancellationToken: cancellationToken
            ) as ITypeSymbol;
        return type;
    }

    private static bool CouldBeClassAsync(
        SyntaxNode syntaxNode,
        CancellationToken cancellationToken
    )
    {
        if (syntaxNode is not AttributeSyntax attribute)
        {
            return false;
        }

        var name = ExtractName(attribute.Name);

        return name is DefaultAttributeName;
    }

    private static string? ExtractName(NameSyntax? name)
    {
        return name switch
        {
            SimpleNameSyntax ins => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _ => null
        };
    }
}
