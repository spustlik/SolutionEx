using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Task = System.Threading.Tasks.Task;

namespace SolutionExtensions
{
    public sealed class CommandAddConfig : CommandBase
    {
        public CommandAddConfig() : base(0x0100)
        {
        }

        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "CommandAddConfig";
            var dte = this.package.GetService<DTE, DTE>();
            dte.AddToOutputPane($"Command clicked", GetType().Name);
            var wnd = this.package.CreateToolWindow<ToolWindows.ExtensionsListToolWindowPane>();
            //wnd.Content.
            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
