using System;
using System.Linq;
using System.Threading.Tasks;

namespace SolutionExtensions.Commands
{
    public sealed class RunExtensionCommand : CommandBaseAsync
    {
        public int ExtensionIndex { get; }
        public RunExtensionCommand(int commandId, int extensionIndex) : base(commandId, CommandIds.CommandSetGuid)
        {
            ExtensionIndex = extensionIndex;
        }
        protected async override Task ExecuteAsync(object sender, EventArgs e)
        {            
            var package = this.package as SolutionExtensionsPackage;
            var item = package.Model.Extensions.ElementAtOrDefault(ExtensionIndex);
            if (item == null)
            {
                package.AddToOutputPane($"Extension with index ${ExtensionIndex} not found");
                return;
            }
            if (!package.ExtensionManager.AskArgumentIfNeeded(item, out var argument))
                return;
            package.Log($"Running extension #{ExtensionIndex}:{item.DllPath},{item.ClassName},{item.Title} with argument '{argument}'");
            try
            {
                package.ExtensionManager.RunExtension(item, argument);
            }
            catch (Exception ex)
            {
                package.AddToOutputPane("Error:" + ex.Message);
                await package.ShowStatusBarErrorAsync($"Error running extension ${item.Title}, see Ooutput window for details");
            }
        }
    }
}
