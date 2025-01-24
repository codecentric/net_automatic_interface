using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

public static class Builder
{
    private static string InheritDoc(ISymbol source) =>
        $"/// <inheritdoc cref=\"{source.ToDisplayString().Replace('<', '{').Replace('>', '}')}\" />"; // we use inherit doc because that should be able to fetch documentation from base classes.

    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormat =
        new(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType
                | SymbolDisplayParameterOptions.IncludeParamsRefOut
                | SymbolDisplayParameterOptions.IncludeDefaultValue
                | SymbolDisplayParameterOptions.IncludeName,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
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

        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(GetGeneric(classSyntax));

        var members = typeSymbol
            .GetAllMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList();

        AddPropertiesToInterface(members, interfaceGenerator);
        AddMethodsToInterface(members, interfaceGenerator);
        AddEventsToInterface(members, interfaceGenerator);

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

    private static void AddMethodsToInterface(List<ISymbol> members, InterfaceBuilder codeGenerator)
    {
        members
            .Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => x.ContainingType.Name != nameof(Object))
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormat))
            .Select(g => g.First())
            .ToList()
            .ForEach(method => AddMethod(codeGenerator, method));
    }

    private static void AddMethod(InterfaceBuilder codeGenerator, IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var name = method.Name;

        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        method
            .Parameters.Select(x => x.ToDisplayString(FullyQualifiedDisplayFormat))
            .ToList()
            .ForEach(x => paramResult.Add(x));

        var typedArgs = method
            .TypeParameters.Select(arg =>
                (
                    arg.ToDisplayString(FullyQualifiedDisplayFormat),
                    arg.GetWhereStatement(FullyQualifiedDisplayFormat)
                )
            )
            .ToList();

        codeGenerator.AddMethodToInterface(
            name,
            returnType.ToDisplayString(FullyQualifiedDisplayFormat),
            InheritDoc(method),
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

    private static void AddEventsToInterface(List<ISymbol> members, InterfaceBuilder codeGenerator)
    {
        members
            .Where(x => x.Kind == SymbolKind.Event)
            .OfType<IEventSymbol>()
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormat))
            .Select(g => g.First())
            .ToList()
            .ForEach(evt =>
            {
                var type = evt.Type;
                var name = evt.Name;

                ActivateNullableIfNeeded(codeGenerator, type);

                codeGenerator.AddEventToInterface(
                    name,
                    type.ToDisplayString(FullyQualifiedDisplayFormat),
                    InheritDoc(evt)
                );
            });
    }

    private static void AddPropertiesToInterface(
        List<ISymbol> members,
        InterfaceBuilder interfaceGenerator
    )
    {
        members
            .Where(x => x.Kind == SymbolKind.Property)
            .OfType<IPropertySymbol>()
            .Where(x => !x.IsIndexer)
            .GroupBy(x => x.Name)
            .Select(g => g.First())
            .ToList()
            .ForEach(prop =>
            {
                var type = prop.Type;

                var name = prop.Name;
                var hasGet = prop.GetMethod?.DeclaredAccessibility == Accessibility.Public;
                var hasSet = GetSetKind(prop.SetMethod);
                var isRef = prop.ReturnsByRef;

                ActivateNullableIfNeeded(interfaceGenerator, type);

                interfaceGenerator.AddPropertyToInterface(
                    name,
                    type.ToDisplayString(FullyQualifiedDisplayFormat),
                    hasGet,
                    hasSet,
                    isRef,
                    InheritDoc(prop)
                );
            });
    }

    private static PropertySetKind GetSetKind(IMethodSymbol? setMethodSymbol)
    {
        return setMethodSymbol switch
        {
            null => PropertySetKind.NoSet,
            { IsInitOnly: true, DeclaredAccessibility: Accessibility.Public }
                => PropertySetKind.Init,
            _
                => setMethodSymbol is { DeclaredAccessibility: Accessibility.Public }
                    ? PropertySetKind.Always
                    : PropertySetKind.NoSet,
        };
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
            SyntaxKind.SingleLineDocumentationCommentTrivia,
        ];

        var trivia = classSyntax
            .GetLeadingTrivia()
            .Where(x => docSyntax.Contains(x.Kind()))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

        return trivia.ToFullString().Trim();
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
