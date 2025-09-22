using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXTests
{
    public class Macro
    {
        public void Run(DTE dte)
        {
            var selection = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
            var s = selection.Text;
            s = s.Replace(".", "_");
            selection.Text = "{x:Static themes:" + s + "}";
        }
    }
}
