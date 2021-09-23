using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutomaticInterface
{
    public record PropertyInfo(string Name, string Ttype, bool HasGet, bool HasSet, string Documentation);

    public record MethodInfo(string Name, string ReturnType, HashSet<string> Parameters, string Documentation);

    public record Model(string InterfaceName, string Namespace, HashSet<string> Usings, List<PropertyInfo> Properties, List<MethodInfo> Methods, string Documentation);

    public class CodeGenerator
    {
        private readonly string nameSpaceName;
        private readonly string interfaceName;

        /// <summary>
        /// 
        /// </summary>
        private readonly HashSet<string> usings = new() { "using System.CodeDom.Compiler;" };
        private readonly List<PropertyInfo> propertyInfos = new();
        private readonly List<MethodInfo> methodInfos = new();
        private string classDocumentation = string.Empty;

        public CodeGenerator(string nameSpaceName, string interfaceName){
            this.nameSpaceName = nameSpaceName;
            this.interfaceName = interfaceName;
        }

        public void AddPropertyToInterface(string name, string ttype, bool hasGet, bool hasSet, string documentation)
        {
            propertyInfos.Add(new PropertyInfo(name, ttype, hasGet, hasSet, documentation));
        }

        private Model BuildModel()
        {
            return new Model(interfaceName, nameSpaceName, usings, propertyInfos, methodInfos, classDocumentation);
        }

        public string BuildCode()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames().Where(r => r.EndsWith("InterfaceTemplate.scriban"));
            var resourceName = resources.Single();

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            using var streamReader = new StreamReader(resourceStream);
            var templateString = streamReader.ReadToEnd();

            var template = Template.Parse(templateString);

            if (template.HasErrors)
            {
                var errors = string.Join(" | ", template.Messages.Select(x => x.Message));
                throw new InvalidOperationException($"Template parse error: {errors}");
            }

            var result = template.Render(BuildModel(), member => member.Name);

            return result;
        }

        internal void AddClassDocumentation(string documentation)
        {
            this.classDocumentation = documentation;
        }

        public void AddUsings(IEnumerable<string> usings)
        {
            foreach (var usg in usings)
            {
                this.usings.Add(usg);
            }

        }

        internal void AddMethodToInterface(string name, string returnType, HashSet<string> parameters, string documentation)
        {
            methodInfos.Add(new MethodInfo(name, returnType, parameters, documentation));
        }
    }
}
