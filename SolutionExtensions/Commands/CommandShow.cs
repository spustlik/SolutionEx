using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionExtensions.ToolWindows;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public sealed class CommandShow : CommandBaseAsync
    {
        public CommandShow() : base(CommandIds.Command_show)
        {
        }

        protected override async Task ExecuteAsync(object sender, EventArgs e)
        {
            var p = this.package as SolutionExtensionsPackage;
            p.AddToOutputPane("Show");
            await p.ShowToolWindowAsync(typeof(ExtensionsListToolWindowPane), 0, true, CancellationToken.None);
        }
    }
}
