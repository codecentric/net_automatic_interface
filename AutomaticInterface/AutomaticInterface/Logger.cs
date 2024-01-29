using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface
{
    public record LoggerOptions(string LogPath, bool EnableLogging, string Name);

    public sealed class Logger : IDisposable
    {
        private const int LineSuffixLength = 20;
        private const int LineLenght = 100;
        private readonly GeneratorExecutionContext executionContext;
        private readonly LoggerOptions options;
        private readonly Stopwatch loggerStopwatch;
        private readonly string logFile;

        public Logger(GeneratorExecutionContext generatorExecutionContext, LoggerOptions options)
        {
            executionContext = generatorExecutionContext;
            this.options = options;

            loggerStopwatch = new Stopwatch();
            loggerStopwatch.Start();

            try
            {
                if (options.EnableLogging)
                {
                    Directory.CreateDirectory(options.LogPath);
                }
            }
            catch (Exception)
            {
                var errorDescriptor = new DiagnosticDescriptor(
                    nameof(AutomaticInterface),
                    "Error",
                    $"{nameof(AutomaticInterfaceGenerator)} cannot store logs at {options.LogPath}",
                    "Compilation",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                );
                generatorExecutionContext.ReportDiagnostic(
                    Diagnostic.Create(errorDescriptor, null)
                );
            }

            var descriptor = new DiagnosticDescriptor(
                nameof(AutomaticInterface),
                "Info",
                $"{nameof(AutomaticInterfaceGenerator)} stores logs at {options.LogPath}",
                "Compilation",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true
            );
            generatorExecutionContext.ReportDiagnostic(Diagnostic.Create(descriptor, null));

            logFile = Path.Combine(options.LogPath, $"{options.Name}_log.txt");

            if (options.EnableLogging)
            {
                WriteHeader();
            }
        }

        public void Dispose()
        {
            DisposeFinal();
            GC.SuppressFinalize(this);
        }

        private void DisposeFinal()
        {
            loggerStopwatch.Stop();

            if (!options.EnableLogging)
            {
                return;
            }

            var summary = GetTextWithLine($"END [{options.Name} | {loggerStopwatch.Elapsed:g}] ");

            WriteLog(summary);
        }

        public void TryLogSourceCode(
            ClassDeclarationSyntax classDeclaration,
            string generatedSource
        )
        {
            if (!options.EnableLogging)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(
                $"-> Generated class for '{classDeclaration.Identifier.Text}':{Environment.NewLine}"
            );
            sb.AppendLine(generatedSource);
            sb.AppendLine("");

            WriteLog(sb.ToString());
        }

        public void TryLogException(ClassDeclarationSyntax classDeclaration, Exception exception)
        {
            if (!options.EnableLogging)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(
                $"-> Exception for '{classDeclaration.Identifier.Text}':{Environment.NewLine}"
            );
            sb.AppendLine(exception.ToString());
            sb.AppendLine("");

            WriteLog(sb.ToString());
        }

        public void LogMessage(string message)
        {
            if (!options.EnableLogging)
                return;

            WriteLog(message);
        }

        private void WriteHeader()
        {
            var sb = new StringBuilder();
            var header = GetTextWithLine($" [{options.Name} | {DateTime.Now:g}] ");

            sb.AppendLine(header);
            sb.AppendLine();

            sb.AppendLine($"-> Language: {executionContext.ParseOptions.Language}");
            sb.AppendLine($"-> Kind: {executionContext.ParseOptions.Kind}");

            foreach (var additionalFile in executionContext.AdditionalFiles)
                sb.AppendLine(additionalFile.Path);

            WriteLog(sb.ToString());
        }

        private void WriteLog(string logText)
        {
            try
            {
                File.AppendAllText(logFile, $"{logText}{Environment.NewLine}");
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static string GetTextWithLine(string context)
        {
            return new string('-', LineSuffixLength)
                + context
                + new string('-', LineLenght - LineSuffixLength - context.Length);
        }
    }
}
