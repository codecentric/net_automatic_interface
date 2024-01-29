using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

[Generator]
public class AutomaticInterfaceGenerator : IIncrementalGenerator
{
    private GeneratorOptions options = new();
    public const string DefaultAttributeName = "GenerateAutomaticInterface";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // todo nullable as in void Test(string? x);
        // todo nullable event
        // todo nullable property
        var classes = context
            .SyntaxProvider.CreateSyntaxProvider(CouldBeClassAsync, Transform)
            .Where(type => type is not null)
            .Collect();

        context.RegisterSourceOutput(classes, GenerateCode);

        options = GetGeneratorOptions(context);
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

            var code = Builder.BuildInterfaceFor(options, type);

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

        return name is DefaultAttributeName or "GenerateAutomaticInterfaceAttribute";
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

    private static GeneratorOptions GetGeneratorOptions(
        IncrementalGeneratorInitializationContext context
    )
    {
        var opts = new GeneratorOptions();
        context.AnalyzerConfigOptionsProvider.Select(
            (options, _) =>
            {
                var enableLogging =
                    options.GlobalOptions.TryGetValue(
                        "build_property.AutomaticInterface_Logging_Enable",
                        out var loggerEnabledValue
                    ) && IsFeatureEnabled(loggerEnabledValue);

                options.GlobalOptions.TryGetValue(
                    "build_property.AutomaticInterface_Logging_Path",
                    out var logPath
                );

                options.GlobalOptions.TryGetValue(
                    "build_property.AutomaticInterface_AttributeName",
                    out var attributeName
                );

                opts.LoggerEnabled = enableLogging;
                opts.LoggerPath = logPath ?? "";
                opts.AttributeName = attributeName ?? DefaultAttributeName;
                return opts;
            }
        );

        return opts;
    }

    private static bool IsFeatureEnabled(string counterEnabledValue)
    {
        return StringComparer.OrdinalIgnoreCase.Equals("enable", counterEnabledValue)
            || StringComparer.OrdinalIgnoreCase.Equals("enabled", counterEnabledValue)
            || StringComparer.OrdinalIgnoreCase.Equals("true", counterEnabledValue);
    }
}

public record GeneratorOptions
{
    public bool LoggerEnabled { get; set; }
    public string LoggerPath { get; set; } = "";
    public string AttributeName { get; set; } = AutomaticInterfaceGenerator.DefaultAttributeName;
}
