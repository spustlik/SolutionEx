using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
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
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await this.SwitchToUiThreadAsync();
            dte = await this.GetServiceAsync<DTE, DTE>();
            ExtensionManager = new ExtensionManager(this);
            Model = new ExtensionsModel();
            // err: AddToOutputPane("Started",true);
            dte.Events.SolutionEvents.Opened += this.OnSolutionOpened;
            await CommandBase.InitializeAsync<CommandAddConfig>(this);
            this.RegisterToolWindows();
        }

        void OnSolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            AddToOutputPane($"Solution opened: {dte.Solution.FullName}", true);
            var si = dte.Solution.FindSolutionItemsProject();
            if (si != null)
                AddToOutputPane($"Found Solution Items project: {si.Name}");
            else
                AddToOutputPane("No Solution Items project found");
        }
        public void AddToOutputPane(string msg, bool clear = false)
        {
            dte.AddToOutputPane(msg, this.GetType().Name, clear);
            if (clear)
                dte.AddToOutputPane("$---started at {Datetime.Now}---", this.GetType().Name);
        }

        public void TestMethod()
        {
            System.Windows.MessageBox.Show("Test method called from extension package");
        }
    }
}
