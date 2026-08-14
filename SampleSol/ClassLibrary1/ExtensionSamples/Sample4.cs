using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionSamples
{
    internal class Sample4
    {
        //prop or field (instance or static), or out param?, NO const!
        public string Extension => ".cs";
        public string Generate(DTE dte, string input, string inputFileName,string ns)
        {
            return $"Output from solution generator: input file {inputFileName} length {input.Length}, namespace {ns}";
        }
    }
}
