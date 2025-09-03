using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

[Generator]
public class AutomaticInterfaceGenerator : IIncrementalGenerator
{
    public const string DefaultAttributeName = "GenerateAutomaticInterface";
    public const string IgnoreAutomaticInterfaceAttributeName = "IgnoreAutomaticInterface";
    public const string NamespaceParameterName = "namespaceName";
    public const string InterfaceParameterName = "interfaceName";
    public const string AsInternalParameterName = "asInternal";

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

        var generatedInterfaceNames = enumerations
            .Select(Builder.GetInterfaceNameFor)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();

        foreach (var type in enumerations)
        {
            var typeNamespace = type.ContainingNamespace.IsGlobalNamespace
                ? $"${Guid.NewGuid()}"
                : $"{type.ContainingNamespace}";

            var code = Builder.BuildInterfaceFor(type, generatedInterfaceNames);

            var hintName = $"{typeNamespace}.I{type.Name}";
            context.AddSource(hintName, code);
        }
    }
}
