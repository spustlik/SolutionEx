using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VSIXTests
{
    public class Test
    {
        public string Argument { get; set; } = "some value";
        public void Run(DTE dte)
        {
            MessageBox.Show("Foo4:"+Argument);
        }
    }
}
