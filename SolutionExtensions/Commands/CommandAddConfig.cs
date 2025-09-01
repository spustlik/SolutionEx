using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;

namespace SolutionExtensions
{
    public sealed class CommandAddConfig : CommandBase
    {
        public CommandAddConfig() : base(0x0100)
        {
            //this command in dte is
            //ID=256,GUID={7A30B1A0-C6BB-41EE-A9B4-F15017E2FEE5},Tools.SolutionExtensionsaddconfig,BIND=0:
            //ID:dec 0x100
            //GUID: guid of cmdSet ?!? guidCommandAddConfigPackageCmdSet/@value
            //Name: how?? from button text ?
        }

        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            var dte = this.package.GetService<DTE, DTE>();
            dte.AddToOutputPane($"Command clicked", GetType().Name);
            var wnd = this.package.CreateToolWindow<ToolWindows.ExtensionsListToolWindowPane>();
            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "CommandAddConfig",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
