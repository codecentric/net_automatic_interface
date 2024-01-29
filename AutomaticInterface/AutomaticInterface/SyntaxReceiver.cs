using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface;

/// <summary>
/// Created on demand before each generation pass
/// </summary>
public sealed class SyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // any field with at least one attribute is a candidate for property generation
        if (
            syntaxNode is ClassDeclarationSyntax
            {
                AttributeLists.Count: > 0
            } classDeclarationSyntax
        )
            CandidateClasses.Add(classDeclarationSyntax);
    }
}
