using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionSamples
{
    internal class Sample5
    {
        public string Extension => ".xyz";
        public string Generate(DTE dte, string input, string inputFileName,string ns)
        {
            return $"Some xyz output";
        }
    }
}
