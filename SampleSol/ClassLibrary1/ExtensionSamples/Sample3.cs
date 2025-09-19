using EnvDTE;
using System.Windows.Forms;

namespace ExtensionSamples
{
    internal class Sample3
    {
        public int Argument { get; set; }
        public void Run(DTE dte)
        {
            MessageBox.Show("Argument: " + Argument);
        }
    }
}
