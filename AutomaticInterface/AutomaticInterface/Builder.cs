using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private static readonly SymbolDisplayFormat FullyQualifiedDisplayFormatForGrouping =
        new(
            genericsOptions: FullyQualifiedDisplayFormat.GenericsOptions,
            memberOptions: FullyQualifiedDisplayFormat.MemberOptions
                & ~SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions: FullyQualifiedDisplayFormat.ParameterOptions,
            typeQualificationStyle: FullyQualifiedDisplayFormat.TypeQualificationStyle,
            globalNamespaceStyle: FullyQualifiedDisplayFormat.GlobalNamespaceStyle,
            miscellaneousOptions: FullyQualifiedDisplayFormat.MiscellaneousOptions
        );

    public static string BuildInterfaceFor(ITypeSymbol typeSymbol)
    {
        if (
            typeSymbol.DeclaringSyntaxReferences.First().GetSyntax()
                is not ClassDeclarationSyntax classSyntax
            || typeSymbol is not INamedTypeSymbol namedTypeSymbol
        )
        {
            return string.Empty;
        }
        var generationAttribute = GetGenerationAttribute(typeSymbol);
        var asInternal = GetAsInternal(generationAttribute);
        var symbolDetails = GetSymbolDetails(typeSymbol, classSyntax);
        var interfaceGenerator = new InterfaceBuilder(
            symbolDetails.NamespaceName,
            symbolDetails.InterfaceName,
            asInternal
        );

        interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
        interfaceGenerator.AddGeneric(GetGeneric(classSyntax, namedTypeSymbol));

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

    private static AttributeData? GetGenerationAttribute(ISymbol typeSymbol)
    {
        return typeSymbol
            .GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass != null
                && x.AttributeClass.Name.Contains(AutomaticInterfaceGenerator.DefaultAttributeName)
            );
    }

    private static string GetNameSpace(ISymbol typeSymbol, AttributeData? generationAttribute)
    {
        if (generationAttribute == null)
        {
            return typeSymbol.ContainingNamespace.ToDisplayString();
        }

        var customNs = generationAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();

        return string.IsNullOrWhiteSpace(customNs)
            ? typeSymbol.ContainingNamespace.ToDisplayString()
            : customNs!;
    }

    private static bool GetAsInternal(AttributeData? generationAttribute)
    {
        if (generationAttribute == null)
        {
            return false;
        }

        var asInternal = (bool?)
            generationAttribute.ConstructorArguments.Skip(2).FirstOrDefault().Value;

        return asInternal.GetValueOrDefault();
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

    private static void AddMethodsToInterface(List<ISymbol> members, InterfaceBuilder codeGenerator)
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
            .ForEach(method => AddMethod(codeGenerator, method));
    }

    private static void AddMethod(InterfaceBuilder codeGenerator, IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var name = method.Name;

        ActivateNullableIfNeeded(codeGenerator, method);

        var paramResult = new HashSet<string>();
        method
            .Parameters.Select(p => GetParameterDisplayString(p, codeGenerator.HasNullable))
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

    private static string GetParameterDisplayString(
        IParameterSymbol param,
        bool nullableContextEnabled
    )
    {
        var paramParts = param.ToDisplayParts(FullyQualifiedDisplayFormat);
        var typeSb = new StringBuilder();
        var restSb = new StringBuilder();
        var isInsideType = true;
        // The part before the first space is the parameter type
        foreach (var part in paramParts)
        {
            if (isInsideType && part.Kind == SymbolDisplayPartKind.Space)
            {
                isInsideType = false;
            }
            if (isInsideType)
            {
                typeSb.Append(part.ToString());
            }
            else
            {
                restSb.Append(part.ToString());
            }
        }
        // If this parameter has default value null and we're enabling the nullable context, we need to force the nullable annotation if there isn't one already
        if (
            param.HasExplicitDefaultValue
            && param.ExplicitDefaultValue is null
            && param.NullableAnnotation != NullableAnnotation.Annotated
            && param.Type.IsReferenceType
            && nullableContextEnabled
        )
        {
            typeSb.Append('?');
        }
        return typeSb.Append(restSb).ToString();
    }

    private static void AddEventsToInterface(List<ISymbol> members, InterfaceBuilder codeGenerator)
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

    private static string GetGeneric(TypeDeclarationSyntax classSyntax, INamedTypeSymbol typeSymbol)
    {
        var whereStatements = typeSymbol
            .TypeParameters.Select(typeParameter =>
                typeParameter.GetWhereStatement(FullyQualifiedDisplayFormat)
            )
            .Where(constraint => !string.IsNullOrEmpty(constraint));

        return $"{classSyntax.TypeParameterList?.ToFullString().Trim()} {string.Join(" ", whereStatements)}".Trim();
    }
}
