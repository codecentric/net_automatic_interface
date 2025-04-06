using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            SymbolDisplayFormat typeDisplayFormat
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
                    t.ToDisplayString(typeDisplayFormat)
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
    }
}
