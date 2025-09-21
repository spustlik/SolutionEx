using EnvDTE;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace ExtensionSamples
{
    //sample to show difference between in-process and out-of-process execution
    public static class Sample2
    {
        //method can be also static
        public static void Run(DTE dte, IServiceProvider package)
        {
            //this will show different results if run in 'out of process' mode
            var p = System.Diagnostics.Process.GetCurrentProcess();
            var a = Assembly.GetEntryAssembly();
            var ad = AppDomain.CurrentDomain;
            MessageBox.Show($"Running from process #{p?.Id} {p?.MainModule?.ModuleName} {p?.MainModule?.FileName}\n" +
                $"Assembly {a?.GetName()?.Name} {a?.Location}\n" +
                $"AppDomain #{ad?.Id} {ad?.FriendlyName} {ad?.BaseDirectory}",
                "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
