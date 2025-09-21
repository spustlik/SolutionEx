using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SolutionExtensions.Commands
{
    public abstract class CommandBase
    {
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        public static async Task<TC> InitializeAsync<TC>(AsyncPackage package) where TC : CommandBase, new()
        {
            var cmd = new TC();
            return await InitializeAsync(package, cmd);
        }

        public static async Task<TC> InitializeAsync<TC>(AsyncPackage package, TC cmd) where TC : CommandBase
        {
            await package.SwitchToUiThreadAsync();
            var commandService = await package.GetMenuCommandServiceAsync();
            cmd.package = package;
            var menuCommandID = new CommandID(cmd.CommandSet, cmd.CommandId);
            var menuComand = new OleMenuCommand(cmd.Execute, menuCommandID);
            commandService.AddCommand(menuComand);
            cmd.MenuComand = menuComand;
            menuComand.BeforeQueryStatus += cmd.MenuComand_BeforeQueryStatus;
            return cmd;
        }

        private void MenuComand_BeforeQueryStatus(object sender, EventArgs e)
        {
            //??? needs <CommandFlag>TextChanges</CommandFlag>
            var cmd = sender as OleMenuCommand;
            if (cmd == null)
                return;
            OnMenuComand_BeforeQueryStatus(cmd);
        }

        protected AsyncPackage package { get; set; }
        protected IAsyncServiceProvider ServiceProvider => package;

        public Guid CommandSet { get; }
        public int CommandId { get; }
        public OleMenuCommand MenuComand { get; private set; }
        protected CommandBase(int commandId, Guid commandSet)
        {
            CommandId = commandId;
            CommandSet = commandSet;
        }
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected abstract void Execute(object sender, EventArgs e);
        protected virtual void OnMenuComand_BeforeQueryStatus(OleMenuCommand cmd)
        {
        }

    }
}