using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace SolutionExtensions.Extensions
{

    public class DumpExtensionMore
    {
        [Description("Dump DTE more")]
        public void Run(EnvDTE.DTE dte, AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dumper = new Dumper();
            var root = dumper.Dump(dte, package, true);
            var fn = dumper.Save(root);
            dte.Documents.Open(fn);
        }
    }
}
