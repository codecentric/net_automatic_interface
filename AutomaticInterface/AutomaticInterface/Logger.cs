using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AutomaticInterface
{
    public record LoggerOptions(string LogPath, bool EnableLogging, bool DetailedLogging, string name);
    internal class Logger : IDisposable
    {
        private const int LineSurfixLenght = 20;
        private const int LineLenght = 100;
        private readonly GeneratorExecutionContext _executionContext;
        private readonly LoggerOptions options;
        private readonly Stopwatch loggerStopwatch;
        private readonly string logFile;
  
        public Logger(GeneratorExecutionContext generatorExecutionContext, LoggerOptions options)
        {
            _executionContext = generatorExecutionContext;
            this.options = options;

            loggerStopwatch = new Stopwatch();
            loggerStopwatch.Start();

            Directory.CreateDirectory(options.LogPath);

            logFile = Path.Combine(options.LogPath, $"{options.name}_log.txt");


            if (options.EnableLogging)
            {
                WriteHeader();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
               loggerStopwatch.Stop();

            if (options.EnableLogging) {
                var summary = GetTextWithLine($"END [{options.name} | {loggerStopwatch.Elapsed:g}] ");

                WriteLog(summary);
            }
        }

        public void TryLogSourceCode(ClassDeclarationSyntax classDeclaration, string generatedSource)
        {
            if (!options.EnableLogging) return;

            var sb = new StringBuilder();
            sb.AppendLine($"-> Generated class for '{classDeclaration.Identifier.Text}':{Environment.NewLine}");
            sb.AppendLine(generatedSource);
            sb.AppendLine("");

            WriteLog(sb.ToString());
        }

        public void TryLogException(ClassDeclarationSyntax classDeclaration, Exception exception)
        {
            if (!options.EnableLogging) return;

            var sb = new StringBuilder();
            sb.AppendLine($"-> Exception for '{classDeclaration.Identifier.Text}':{Environment.NewLine}");
            sb.AppendLine(exception.ToString());
            sb.AppendLine("");

            WriteLog(sb.ToString());
        }

        public void LogMessage(string message)
        {
            if (!options.EnableLogging) return;

            WriteLog(message);
        }


        private void WriteHeader()
        {
            var sb = new StringBuilder();
            var header = GetTextWithLine($" [{options.name} | {DateTime.Now:g}] ");

            sb.AppendLine(header);
            sb.AppendLine();

            sb.AppendLine($"-> Language: {_executionContext.ParseOptions.Language}");
            sb.AppendLine($"-> Kind: {_executionContext.ParseOptions.Kind}");

            foreach (var additionalFile in _executionContext.AdditionalFiles) sb.AppendLine(additionalFile.Path);

            WriteLog(sb.ToString());
        }


        private void WriteLog(string logtext)
        {
            File.AppendAllText(logFile, $"{logtext}{Environment.NewLine}");
        }

        private string GetTextWithLine(string context)
        {
            return new string('-', LineSurfixLenght) + context +
                   new string('-', LineLenght - LineSurfixLenght - context.Length);
        }
    }
}
