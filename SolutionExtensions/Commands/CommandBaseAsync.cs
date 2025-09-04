using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public abstract class CommandBaseAsync : CommandBase
    {
        protected CommandBaseAsync(int commandId, Guid commandSet) : base(commandId, commandSet)
        {
        }

        protected sealed override void Execute(object sender, EventArgs e)
        {
#pragma warning disable VSSDK007 // ThreadHelper.JoinableTaskFactory.RunAsync
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ExecuteAsync(sender, e);
            });
#pragma warning restore VSSDK007 // ThreadHelper.JoinableTaskFactory.RunAsync
        }
        protected abstract Task ExecuteAsync(object sender, EventArgs e);

    }
}