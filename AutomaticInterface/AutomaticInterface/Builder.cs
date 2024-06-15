using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

public static class Builder
{
    private const string InheritDoc = "/// <inheritdoc />"; // we use inherit doc because that should be able to fetch documentation from base classes.

    private static readonly SymbolDisplayFormat MethodSignatureDisplayFormat =
        new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType
                | SymbolDisplayParameterOptions.IncludeParamsRefOut
        );

    public static string BuildInterfaceFor(ITypeSymbol typeSymbol)
    {
        if (
            typeSymbol.DeclaringSyntaxReferences.First().GetSyntax()
            is not ClassDeclarationSyntax classSyntax
        )
        {
            return string.Empty;
        }

        var namespaceName = GetNameSpace(typeSymbol);

        var interfaceName = $"I{classSyntax.GetClassName()}";

        var interfaceGenerator = new InterfaceBuilder(namespaceName, interfaceName);

        interfaceGenerator.AddUsings(GetUsings(typeSymbol));
        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(GetGeneric(classSyntax));
        AddPropertiesToInterface(typeSymbol, interfaceGenerator);
        AddMethodsToInterface(typeSymbol, interfaceGenerator);
        AddEventsToInterface(typeSymbol, interfaceGenerator);

        var generatedCode = interfaceGenerator.Build();

        return generatedCode;
    }

    private static string GetNameSpace(ISymbol typeSymbol)
    {
        var generationAttribute = typeSymbol
            .GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass != null
                && x.AttributeClass.Name.Contains(AutomaticInterfaceGenerator.DefaultAttributeName)
            );

        if (generationAttribute == null)
        {
            return typeSymbol.ContainingNamespace.ToDisplayString();
        }
        var customNs = generationAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();

        return string.IsNullOrWhiteSpace(customNs)
            ? typeSymbol.ContainingNamespace.ToDisplayString()
            : customNs!;
    }

    private static void AddMethodsToInterface(
        ITypeSymbol classSymbol,
        InterfaceBuilder codeGenerator
    )
    {
        classSymbol
            .GetAllMembers()
            .Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => !x.IsStatic)
            .Where(x => x.ContainingType.Name != nameof(Object))
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.ToDisplayString(MethodSignatureDisplayFormat))
            .Select(g => g.First())
            .ToList()
            .ForEach(method =>
            {
                AddMethod(codeGenerator, method);
            });
    }

    private static void AddMethod(InterfaceBuilder codeGenerator, IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var name = method.Name;

        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        method.Parameters.Select(GetMethodSignature).ToList().ForEach(x => paramResult.Add(x));

        var typedArgs = method
            .TypeParameters.Select(arg => (arg.ToDisplayString(), arg.GetWhereStatement()))
            .ToList();

        codeGenerator.AddMethodToInterface(
            name,
            returnType.ToDisplayString(),
            InheritDoc,
            paramResult,
            typedArgs
        );
    }

    private static void ActivateNullableIfNeeded(
        InterfaceBuilder codeGenerator,
        ITypeSymbol typeSymbol
    )
    {
        if (IsNullable(typeSymbol))
        {
            codeGenerator.HasNullable = true;
        }
    }

    private static void ActivateNullableIfNeeded(
        InterfaceBuilder codeGenerator,
        IMethodSymbol method
    )
    {
        var hasNullableParameter = method.Parameters.Any(x => IsNullable(x.Type));

        var hasNullableReturn = IsNullable(method.ReturnType);

        if (hasNullableParameter || hasNullableReturn)
        {
            codeGenerator.HasNullable = true;
        }
    }

    private static bool IsNullable(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (typeSymbol is not INamedTypeSymbol named)
        {
            return false;
        }

        foreach (var param in named.TypeArguments)
        {
            if (IsNullable(param))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddEventsToInterface(
        ITypeSymbol classSymbol,
        InterfaceBuilder codeGenerator
    )
    {
        classSymbol
            .GetAllMembers()
            .Where(x => x.Kind == SymbolKind.Event)
            .OfType<IEventSymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList()
            .ForEach(evt =>
            {
                var type = evt.Type;
                var name = evt.Name;

                ActivateNullableIfNeeded(codeGenerator, type);

                codeGenerator.AddEventToInterface(name, type.ToDisplayString(), InheritDoc);
            });
    }

    private static string GetMethodSignature(IParameterSymbol x)
    {
        var name = GetMethodName(x);
        var refKindText = GetRefKind(x);
        var optionalValue = GetMethodOptionalValue(x);

        return $"{refKindText}{x.Type.ToDisplayString()} {name}{optionalValue}";
    }

    private static string GetMethodOptionalValue(IParameterSymbol x)
    {
        if (!x.HasExplicitDefaultValue)
        {
            return string.Empty;
        }

        return x.ExplicitDefaultValue switch
        {
            string => $" = \"{x.ExplicitDefaultValue}\"",
            bool value => $" = {(value ? "true" : "false")}",
            // struct
            null when x.Type.IsValueType => $" = default({x.Type})",
            null => " = null",
            _ => $" = {x.ExplicitDefaultValue}"
        };
    }

    private static string GetMethodName(IParameterSymbol x)
    {
        var syntaxReference = x.DeclaringSyntaxReferences.FirstOrDefault();

        return syntaxReference != null
            ? ((ParameterSyntax)syntaxReference.GetSyntax()).Identifier.Text
            : x.Name;
    }

    private static string GetRefKind(IParameterSymbol x)
    {
        return x.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            // Not sure why RefReadOnly and In both has Enum index 3.
            // RefKind.RefReadOnly => "ref readonly ",
            _ => string.Empty,
        };
    }

    private static void AddPropertiesToInterface(
        ITypeSymbol classSymbol,
        InterfaceBuilder interfaceGenerator
    )
    {
        classSymbol
            .GetAllMembers()
            .Where(x => x.Kind == SymbolKind.Property)
            .OfType<IPropertySymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !x.IsIndexer)
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.Name)
            .Select(g => g.First())
            .ToList()
            .ForEach(prop =>
            {
                var type = prop.Type;

                var name = prop.Name;
                var hasGet = prop.GetMethod?.DeclaredAccessibility == Accessibility.Public;
                var hasSet = prop.SetMethod?.DeclaredAccessibility == Accessibility.Public;
                var isRef = prop.ReturnsByRef;

                ActivateNullableIfNeeded(interfaceGenerator, type);

                interfaceGenerator.AddPropertyToInterface(
                    name,
                    type.ToDisplayString(),
                    hasGet,
                    hasSet,
                    isRef,
                    InheritDoc
                );
            });
    }

    private static bool HasIgnoreAttribute(ISymbol x)
    {
        return x.GetAttributes()
            .Any(a =>
                a.AttributeClass != null
                && a.AttributeClass.Name.Contains(
                    AutomaticInterfaceGenerator.IgnoreAutomaticInterfaceAttributeName
                )
            );
    }

    private static string GetDocumentationForClass(CSharpSyntaxNode classSyntax)
    {
        if (!classSyntax.HasLeadingTrivia)
        {
            // no documentation
            return string.Empty;
        }

        SyntaxKind[] docSyntax =
        [
            SyntaxKind.DocumentationCommentExteriorTrivia,
            SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia,
            SyntaxKind.SingleLineDocumentationCommentTrivia
        ];

        var trivia = classSyntax
            .GetLeadingTrivia()
            .Where(x => docSyntax.Contains(x.Kind()))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

        return trivia.ToFullString().Trim();
    }

    private static IEnumerable<string> GetUsings(ISymbol classSymbol)
    {
        var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
        foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            allUsings = syntaxRef
                .GetSyntax()
                .Ancestors(false)
                .Aggregate(
                    allUsings,
                    (current, parent) =>
                        parent switch
                        {
                            NamespaceDeclarationSyntax ndSyntax
                                => current.AddRange(ndSyntax.Usings),
                            CompilationUnitSyntax cuSyntax => current.AddRange(cuSyntax.Usings),
                            _ => current
                        }
                );
        }

        return [.. allUsings.Select(x => x.ToString())];
    }

    private static string GetGeneric(TypeDeclarationSyntax classSyntax)
    {
        if (classSyntax.TypeParameterList?.Parameters.Count == 0)
        {
            return string.Empty;
        }

        var formattedGeneric =
            $"{classSyntax.TypeParameterList?.ToFullString().Trim()} {classSyntax.ConstraintClauses}".Trim();

        return formattedGeneric;
    }
}
