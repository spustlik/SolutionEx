using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public abstract class CommandBase
    {
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static async Task<TC> InitializeAsync<TC>(AsyncPackage package) where TC : CommandBase, new()
        {
            // Switch to the main thread - the call to AddCommand in CommandAddConfig's constructor requires
            // the UI thread.
            await package.SwitchToUiThreadAsync();
            var commandService = await package.GetMenuCommandServiceAsync();
            var cmd = new TC();
            cmd.package = package;
            var menuCommandID = new CommandID(cmd.CommandSet, cmd.CommandId);
            var menuItem = new MenuCommand(cmd.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
            return cmd;
        }

        // in commands.vsct
        public Guid CommandSet = new Guid("7a30b1a0-c6bb-41ee-a9b4-f15017e2fee5");
        protected AsyncPackage package { get; private set; }
        protected IAsyncServiceProvider ServiceProvider => package;
        public readonly int CommandId;

        protected CommandBase(int commandId)
        {
            this.CommandId = commandId;
        }
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected abstract void Execute(object sender, EventArgs e);
    }
}