using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions.Runner
{
    [ComVisible(true)]
    [Guid("EB4D0ECE-19DA-40B4-97DE-7D3785D05C87")]
    [ClassInterface(ClassInterfaceType.None)]
    public class ExtensionRunner : IExtensionRunner
    {
        public ExtensionRunner()
        {
            Console.WriteLine($"ExtensionRunner created in process ${Process.GetCurrentProcess().Id}");
        }
        public void Test(string param)
        {
            Console.WriteLine($"Test method called with param: {param}");
        }
    }

    public interface IExtensionRunner
    {
        void Test(string param);
    }
}
