using Scriban;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutomaticInterface
{
    public record PropertyInfo(string Name, string Ttype, bool HasGet, bool HasSet);

    public record MethodDescription(string Name, string ReturnType, string ReturnTypeGenericArgument, List<string> arguments);

    public record Model(string InterfaceName, string Namespace, HashSet<string> Usings, List<PropertyInfo> Properties, List<MethodDescription> Methods);

    public class CodeGenerator
    {
        private readonly string nameSpaceName;
        private readonly string interfaceName;
        private readonly HashSet<string> usings = new() { "System.CodeDom.Compiler" };
        private readonly List<PropertyInfo> propertyInfos = new();

        public CodeGenerator(string nameSpaceName, string interfaceName){
            this.nameSpaceName = nameSpaceName;
            this.interfaceName = interfaceName;
        }

        public void AddPropertyToInterface(string name, string ttype, bool hasGet, bool hasSet)
        {
            // todo add necessary namespaces?
            propertyInfos.Add(new PropertyInfo(name, ttype, hasGet, hasSet));
        }

        private Model BuildModel()
        {
            return new Model(interfaceName, nameSpaceName, usings, propertyInfos, new());
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
                throw new InvalidOperationException($"Template parse error: {template.Messages}");
            }

            var result = template.Render(BuildModel(), member => member.Name);

            return result;
        }
    }
}
