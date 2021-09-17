using System;
using System.CodeDom;
using System.Collections.Generic;

namespace AutomaticInterface
{
    public class CodeGenerator
    {
        private readonly CodeNamespace _nameSpace;
        private readonly CodeTypeDeclaration _interface;
        private readonly HashSet<String> imports = new();

        public CodeGenerator(string nameSpaceName, string interfaceName){
            this._nameSpace = new CodeNamespace(nameSpaceName);
            _interface = new CodeTypeDeclaration();
            _interface.Name = interfaceName;
            _interface.IsInterface = true;
            _interface.Attributes = MemberAttributes.Public;

            _nameSpace.Types.Add(_interface);
        }

        public void AddMemberToInterface(Action<CodeTypeDeclaration> action)
        {
            action(_interface);
            // todo logging
        }

        public void SaveAssembly()
        {
            var result = _nameSpace.ToString();
            Console.WriteLine(result);
        }
    }
}
