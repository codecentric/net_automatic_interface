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
        )
        {
            var parameterDisplayString = symbol.ToDisplayString(displayFormat);

            var parameterTypeDisplayString = symbol.Type.ToDisplayString(
                displayFormat,
                generatedInterfaceNames
            );

            // Replace the type part of the parameter definition - we don't try to generate the whole parameter definition
            // as it's quite complex - we leave that to Roslyn.
            return ParameterTypeMatcher.Replace(parameterDisplayString, parameterTypeDisplayString);
        }

        /// <summary>
        /// Matches the type part of a parameter definition (Type name[ = defaultValue])
        /// </summary>
        private static readonly Regex ParameterTypeMatcher =
            new(@"[^\s=]+(?=\s\S+(\s?=\s?\S+)?$)", RegexOptions.Compiled);

        /// <summary>
        /// Wraps <see cref="ITypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat?)" /> with custom resolution for generated types
        /// </summary>
        /// <returns></returns>
        public static string ToDisplayString(
            this ITypeSymbol symbol,
            SymbolDisplayFormat displayFormat,
            List<string> generatedInterfaceNames
        )
        {
            var builder = new StringBuilder();

            AppendTypeSymbolDisplayString(symbol, displayFormat, generatedInterfaceNames, builder);

            return builder.ToString();
        }

        private static void AppendTypeSymbolDisplayString(
            ITypeSymbol typeSymbol,
            SymbolDisplayFormat displayFormat,
            List<string> generatedInterfaceNames,
            StringBuilder builder
        )
        {
            if (typeSymbol is not IErrorTypeSymbol errorTypeSymbol)
            {
                // This symbol contains no unresolved types. Fall back to the default generation provided by Roslyn
                builder.Append(typeSymbol.ToDisplayString(displayFormat));
                return;
            }

            var symbolName =
                InferGeneratedInterfaceName(errorTypeSymbol, generatedInterfaceNames)
                ?? errorTypeSymbol.Name;

            builder.Append(symbolName);

            if (errorTypeSymbol.IsGenericType)
            {
                builder.Append('<');

                bool isFirstTypeArgument = true;
                foreach (var typeArgument in errorTypeSymbol.TypeArguments)
                {
                    if (!isFirstTypeArgument)
                    {
                        builder.Append(", ");
                    }

                    AppendTypeSymbolDisplayString(
                        typeArgument,
                        displayFormat,
                        generatedInterfaceNames,
                        builder
                    );

                    isFirstTypeArgument = false;
                }

                builder.Append('>');
            }
        }

        private static string? InferGeneratedInterfaceName(
            IErrorTypeSymbol unrecognisedSymbol,
            List<string> generatedInterfaceNames
        )
        {
            var matches = generatedInterfaceNames
                .Where(name => Regex.IsMatch(name, $"[.:]{unrecognisedSymbol.Name}$"))
                .ToList();

            if (matches.Count != 1)
            {
                // Either there's no match or an ambiguous match - we can't safely infer the interface name.
                // This is very much a "best effort" approach - if there are two interfaces with the same name,
                // there's no obvious way to work out which one the symbol is referring to.
                return null;
            }

            return matches[0];
        }
    }
}
