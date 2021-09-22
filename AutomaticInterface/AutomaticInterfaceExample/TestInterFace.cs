using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticInterfaceExample
{
    public interface ITestInterFace
    {
        string MyProperty { get; set; }

        Task<string> GetData();

        string GetHello(string x);
    }
}