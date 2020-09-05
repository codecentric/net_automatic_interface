using AutomaticInterfaceAttribute;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomaticInterface
{
    [Generator]
    public class AutomaticInterfaceGenerator: ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("AutomaticInterfaceAttribute.GenerateAutomaticInterfaceAttribute");

            List<INamedTypeSymbol> classSymbols = new List<INamedTypeSymbol>();
            foreach (ClassDeclarationSyntax cls in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(cls.SyntaxTree);

                var classSymbol = model.GetDeclaredSymbol(cls);

                if (classSymbol.GetAttributes().Any(ad => ad.AttributeClass.Name == attributeSymbol.Name)) // todo, weird that  ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default) always returns null - see https://github.com/dotnet/roslyn/issues/30248 maybe?
                {
                    classSymbols.Add(classSymbol);
                }
            }

            foreach (var classSymbol in classSymbols)
            {
                var sourceBuilder = new StringBuilder();
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var interfaceName = $"I{classSymbol.Name}";
                sourceBuilder.Append($@"
using System;
namespace {namespaceName}
{{
    public interface {interfaceName}
    {{
         
");
                addMembersToInterface(classSymbol, sourceBuilder);

                sourceBuilder.Append(@"
    }      
}");
                File.WriteAllText(@"C:\dev\net_automatic_interface\AutomaticInterface\bla.cs", sourceBuilder.ToString());
                var descriptor = new DiagnosticDescriptor(nameof(AutomaticInterface), "Result", $"Finished compilation for {interfaceName}", "Compilation", DiagnosticSeverity.Warning, isEnabledByDefault: true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

                // inject the created source into the users compilation
                context.AddSource(nameof(AutomaticInterfaceGenerator), SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            }
        }

        private void addMembersToInterface(INamedTypeSymbol classSymbol, StringBuilder sourceBuilder)
        {
            foreach (var member in classSymbol.GetMembers())
            {
                // todo member.kind?

                if (member is IPropertySymbol && member.DeclaredAccessibility == Accessibility.Public)
                {
                    var prop = member as IPropertySymbol;
                    var type = prop.Type;
                    var name = prop.Name;
                    var hasGet = prop.GetMethod != null;
                    var hasSet = prop.SetMethod != null;
                    sourceBuilder.Append($"{type} {name} {{ {(hasGet ? "get;" : "" )}{(hasSet ? "set;" : "")}}}"); // todo get / set?
                }
                // todo check that using are included as necessary (e.g. using xyz and then referencing the type
                // todo all other cases.
            }
        }

        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
            Debugger.Launch();
        }
    }

    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // any field with at least one attribute is a candidate for property generation
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                CandidateClasses.Add(classDeclarationSyntax);
            }
        }
    }
}
