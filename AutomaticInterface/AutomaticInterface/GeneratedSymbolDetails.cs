using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

internal sealed class GeneratedSymbolDetails(
    AttributeData? generationAttribute,
    ITypeSymbol typeSymbol,
    ClassDeclarationSyntax classSyntax
)
{
    /// <summary>
    /// Represents the namespace name associated with the generated interface or type symbol.
    /// This value is typically derived from the provided generation attribute or defaults
    /// to the containing namespace of the type symbol.
    /// </summary>
    public string NamespaceName { get; } =
        PrepareValue(
            generationAttribute,
            AutomaticInterfaceGenerator.NamespaceParameterName,
            typeSymbol.ContainingNamespace.ToDisplayString()
        );

    /// <summary>
    /// Represents the name of the interface generated for a class. The interface name
    /// is derived from the class name, prefixed with 'I', unless overridden by a specific
    /// attribute value during generation.
    /// </summary>
    public string InterfaceName { get; } =
        PrepareValue(
            generationAttribute,
            AutomaticInterfaceGenerator.InterfaceParameterName,
            $"I{classSyntax.GetClassName()}"
        );

    /// <summary>
    /// Determines the access level for the generated interface.
    /// This property is derived from the presence of <see cref="AutomaticInterfaceGenerator.AsInternalParameterName"/>
    /// that, if set, defines the interface as `internal`. Otherwise, the interface defaults to `public`.
    /// </summary>
    public string AccessLevel { get; } =
        PrepareValue(
            generationAttribute,
            AutomaticInterfaceGenerator.AsInternalParameterName,
            false
        )
            ? "internal"
            : "public";

    /// <summary>
    /// Prepares a value by retrieving it from an attribute's constructor arguments if available; otherwise, returns the provided default value.
    /// </summary>
    /// <typeparam name="T">The type of the value to prepare.</typeparam>
    /// <param name="generationAttribute">The attribute data containing constructor arguments.</param>
    /// <param name="key">The key to identify the relevant parameter in the constructor arguments.</param>
    /// <param name="defaultValue">The default value to return if the attribute does not provide a value.</param>
    /// <returns>
    /// The retrieved value from the attribute constructor's arguments, or the provided default value if the key is not found.
    /// </returns>
    private static T PrepareValue<T>(AttributeData? generationAttribute, string key, T defaultValue)
    {
        var parameterSymbol = generationAttribute?.AttributeConstructor?.Parameters.SingleOrDefault(
            x => x.Name == key
        );

        if (parameterSymbol != null)
        {
            var index = generationAttribute!.AttributeConstructor!.Parameters.IndexOf(
                parameterSymbol
            );
            var result = generationAttribute.ConstructorArguments[index].Value;
            if (result != null)
            {
                return (T)result;
            }
        }

        return defaultValue;
    }
}
