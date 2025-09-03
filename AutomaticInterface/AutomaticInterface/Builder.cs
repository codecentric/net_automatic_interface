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
        $"/// <inheritdoc cref=\"{source.ToDisplayString().Replace('<', '{').Replace('>', '}').Replace("params ", "")}\" />"; // we use inherit doc because that should be able to fetch documentation from base classes.

    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeContainingType,
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

    /// <summary>
    /// We do need to be able to group shadowing and new methods/events into a single entry, hence this is missing SymbolDisplayMemberOptions.IncludeContainingType
    /// </summary>
    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormatForGrouping = new(
        genericsOptions: FullyQualifiedDisplayFormat.GenericsOptions,
        memberOptions: FullyQualifiedDisplayFormat.MemberOptions
            & ~SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: FullyQualifiedDisplayFormat.ParameterOptions,
        typeQualificationStyle: FullyQualifiedDisplayFormat.TypeQualificationStyle,
        globalNamespaceStyle: FullyQualifiedDisplayFormat.GlobalNamespaceStyle,
        miscellaneousOptions: FullyQualifiedDisplayFormat.MiscellaneousOptions
    );

    public static string? GetInterfaceNameFor(ITypeSymbol typeSymbol)
    {
        if (
            typeSymbol.DeclaringSyntaxReferences.First().GetSyntax()
                is not ClassDeclarationSyntax classSyntax
            || typeSymbol is not INamedTypeSymbol
        )
        {
            return null;
        }
        var symbolDetails = GetSymbolDetails(typeSymbol, classSyntax);

        return $"global::{symbolDetails.NamespaceName}.{symbolDetails.InterfaceName}";
    }

    /// <param name="typeSymbol">The symbol from which the interface will be built</param>
    /// <param name="generatedInterfaceNames">A list of interface names that will be generated in this session. Used to resolve type references to interfaces that haven't yet been generated</param>
    /// <returns></returns>
    public static string BuildInterfaceFor(
        ITypeSymbol typeSymbol,
        List<string> generatedInterfaceNames
    )
    {
        if (
            typeSymbol.DeclaringSyntaxReferences.First().GetSyntax()
                is not ClassDeclarationSyntax classSyntax
            || typeSymbol is not INamedTypeSymbol namedTypeSymbol
        )
        {
            return string.Empty;
        }

        var symbolDetails = GetSymbolDetails(typeSymbol, classSyntax);
        var interfaceGenerator = new InterfaceBuilder(
            symbolDetails.NamespaceName,
            symbolDetails.InterfaceName,
            symbolDetails.AccessLevel
        );

        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(
            GetGeneric(classSyntax, namedTypeSymbol, generatedInterfaceNames)
        );

        var members = typeSymbol
            .GetAllMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => !x.IsStatic)
            .Where(x => !HasIgnoreAttribute(x))
            .ToList();

        AddPropertiesToInterface(members, interfaceGenerator, generatedInterfaceNames);
        AddMethodsToInterface(members, interfaceGenerator, generatedInterfaceNames);
        AddEventsToInterface(members, interfaceGenerator, generatedInterfaceNames);

        var generatedCode = interfaceGenerator.Build();

        return generatedCode;
    }

    private static GeneratedSymbolDetails GetSymbolDetails(
        ITypeSymbol typeSymbol,
        ClassDeclarationSyntax classSyntax
    )
    {
        var generationAttribute = typeSymbol
            .GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass != null
                && x.AttributeClass.Name.Contains(AutomaticInterfaceGenerator.DefaultAttributeName)
            );

        return new GeneratedSymbolDetails(generationAttribute, typeSymbol, classSyntax);
    }

    private static void AddMethodsToInterface(
        List<ISymbol> members,
        InterfaceBuilder codeGenerator,
        List<string> generatedInterfaceNames
    )
    {
        members
            .Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => x.ContainingType.Name != nameof(Object))
            .Where(x => !HasIgnoreAttribute(x))
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
            .Select(g => g.First())
            .ToList()
            .ForEach(method => AddMethod(codeGenerator, method, generatedInterfaceNames));
    }

    private static void AddMethod(
        InterfaceBuilder codeGenerator,
        IMethodSymbol method,
        List<string> generatedInterfaceNames
    )
    {
        var returnType = method.ReturnType;
        var name = method.Name;

        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        method
            .Parameters.Select(x =>
                x.ToDisplayString(FullyQualifiedDisplayFormat, generatedInterfaceNames)
            )
            .ToList()
            .ForEach(x => paramResult.Add(x));

        var typedArgs = method
            .TypeParameters.Select(arg =>
                (
                    arg.ToDisplayString(FullyQualifiedDisplayFormat),
                    arg.GetWhereStatement(FullyQualifiedDisplayFormat, generatedInterfaceNames)
                )
            )
            .ToList();

        codeGenerator.AddMethodToInterface(
            name,
            returnType.ToDisplayString(FullyQualifiedDisplayFormat, generatedInterfaceNames),
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

    private static void AddEventsToInterface(
        List<ISymbol> members,
        InterfaceBuilder codeGenerator,
        List<string> generatedInterfaceNames
    )
    {
        members
            .Where(x => x.Kind == SymbolKind.Event)
            .OfType<IEventSymbol>()
            .GroupBy(x => x.ToDisplayString(FullyQualifiedDisplayFormatForGrouping))
            .Select(g => g.First())
            .ToList()
            .ForEach(evt =>
            {
                var type = evt.Type;
                var name = evt.Name;

                ActivateNullableIfNeeded(codeGenerator, type);

                codeGenerator.AddEventToInterface(
                    name,
                    type.ToDisplayString(FullyQualifiedDisplayFormat, generatedInterfaceNames),
                    InheritDoc(evt)
                );
            });
    }

    private static void AddPropertiesToInterface(
        List<ISymbol> members,
        InterfaceBuilder interfaceGenerator,
        List<string> generatedInterfaceNames
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
                    type.ToDisplayString(FullyQualifiedDisplayFormat, generatedInterfaceNames),
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
            { IsInitOnly: true, DeclaredAccessibility: Accessibility.Public } =>
                PropertySetKind.Init,
            _ => setMethodSymbol is { DeclaredAccessibility: Accessibility.Public }
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

    private static string GetGeneric(
        TypeDeclarationSyntax classSyntax,
        INamedTypeSymbol typeSymbol,
        List<string> generatedInterfaceNames
    )
    {
        var whereStatements = typeSymbol
            .TypeParameters.Select(typeParameter =>
                typeParameter.GetWhereStatement(
                    FullyQualifiedDisplayFormat,
                    generatedInterfaceNames
                )
            )
            .Where(constraint => !string.IsNullOrEmpty(constraint));

        return $"{classSyntax.TypeParameterList?.ToFullString().Trim()} {string.Join(" ", whereStatements)}".Trim();
    }
}
