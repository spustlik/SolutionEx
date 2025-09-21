using EnvDTE;
using System.ComponentModel;
using System.Windows.Forms;

namespace ExtensionSamples
{
    //sample to show argument usage (with automatic conversion)
    internal class Sample3
    {
        [Description("How many horses?")]
        public int Argument { get; set; }
        public void Run(DTE dte)
        {
            MessageBox.Show($"Horses: {Argument}");
        }
    }
}
