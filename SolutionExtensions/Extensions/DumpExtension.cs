using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace SolutionExtensions.Extensions
{

    public class DumpExtension
    {
        [Description("Verbose level")]
        public int Argument { get; set; }
        [Description("Dump DTE")]
        public void Run(EnvDTE.DTE dte, AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dumper = new DteToXml();
            var root = dumper.Dump(dte, package, Argument>=0);
            var fn = dumper.Save(root);
            dte.Documents.Open(fn);
            
        }
    }
}
