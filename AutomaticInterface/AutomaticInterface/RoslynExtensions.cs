using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Source: https://github.com/dominikjeske/Samples/blob/main/SourceGenerators/HomeCenter.SourceGenerators/Extensions/RoslynExtensions.cs
/// </summary>

namespace AutomaticInterface
{
    public static class RoslynExtensions
    {
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
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

        public static CompilationUnitSyntax GetCompilationUnit(this SyntaxNode syntaxNode)
        {
            return syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        }

        public static string GetClassName(this ClassDeclarationSyntax proxy)
        {
            return proxy.Identifier.Text;
        }

        public static string GetClassModifier(this ClassDeclarationSyntax proxy)
        {
            return proxy.Modifiers.ToFullString().Trim();
        }

        public static bool HaveAttribute(this ClassDeclarationSyntax classSyntax, string attributeName)
        {
            return classSyntax.AttributeLists.Count > 0 &&
                   classSyntax.AttributeLists.SelectMany(al => al.Attributes
                           .Where(a => { return (a?.Name as IdentifierNameSyntax)?.Identifier.Text == attributeName; }))
                       .Any();
        }


        public static string GetNamespace(this CompilationUnitSyntax root)
        {
            return root.ChildNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()
                .Name
                .ToString();
        }

        public static List<string> GetUsings(this CompilationUnitSyntax root)
        {
            return root.ChildNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(n => n.Name.ToString())
                .ToList();
        }

        /// <summary>
        /// Thanks to https://www.codeproject.com/Articles/871704/Roslyn-Code-Analysis-in-Easy-Samples-Part-2
        /// </summary>
        /// <param name="typeParameterSymbol"></param>
        /// <returns></returns>
        public static string GetWhereStatement(this ITypeParameterSymbol typeParameterSymbol)
        {
            string result = "where " + typeParameterSymbol.Name + " : ";

            string constraints = "";

            bool isFirstConstraint = true;

            if (typeParameterSymbol.HasReferenceTypeConstraint)
            {
                constraints += "class";

                isFirstConstraint = false;
            }

            if (typeParameterSymbol.HasValueTypeConstraint)
            {
                constraints += "struct";

                isFirstConstraint = false;
            }

            foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
            {
                // if not first constraint prepend with comma
                if (!isFirstConstraint)
                {
                    constraints += ", ";
                }
                else
                {
                    isFirstConstraint = false;
                }

                constraints += constraintType.GetFullTypeString();
            }

            if (string.IsNullOrEmpty(constraints))
                return null;

            result += constraints;

            return result;
        }

        public static string GetFullTypeString(this ITypeSymbol type)
        {
            string result =
                type.Name +
                type.GetTypeArgsStr(symbol => ((INamedTypeSymbol)symbol).TypeArguments);

            return result;
        }

        static string GetTypeArgsStr
        (
            this ISymbol symbol,
            Func<ISymbol, IEnumerable<ITypeSymbol>> typeArgGetter
        )
        {
            IEnumerable<ITypeSymbol> typeArgs = typeArgGetter(symbol).ToList();

            string result = "";

            if (typeArgs.Any())
            {
                result += "<";

                bool isFirstIteration = true;
                foreach (ITypeSymbol typeArg in typeArgs)
                {
                    // insert comma if not first iteration                    
                    if (isFirstIteration)
                    {
                        isFirstIteration = false;
                    }
                    else
                    {
                        result += ", ";
                    }

                    ITypeParameterSymbol typeParameterSymbol =
                        typeArg as ITypeParameterSymbol;

                    string strToAdd = null;
                    if (typeParameterSymbol != null)
                    {
                        // this is a generic argument
                        strToAdd = typeParameterSymbol.Name;
                    }
                    else
                    {
                        // this is a generic argument value. 
                        INamedTypeSymbol namedTypeSymbol =
                            typeArg as INamedTypeSymbol;

                        strToAdd = namedTypeSymbol.GetFullTypeString();
                    }

                    result += strToAdd;
                }

                result += ">";
            }

            return result;
        }
    }
}