using EnvDTE80;
using System.Windows.Forms;

namespace ExtensionSamples
{
    public class Sample1
    {
        //change type of package, if you want to reference another libraries
        public void Run(DTE2 dte, /*Microsoft.VisualStudio.Shell.Package*/ object package)
        {
            MessageBox.Show("Running!");
        }
    }
}
