using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomaticInterface
{
    [Generator]
    public class AutomaticInterfaceGenerator: ISourceGenerator
    {

        public void Execute(GeneratorExecutionContext context)
        {
            var options = new LoggerOptions(Path.Combine(Environment.CurrentDirectory, "logs"), true, true, typeof(AutomaticInterfaceGenerator).Name);
            using Logger logger = new(context, options) ;




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

            CreateInterfaces(context, classSymbols, logger);
        }

        private void CreateInterfaces(GeneratorExecutionContext context, List<ClassDeclarationSyntax> classes, Logger logger)
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
                AddMethodsToInterface(namedType, interfaceGenerator, classSyntax);

                var descriptor = new DiagnosticDescriptor(nameof(AutomaticInterface), "Result", $"Finished compilation for {interfaceName}", "Compilation", DiagnosticSeverity.Info, isEnabledByDefault: true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

                // inject the created source into the users compilation
                string generatedCode = interfaceGenerator.BuildCode();
                context.AddSource($"I{classSyntax.GetClassName()}", SourceText.From(generatedCode, Encoding.UTF8));

                logger.TryLogSourceCode(classSyntax, generatedCode);
            }
        }

        private void AddMethodsToInterface(INamedTypeSymbol classSymbol, CodeGenerator codeGenerator, ClassDeclarationSyntax classSyntax)
        {
            classSymbol.GetAllMembers()
                 .Where(x => x.Kind == SymbolKind.Method)
                 .OfType<IMethodSymbol>()
                 .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                 .Where(x => x.MethodKind == MethodKind.Ordinary) // todo is this everything?
                 .Where(x => x.ContainingType.Name != typeof(object).Name)
                 .ToList()
                .ForEach(method =>
                {
                    var returnType = method.ReturnType;
                    var name = method.Name;
                    var documentation = GetDocumentationFor(method, classSyntax);

                    var paramResult = new HashSet<string>();
                    method.Parameters
                    .Select(x => $"{x.ToDisplayString()} {x.Name}" )
                    .ToList()
                    .ForEach(x => paramResult.Add(x));

                   codeGenerator.AddMethodToInterface(name, returnType.ToDisplayString(), paramResult, documentation);
                });
        }

        private string GetDocumentationFor(IMethodSymbol method, ClassDeclarationSyntax classSyntax)
        {
            SyntaxKind[] docSyntax = { SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EndOfDocumentationCommentToken, SyntaxKind.MultiLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia };

            var match = classSyntax.DescendantNodes()
             .OfType<MethodDeclarationSyntax>()
             .SingleOrDefault(x => x.Identifier.ValueText == method.Name);

            if (match is null)
            {
                return string.Empty;
            }

            if (!match.HasLeadingTrivia)
            {
                // no documentation
                return string.Empty;
            }

            var trivia = match.GetLeadingTrivia()
                .Where(x => docSyntax.Contains(x.Kind()))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

            return trivia.ToFullString().Trim();
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
            List<ClassDeclarationSyntax> classSymbols = new ();
            foreach (ClassDeclarationSyntax cls in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(cls.SyntaxTree);

                var classSymbol = model.GetDeclaredSymbol(cls);

                if (classSymbol is null)
                {
                    continue;
                }

                if (classSymbol.GetAttributes().Any(ad =>
                {
                    var name = ad?.AttributeClass?.Name;

                    if (name == null)
                    {
                        return false;
                    }
                    return name.StartsWith("GenerateAutomaticInterface");
                }))
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
                   var hasGet = prop.GetMethod?.DeclaredAccessibility == Accessibility.Public;
                   var hasSet = prop.SetMethod?.DeclaredAccessibility == Accessibility.Public;

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
