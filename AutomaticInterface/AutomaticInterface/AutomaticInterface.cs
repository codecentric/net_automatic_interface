﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutomaticInterface;

[Generator]
public class AutomaticInterfaceGenerator : ISourceGenerator
{
  public void Execute(GeneratorExecutionContext context)
  {
    var logPath = GetLogPath();
    var options = new LoggerOptions(logPath, false, nameof(AutomaticInterfaceGenerator)); // todo use env variable for logging?
    using Logger logger = new(context, options);

    // retrieve the populated receiver 
    if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

    var compilation = context.Compilation;

    // inject attribute directly into user's project
    context.AddSource
    (
      "GenerateAutomaticInterfaceAttribute.cs",
      SourceText.From
      (@"
using System;

namespace AutomaticInterface
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateAutomaticInterfaceAttribute : Attribute
    {
    }
}
", Encoding.UTF8)
    );

    try
    {
      var classSymbols = GetClassesToAddInterfaceFor(receiver, compilation);

      CreateInterfaces(context, classSymbols, logger);
    }
    catch (Exception e)
    {
      var descriptor = new DiagnosticDescriptor(nameof(AutomaticInterface), "Error",
          $"{nameof(AutomaticInterfaceGenerator)} failed to generate Interface due to an error. Please inform the author. Error: {e}",
          "Compilation", DiagnosticSeverity.Error, true);
      context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

      throw;
    }

    string GetLogPath()
    {
      var mainSyntaxTree = context.Compilation.SyntaxTrees
          .First(x => x.HasCompilationUnitRoot);

      var logDir = Path.GetDirectoryName(mainSyntaxTree.FilePath) ?? Environment.CurrentDirectory;

      if (logDir.Contains("MSBuild") || logDir.StartsWith("/0/"))
        // MSBuild is often in Program Files and cannot be written
        // /0/ happens in github pipeline
        logDir = Path.GetTempPath();
      return Path.Combine(logDir, "logs");
    }
  }

