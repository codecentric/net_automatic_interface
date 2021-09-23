using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticInterfaceExample
{
    public interface ITestInterFace
    {
        string MyProperty { get; set; }

        /// <summary>
        /// test
        /// </summary>
        /// <returns></returns>
        Task<string> GetData();

        /**
         * <summary>Hello World!</summary>
         */
        string GetHello(string x);
    }
}