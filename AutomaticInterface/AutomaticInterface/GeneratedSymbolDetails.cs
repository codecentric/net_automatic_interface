using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

internal sealed class GeneratedSymbolDetails(AttributeData? generationAttribute, ITypeSymbol typeSymbol, ClassDeclarationSyntax classSyntax)
{
    /// <summary>
    /// Represents the namespace name associated with the generated interface or type symbol.
    /// This value is typically derived from the provided generation attribute or defaults
    /// to the containing namespace of the type symbol.
    /// </summary>
    public string NamespaceName { get; } = PrepareValue(generationAttribute, AutomaticInterfaceGenerator.NamespaceParameterName, typeSymbol.ContainingNamespace.ToDisplayString());

    /// <summary>
    /// Represents the name of the interface generated for a class. The interface name
    /// is derived from the class name, prefixed with 'I', unless overridden by a specific
    /// attribute value during generation.
    /// </summary>
    public string InterfaceName { get; } = PrepareValue(generationAttribute, AutomaticInterfaceGenerator.InterfaceParameterName, $"I{classSyntax.GetClassName()}");

    private static string PrepareValue(AttributeData? generationAttribute, string key, string defaultValue)
    {
        var parameterSymbol = generationAttribute?.AttributeConstructor?.Parameters.SingleOrDefault(x => x.Name == key);

        if (parameterSymbol != null)
        {
            var index = generationAttribute!.AttributeConstructor!.Parameters.IndexOf(parameterSymbol);
            var result = generationAttribute.ConstructorArguments[index].Value!.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return defaultValue;
    }
}