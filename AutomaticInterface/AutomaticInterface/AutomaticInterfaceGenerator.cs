using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

[Generator]
public class AutomaticInterfaceGenerator : IIncrementalGenerator
{
    public const string DefaultAttributeName = "GenerateAutomaticInterface";
    public const string IgnoreAutomaticInterfaceAttributeName = "IgnoreAutomaticInterface";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterDefaultAttribute();
        context.RegisterIgnoreAttribute();

        var classes = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                $"AutomaticInterface.{DefaultAttributeName}Attribute",
                (node, _) => node is ClassDeclarationSyntax,
                (ctx, _) => (ITypeSymbol)ctx.TargetSymbol
            )
            .Collect();

        context.RegisterSourceOutput(classes, GenerateCode);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        ImmutableArray<ITypeSymbol> enumerations
    )
    {
        if (enumerations.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var type in enumerations)
        {
            var typeNamespace = type.ContainingNamespace.IsGlobalNamespace
                ? $"${Guid.NewGuid()}"
                : $"{type.ContainingNamespace}";

            var code = Builder.BuildInterfaceFor(type);

            var hintName = $"{typeNamespace}.I{type.Name}";
            context.AddSource(hintName, code);
        }
    }
}
