using SolutionExtensions.ToolWindows;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SolutionExtensions.Commands
{
    public sealed class CommandShow : CommandBaseAsync
    {
        public CommandShow() : base(CommandIds.Command_show, CommandIds.CommandSetGuid)
        {
        }

        protected override async Task ExecuteAsync(object sender, EventArgs e)
        {
            var p = package as SolutionExtensionsPackage;
            p.Log("Show command executed");
            await p.ShowToolWindowAsync(typeof(ExtensionsListToolWindowPane), 0, true, CancellationToken.None);
        }
    }
}
