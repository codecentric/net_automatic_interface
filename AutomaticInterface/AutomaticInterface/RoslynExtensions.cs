using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface
{
    /// <summary>
    /// Source: https://github.com/dominikjeske/Samples/blob/main/SourceGenerators/HomeCenter.SourceGenerators/Extensions/RoslynExtensions.cs
    /// </summary>
    public static class RoslynExtensions
    {
        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            return type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers());
        }

        public static string GetClassName(this ClassDeclarationSyntax proxy)
        {
            return proxy.Identifier.Text;
        }

        /// <summary>
        /// Thanks to https://www.codeproject.com/Articles/871704/Roslyn-Code-Analysis-in-Easy-Samples-Part-2
        /// </summary>
        public static string GetWhereStatement(
            this ITypeParameterSymbol typeParameterSymbol,
            SymbolDisplayFormat typeDisplayFormat,
            List<string> generatedInterfaceNames
        )
        {
            var result = $"where {typeParameterSymbol.Name} : ";

            var constraints = new List<string>();

            if (typeParameterSymbol.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }

            if (typeParameterSymbol.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            if (typeParameterSymbol.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(
                typeParameterSymbol.ConstraintTypes.Select(t =>
                    t.ToDisplayString(typeDisplayFormat, generatedInterfaceNames)
                )
            );

            // The new() constraint must be last
            if (typeParameterSymbol.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count == 0)
            {
                return "";
            }

            result += string.Join(", ", constraints);

            return result;
        }

        public static string ToDisplayString(
            this IParameterSymbol symbol,
            SymbolDisplayFormat displayFormat,
            List<string> generatedInterfaceNames
        ) => ToDisplayString((ISymbol)symbol, displayFormat, generatedInterfaceNames);

        public static string ToDisplayString(
            this ITypeSymbol symbol,
            SymbolDisplayFormat displayFormat,
            List<string> generatedInterfaceNames
        ) => ToDisplayString((ISymbol)symbol, displayFormat, generatedInterfaceNames);

        /// <summary>
        /// Wraps <see cref="ITypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat?)" /> with custom resolution for generated types
        /// </summary>
        private static string ToDisplayString(
            this ISymbol symbol,
            SymbolDisplayFormat displayFormat,
            List<string> generatedInterfaceNames
        )
        {
            var displayStringBuilder = new StringBuilder();

            var displayParts = GetDisplayParts(symbol, displayFormat);

            foreach (var part in displayParts)
            {
                if (part.Kind == SymbolDisplayPartKind.ErrorTypeName)
                {
                    var unrecognisedName = part.ToString();

                    var inferredName = ReplaceWithInferredInterfaceName(
                        unrecognisedName,
                        generatedInterfaceNames
                    );

                    displayStringBuilder.Append(inferredName);
                }
                else
                {
                    displayStringBuilder.Append(part);
                }
            }

            return displayStringBuilder.ToString();
        }

        /// <summary>
        /// The same as <see cref="ISymbol.ToDisplayParts"/> but with adjacent SymbolDisplayParts merged into qualified type references, e.g. [Parent, ., Child] => Parent.Child
        /// </summary>
        private static IEnumerable<SymbolDisplayPart> GetDisplayParts(
            ISymbol symbol,
            SymbolDisplayFormat displayFormat
        )
        {
            var cache = new List<SymbolDisplayPart>();

            foreach (var part in symbol.ToDisplayParts(displayFormat))
            {
                if (cache.Count == 0)
                {
                    cache.Add(part);
                    continue;
                }

                var previousPart = cache.Last();

                if (
                    IsPartQualificationPunctuation(previousPart)
                    ^ IsPartQualificationPunctuation(part)
                )
                {
                    cache.Add(part);
                }
                else
                {
                    yield return CombineQualifiedTypeParts(cache);
                    cache.Clear();
                    cache.Add(part);
                }
            }

            if (cache.Count > 0)
            {
                yield return CombineQualifiedTypeParts(cache);
            }

            static SymbolDisplayPart CombineQualifiedTypeParts(
                ICollection<SymbolDisplayPart> qualifiedTypeParts
            )
            {
                var qualifiedType = qualifiedTypeParts.Last();

                return qualifiedTypeParts.Count == 1
                    ? qualifiedType
                    : new SymbolDisplayPart(
                        qualifiedType.Kind,
                        qualifiedType.Symbol,
                        string.Join("", qualifiedTypeParts)
                    );
            }

            static bool IsPartQualificationPunctuation(SymbolDisplayPart part) =>
                part.ToString() is "." or "::";
        }

        private static string ReplaceWithInferredInterfaceName(
            string unrecognisedName,
            List<string> generatedInterfaceNames
        )
        {
            var matches = generatedInterfaceNames
                .Where(name => Regex.IsMatch(name, $"[.:]{Regex.Escape(unrecognisedName)}$"))
                .ToList();

            if (matches.Count != 1)
            {
                // Either there's no match or an ambiguous match - we can't safely infer the interface name.
                // This is very much a "best effort" approach - if there are two interfaces with the same name,
                // there's no obvious way to work out which one the symbol is referring to.
                return unrecognisedName;
            }

            return matches[0];
        }
    }
}
