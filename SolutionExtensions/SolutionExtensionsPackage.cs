using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SolutionExtensions
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(SolutionExtensionsPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindow(typeof(ToolWindows.ExtensionsListToolWindowPane))]
    [ComVisible(true)]
    public sealed class SolutionExtensionsPackage : ToolkitPackage
    {
        /// <summary>
        /// SolutionExtensionsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ac39d443-46d7-4bf6-9691-552e1d504216";
        private DTE dte;
        public ExtensionManager ExtensionManager { get; private set; }
        public ExtensionsModel Model { get; private set; }
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            dte = await this.GetServiceAsync<DTE, DTE>();
            ExtensionManager = new ExtensionManager(this);
            Model = new ExtensionsModel();
            // err: AddToOutputPane("Started",true);
            dte.Events.SolutionEvents.Opened += this.OnSolutionOpened;

            this.RegisterToolWindows();
            await CommandBase.InitializeAsync<CommandShow>(this);
            var tasks = new List<Task>();
            for (int i = 0; i <= CommandIds.Command_Extension50 - CommandIds.Command_Extension1; i++)
            {
                tasks.Add(CommandBase.InitializeAsync(this, new RunExtensionCommand(CommandIds.Command_Extension1 + i, i)));
            }
            await Task.WhenAll(tasks);
            SyncWithDte(throwErrors: false);
            dte.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
        }

        private void DTEEvents_OnStartupComplete()
        {
            AddToOutputPane($"---Started at {DateTime.Now}", clear: true);
        }

        private void SyncWithDte(bool throwErrors = true)
        {
            try
            {
                if (ExtensionManager.LoadFile(Model, true))
                {
                    ExtensionManager.SyncToDte(Model);
                }
            }
            catch (Exception)
            {
                if (throwErrors)
                    throw;
            }
        }

        void OnSolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Log($"Solution opened: {dte.Solution.FullName}");
            try
            {
                SyncWithDte();
            }
            catch (Exception ex)
            {
                AddToOutputPane($"Error syncing to dte:" + ex);
            }
        }

        public void Log(string msg)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
                return;
            AddToOutputPane(msg);
        }

        public void AddToOutputPaneThreadSafe(string msg)
        {
            if (!msg.EndsWith("\n")) msg += "\n";
            _ = Task.Factory.StartNew(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
                AddToOutputPane(msg);
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public void AddToOutputPane(string msg, bool clear = false)
        {
            dte.AddToOutputPane(msg, this.GetType().Namespace, clear);
        }

        public async Task ShowStatusBarErrorAsync(string message)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            IVsStatusbar statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            if (statusBar != null)
            {
                // Set the status bar text
                //statusBar.SetText(message);
                statusBar.SetColorText(message, 0xFF0000, 0);
                // Optionally, you can flash the status bar to draw attention
                statusBar.FreezeOutput(0); // Unfreeze if previously frozen
                statusBar.FreezeOutput(1); // Freeze to highlight the message
                await Task.Delay(2000);    // Keep it frozen for 2 seconds
                statusBar.FreezeOutput(0); // Unfreeze again
            }
        }
        public void AddConfigToSolutionItem()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte.Solution == null)
                return;
            var cfgFilePath = ExtensionManager.GetCfgFilePath();
            if (!File.Exists(cfgFilePath))
            {
                _ = ShowStatusBarErrorAsync($"Config not yet saved. Add some extensions");
                return;
            }
            var si = dte.Solution.FindSolutionFolder(addIfNotExists: true);
            var fi = si.FindProjectItem(cfgFilePath) ?? si.ProjectItems.AddFromFile(cfgFilePath);
        }

        public void TestMethod()
        {
            System.Windows.MessageBox.Show("Test method called from extension package");
        }

    }
}
