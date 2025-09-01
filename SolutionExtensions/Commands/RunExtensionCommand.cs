using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;

namespace SolutionExtensions
{
    public sealed class RunExtensionCommand : CommandBase
    {
        public int ExtensionIndex { get; }
        public RunExtensionCommand(int commandId, int extensionIndex) : base(commandId)
        {
            ExtensionIndex = extensionIndex;
        }
        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var package = this.package as SolutionExtensionsPackage;
            var item = package.Model.Extensions.ElementAtOrDefault(ExtensionIndex);
            if (item == null)
                return;
            package.ExtensionManager.RunExtension(item);
        }
    }
}