  private void CreateInterfaces(GeneratorExecutionContext context, List<ClassDeclarationSyntax> classes,
      Logger logger)
  {
    foreach (var classSyntax in classes)
    {
      if (classSyntax == null) continue;

      var compilation = context.Compilation;
      var classSemanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);

      var root = classSyntax.GetCompilationUnit();

      var namespaceName = root.GetNamespace();
      var interfaceName = $"I{classSyntax.GetClassName()}";

      //Checking globally enabled context. Probably in future we should check for every generated line
      var nullableContext = classSemanticModel.GetNullableContext(0);

      var interfaceGenerator = new InterfaceBuilder(namespaceName, interfaceName, nullableContext.AnnotationsEnabled());


      var namedType = classSemanticModel.GetDeclaredSymbol(classSyntax);

      if (namedType == null) continue;

      interfaceGenerator.AddUsings(GetUsings(namedType));
      interfaceGenerator.AddClassDocumentation(GetDocumentationForClass(classSyntax));
      interfaceGenerator.AddGeneric(GetGeneric(namedType, classSyntax));

      AddPropertiesToInterface(namedType, interfaceGenerator, classSyntax);
      AddMethodsToInterface(namedType, interfaceGenerator, classSyntax, classSemanticModel);
      AddEventsToInterface(namedType, interfaceGenerator, classSyntax);

      var descriptor = new DiagnosticDescriptor(nameof(AutomaticInterface), "Result",
          $"Finished compilation for {interfaceName}", "Compilation", DiagnosticSeverity.Info, true);
      context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

      // inject the created source into the users compilation
      var generatedCode = interfaceGenerator.Build();
      context.AddSource($"I{classSyntax.GetClassName()}", SourceText.From(generatedCode, Encoding.UTF8));

      logger.TryLogSourceCode(classSyntax, generatedCode);
    }
  }

  private static void AddEventsToInterface(INamedTypeSymbol classSymbol, InterfaceBuilder codeGenerator,
      ClassDeclarationSyntax classSyntax)
  {
    classSymbol.GetAllMembers()
        .Where(x => x.Kind == SymbolKind.Event)
        .OfType<IEventSymbol>()
        .Where(x => x.DeclaredAccessibility == Accessibility.Public)
        .Where(x => !x.IsStatic)
        .ToList()
        .ForEach(evt =>
        {
          var type = evt.Type;
          var name = evt.Name;
          var documentation = GetDocumentationForEvent(evt, classSyntax);

          codeGenerator.AddEventToInterface(name, type.ToDisplayString(), documentation);
        });
  }


  private static string GetGeneric(INamedTypeSymbol classSymbol, ClassDeclarationSyntax cls)
  {
    if (!classSymbol.IsGenericType) return string.Empty;

    var formattedGeneric = $"{cls.TypeParameterList?.ToFullString().Trim()} {cls.ConstraintClauses}";
    return formattedGeneric;
  }

  private static void AddMethodsToInterface(INamedTypeSymbol classSymbol, InterfaceBuilder codeGenerator,
      ClassDeclarationSyntax classSyntax, SemanticModel classSemanticModel)
  {
    classSymbol.GetAllMembers()
            .Where(x => x.Kind == SymbolKind.Method)
            .OfType<IMethodSymbol>()
            .Where(
              x => x.DeclaredAccessibility == Accessibility.Public
                  && x.MethodKind == MethodKind.Ordinary
                  && !x.IsStatic
                  && !x.IsOverride
                  && x.ContainingType.Name != nameof(Object)
            )
            .ToList()
            .ForEach(method =>
            {
              var returnType = method.ReturnType;
              var name = method.Name;
              var documentation = GetDocumentationFor(method, classSyntax, classSemanticModel);

              var paramResult = new HashSet<string>();
              method.Parameters
                  .Select(GetMethodSignature)
                  .ToList()
                  .ForEach(x => paramResult.Add(x));

              var typedArgs = method.TypeParameters.Select(arg => (arg.ToDisplayString(), arg.GetWhereStatement()))
                  .ToList();
              codeGenerator.AddMethodToInterface(name, returnType.ToDisplayString(), documentation, paramResult,
                  typedArgs);
            });
  }

  private static string GetMethodSignature(IParameterSymbol x)
  {
    // Roslyn strips out the @ sign on x.Name so we need to check
    // if it had one by examining the parameter's identifier syntax node
    bool wasVerbatim = x.DeclaringSyntaxReferences
      .Select(reference => reference.GetSyntax())
      .OfType<ParameterSyntax>()
      .Any(param =>
      {
        string text = param.Identifier.Text;
        return text.Length > 0 && text[0] == '@';
      });

    bool isReservedWord =
      SyntaxFacts.GetKeywordKind(x.Name) != SyntaxKind.None ||
      SyntaxFacts.GetContextualKeywordKind(x.Name) != SyntaxKind.None;

    string name = isReservedWord || wasVerbatim ? $"@{x.Name}" : x.Name;

    if (!x.HasExplicitDefaultValue)
    {
      return $"{x.Type.ToDisplayString()} {name}";
    }

    // bool is a special case that doesn't match the pattern because
    // the explicit default value gets coerced to a string
    if (x.Type.SpecialType == SpecialType.System_Boolean)
    {
      var defaultValue = x.ExplicitDefaultValue is bool boolean && boolean ? "true" : "false";
      return $"{x.Type.ToDisplayString()} {name} = {defaultValue}";
    }

    if (x.Type.TypeKind == TypeKind.Enum)
    {
      // cast default value to enum type if it's an enum
      return $"{x.Type.ToDisplayString()} {name} = ({x.Type.ToDisplayString()}){x.ExplicitDefaultValue}";
    }

    string optionalValue = x.ExplicitDefaultValue switch
    {
      string => $" = \"{x.ExplicitDefaultValue}\"",
      // struct
      null when x.Type.IsValueType => $" = default({x.Type})",
      null => " = null",
      _ => $" = {x.ExplicitDefaultValue}"
    };

    return $"{x.Type.ToDisplayString()} {name}{optionalValue}";
  }

  private static string GetDocumentationFor(IMethodSymbol method, ClassDeclarationSyntax classSyntax,
      SemanticModel classSemanticModel)
  {
    SyntaxKind[] docSyntax =
    {
            SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia
        };

    var match = classSyntax
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .SingleOrDefault(x => IsSameMethod(method, x, classSemanticModel));

    if (match is null) return string.Empty;

    if (!match.HasLeadingTrivia)
      // no documentation
      return string.Empty;

    var trivia = match.GetLeadingTrivia()
        .Where(x => docSyntax.Contains(x.Kind()))
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

    return trivia.ToFullString().Trim();
  }

  private static bool IsSameMethod(IMethodSymbol method, MethodDeclarationSyntax x,
      SemanticModel classSemanticModel)
  {
    if (x.Identifier.ValueText != method.Name) return false;

    // ok name is matching, now we have to see if overloads match

    var xParams = x.ParameterList.Parameters;
    var methodParams = method.Parameters;

    if (xParams.Count != methodParams.Length) return false;

    for (var i = 0; i < xParams.Count; i++)
    {
      var methodSymbol = methodParams[i];
      var xParam = xParams[i];

      var typeSymbol = classSemanticModel.GetSymbolInfo(xParam.Type!).Symbol;

      if (typeSymbol == null) return false;

#pragma warning disable RS1024
      var matches = typeSymbol.Equals(methodSymbol.Type);
#pragma warning restore RS1024

      if (!matches) return false;
    }

    return true;
  }

  private static string GetDocumentationForProperty(IPropertySymbol method, ClassDeclarationSyntax classSyntax)
  {
    SyntaxKind[] docSyntax =
    {
            SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia
        };

    var match = classSyntax.DescendantNodes()
        .OfType<PropertyDeclarationSyntax>()
        .SingleOrDefault(x => x.Identifier.ValueText == method.Name);

    if (match is null) return string.Empty;

    if (!match.HasLeadingTrivia)
      // no documentation
      return string.Empty;

    var trivia = match.GetLeadingTrivia()
        .Where(x => docSyntax.Contains(x.Kind()))
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

    return trivia.ToFullString().Trim();
  }

  private static string GetDocumentationForEvent(IEventSymbol method, ClassDeclarationSyntax classSyntax)
  {
    SyntaxKind[] docSyntax =
    {
            SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia
        };

    var match = classSyntax.DescendantNodes()
        .OfType<EventFieldDeclarationSyntax>()
        .SingleOrDefault(x => x.Declaration.Variables.Any(y => y.Identifier.ValueText == method.Name));

    if (match is null) return string.Empty;

    if (!match.HasLeadingTrivia)
      // no documentation
      return string.Empty;

    var trivia = match.GetLeadingTrivia()
        .Where(x => docSyntax.Contains(x.Kind()))
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

    return trivia.ToFullString().Trim();
  }

  private static string GetDocumentationForClass(ClassDeclarationSyntax classSyntax)
  {
    if (!classSyntax.HasLeadingTrivia)
      // no documentation
      return string.Empty;

    SyntaxKind[] docSyntax =
    {
            SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EndOfDocumentationCommentToken,
            SyntaxKind.MultiLineDocumentationCommentTrivia, SyntaxKind.SingleLineDocumentationCommentTrivia
        };

    var trivia = classSyntax.GetLeadingTrivia()
        .Where(x => docSyntax.Contains(x.Kind()))
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ToFullString()));

    return trivia.ToFullString().Trim();
  }

  private static IEnumerable<string> GetUsings(ISymbol classSymbol)
  {
    var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
    foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
      foreach (var parent in syntaxRef.GetSyntax().Ancestors(false))
        allUsings = parent switch
        {
          NamespaceDeclarationSyntax ndSyntax => allUsings.AddRange(ndSyntax.Usings),
          CompilationUnitSyntax cuSyntax => allUsings.AddRange(cuSyntax.Usings),
          _ => allUsings
        };

    return new HashSet<string>(allUsings.Select(x => x.ToString()));
  }

  private static List<ClassDeclarationSyntax> GetClassesToAddInterfaceFor(SyntaxReceiver receiver,
      Compilation compilation)
  {
    List<ClassDeclarationSyntax> classSymbols = new();
    foreach (var cls in receiver.CandidateClasses)
    {
      var model = compilation.GetSemanticModel(cls.SyntaxTree);

      var classSymbol = model.GetDeclaredSymbol(cls);

      if (classSymbol is null) continue;

      if (classSymbol.GetAttributes().Any(ad =>
          {
            var name = ad?.AttributeClass?.Name;

            if (name == null) return false;
            return name.StartsWith("GenerateAutomaticInterface");
          }))
        classSymbols.Add(cls);
    }

    return classSymbols;
  }

  private static void AddPropertiesToInterface(INamedTypeSymbol classSymbol, InterfaceBuilder codeGenerator,
      ClassDeclarationSyntax classSyntax)
  {
    classSymbol.GetAllMembers()
        .Where(x => x.Kind == SymbolKind.Property)
        .OfType<IPropertySymbol>()
        .Where(x => x.DeclaredAccessibility == Accessibility.Public)
        .Where(x => !x.IsStatic)
        .Where(x => !x.IsIndexer)
        .ToList()
        .ForEach(prop =>
        {
          var type = prop.Type;

          var name = prop.Name;
          var hasGet = prop.GetMethod?.DeclaredAccessibility == Accessibility.Public;
          var hasSet = prop.SetMethod?.DeclaredAccessibility == Accessibility.Public;
          var documentation = GetDocumentationForProperty(prop, classSyntax);

          codeGenerator.AddPropertyToInterface(name, type.ToDisplayString(), hasGet, hasSet, documentation);
        });
  }

  public void Initialize(GeneratorInitializationContext context)
  {
    // Register a syntax receiver that will be created for each generation pass
    context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
  }
}

/// <summary>
/// Created on demand before each generation pass
/// </summary>
internal class SyntaxReceiver : ISyntaxReceiver
{
  public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

  /// <summary>
  /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
  /// </summary>
  public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
  {
    // any field with at least one attribute is a candidate for property generation
    if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax)
      CandidateClasses.Add(classDeclarationSyntax);
  }
}
