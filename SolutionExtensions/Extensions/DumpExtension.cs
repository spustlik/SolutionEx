using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace SolutionExtensions.Extensions
{

    public class DumpExtension
    {
        [Description("Dump DTE")]
        public void Run(EnvDTE.DTE dte, AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dumper = new Dumper();
            var root = dumper.Dump(dte, package, false);
            var fn = dumper.Save(root);
            dte.Documents.Open(fn);
            
        }
    }
}
