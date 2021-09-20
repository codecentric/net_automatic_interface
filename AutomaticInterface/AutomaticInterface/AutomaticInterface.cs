using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AutomaticInterface
{
    [Generator]
    public class AutomaticInterfaceGenerator: ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // retreive the populated receiver 
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var compilation = context.Compilation;

            if (compilation == null)
            {
                return;
            }

            var classSymbols = GetClassesToAddInterfaceFor(receiver, compilation);

            CreateInterfaces(context, classSymbols);
        }

        private void CreateInterfaces(GeneratorExecutionContext context, List<ClassDeclarationSyntax> classes)
        {
            foreach (var classSyntax in classes)
            {
                if (classSyntax == null)
                {
                    continue;
                }

                var compilation = context.Compilation;
                var classSemanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);


                if (classSemanticModel == null)
                {
                    continue;
                }

                var root = classSyntax.GetCompilationUnit();

                var namespaceName = root.GetNamespace();
                var interfaceName = $"I{classSyntax.GetClassName()}";

                var interfaceGenerator = new CodeGenerator(namespaceName, interfaceName);

                INamedTypeSymbol? namedType = classSemanticModel.GetDeclaredSymbol(classSyntax);

                if (namedType == null)
                {
                    continue;
                }

                interfaceGenerator.AddUsings(GetUsings(namedType));
                AddMembersToInterface(namedType, interfaceGenerator);

                var descriptor = new DiagnosticDescriptor(nameof(AutomaticInterface), "Result", $"Finished compilation for {interfaceName}", "Compilation", DiagnosticSeverity.Info, isEnabledByDefault: true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

                // inject the created source into the users compilation
                context.AddSource(nameof(AutomaticInterfaceGenerator), SourceText.From(interfaceGenerator.BuildCode(), Encoding.UTF8));
            }
        }

        private HashSet<string> GetUsings(INamedTypeSymbol classSymbol)
        {
            SyntaxList<UsingDirectiveSyntax> allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
            foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
            {
                foreach (var parent in syntaxRef.GetSyntax().Ancestors(false))
                {
                    if (parent is NamespaceDeclarationSyntax ndsyntax)
                    {
                        allUsings = allUsings.AddRange(ndsyntax.Usings);
                    }
                    else if (parent is CompilationUnitSyntax cusyntax)
                    {
                        allUsings = allUsings.AddRange(cusyntax.Usings);
                    }
                }
            }

            return new HashSet<string>(allUsings.Select(x => x.ToString()));
        }

        private static List<ClassDeclarationSyntax> GetClassesToAddInterfaceFor(SyntaxReceiver receiver, Compilation compilation)
        {
            INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName("AutomaticInterfaceAttribute.GenerateAutomaticInterfaceAttribute"); // todo reference this?

            if (attributeSymbol is null)
            {
                throw new ArgumentNullException("AutomaticInterfaceAttribute.GenerateAutomaticInterfaceAttribute not referenced");
            }

            List<ClassDeclarationSyntax> classSymbols = new ();
            foreach (ClassDeclarationSyntax cls in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(cls.SyntaxTree);

                var classSymbol = model.GetDeclaredSymbol(cls);

                if (classSymbol is null)
                {
                    continue;
                }

                if (classSymbol.GetAttributes().Any(ad => ad?.AttributeClass?.Name == attributeSymbol.Name)) // todo, weird that  ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default) always returns null - see https://github.com/dotnet/roslyn/issues/30248 maybe?
                {
                    classSymbols.Add(cls);
                }
            }

            return classSymbols;
        }

        private void AddMembersToInterface(INamedTypeSymbol classSymbol, CodeGenerator codeGenerator)
        {
            classSymbol.GetAllMembers()
                .Where(x => x.Kind == SymbolKind.Property)
                .OfType<IPropertySymbol>()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                .ToList()
               .ForEach(prop =>
               {
                   var type = prop.Type;
                   var name = prop.Name;
                   var hasGet = prop.GetMethod != null;
                   var hasSet = prop.SetMethod != null;
                   // todo check if get set is public?

                   codeGenerator.AddPropertyToInterface(name, type.ToDisplayString(), hasGet, hasSet);
               });
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
#if DEBUGGENERATOR
  if (!Debugger.IsAttached)
  {
    Debugger.Launch();
  }
#endif
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
