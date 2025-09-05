using EnvDTE;
using System;
using System.Windows.Forms;

namespace ExtensionSamples
{
    public class Sample1
    {
        //change type of package to Microsoft.VisualStudio.Shell.Package, if you want to reference another libraries
        /// <summary>
        /// Runs your code 
        /// </summary>
        /// <param name="dte">Reference to VS DTE, you can change it to EnvDTE80.DTE2</param>
        /// <param name="package">Reference to executing package, you can change it to AsyncPackage,IAsyncServiceProvider</param>
        public void Run(DTE dte, IServiceProvider package)
        {
            MessageBox.Show("Running!");
            System.Diagnostics.Debugger.Break();
            if (package != null)
            {
                dynamic p = package;
                p.TestMethod();
            }
        }
    }
}
