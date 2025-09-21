using EnvDTE;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ExtensionSamples
{
    /// <summary>
    /// you can add Description attribute to class or method
    /// </summary>
    [Description("Sample 1 extension")]
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
            //add breakpoint or use this line to debug it
            //System.Diagnostics.Debugger.Break();
        }
    }
}
