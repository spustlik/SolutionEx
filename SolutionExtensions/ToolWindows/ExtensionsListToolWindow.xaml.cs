using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using SolutionExtensions.ColorDump;
using SolutionExtensions.Model;
using SolutionExtensions.UI;
using SolutionExtensions.UI.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensions.ToolWindows
{

    public partial class ExtensionsListToolWindow : UserControl, IExtensionsService
    {
        private SolutionExtensionsPackage Package;
        private ExtensionManager ExtensionManager => this.Package.ExtensionManager;
        public ExtensionsListToolWindow()
        {
            InitializeComponent();
        }
        private void UserControl_Initialized(object sender, EventArgs e)
        {
            Package = SolutionExtensionsPackage.GetGlobal();
            list.Init(Package.Model, this);
            list.ViewModel.AddMenuItem("🔦DTE", ButtonDump_Click, "Inspect DTE objects...");
            list.ViewModel.AddMenuItem("-", null);
            list.ViewModel.AddMenuItem("Add new extension project to solution", AddProj_Click);
            list.ViewModel.AddMenuItem("Add config as solution item", AddConfig_Click);
            list.ViewModel.AddMenuItem("Validate all extensions", CheckAll_Click);
#if DEBUG
            //<Separator/>
            list.ViewModel.AddMenuItem("Reload", Load_Click);
            list.ViewModel.AddMenuItem("Sync to VS", SyncToDte_Click);
            list.ViewModel.AddMenuItem("Save", Save_Click);
            list.ViewModel.AddMenuItem("Show colors", ShowColors_Click);
#endif
        }

        private void AddConfig_Click(object sender, RoutedEventArgs e)
        {
            this.Package.AddConfigToSolutionItem();
        }

        private void ButtonDump_Click(object sender, RoutedEventArgs e)
        {
            _ = this.Package.ShowToolWindowAsync(typeof(ReflectorToolWindowPane), 0, true, CancellationToken.None);
        }

        private void ShowColors_Click(object sender, RoutedEventArgs e)
        {
            var w = new ColorWindow();
            w.Show();
            //await this.Package.ShowStatusBarErrorAsync("Some error");
            //await this.Package.ShowStatusBarAsync("Some text 1", isImportant:true);
            //await this.Package.ShowStatusBarAsync("Some text 2", waitMs:5000);
            //await this.Package.ShowStatusBarAsync("Some text 3");
            //await Task.Delay(1000);
            //await this.Package.ShowStatusBarAsync(null);
        }

        private void SyncToDte_Click(object sender, RoutedEventArgs e)
        {
            SyncToDTE();
        }

        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.LoadFile(list.ViewModel.Model);
        }
        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.SaveFile(list.ViewModel.Model);
        }

        private void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            foreach (var item in list.ViewModel.Model.Extensions)
            {
                var msg = list.ExtensionsService.GetValidation(item);
                if (msg != null)
                    sb.AppendLine($"Extension #{list.ViewModel.Model.Extensions.IndexOf(item) + 1} '{item.Title}': {msg}");
            }
            if (sb.Length == 0)
                MessageBox.Show("All extensions look valid");
            else
                MessageBox.Show(sb.ToString(), "Validation results", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void SyncToDTE()
        {
            try
            {
                Package.Log($"Syncing to DTE");
                ExtensionManager.SyncToDte(list.ViewModel.Model);
            }
            catch (Exception ex)
            {
                Package.AddToOutputPane($"Error syncing to DTE:" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
            }
        }
        private void AddProj_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetService<DTE, DTE>();
            if (dte.Solution == null)
                return;
            var nuget = StringTemplates.Nuget_VS;
            if (MessageBox.Show($"Not yet implemented\n" +
                $"Follow these steps:\n" +
                $"- Add new class library project in .net 4.8 to your solution\n" +
                $"- Add nuget package '{nuget}' to it\n" +
                $"- Add your new class\n" +
                $"- Add method called 'Run(DTE dte, IServiceProvider package)" +
                $"\n" +
                $"Do you want to add class source to clipboard? ",
                "Add new extension project", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            var s = StringTemplates.GetExtensionCsharp(
                Path.GetFileNameWithoutExtension(dte.Solution.FileName),
                "MyExtension1",
                nuget);
            Clipboard.SetText(s);
            /*
            var projectName = Path.GetFileNameWithoutExtension(dte.Solution.FileName) + ".Extensions";
            var projectFile = Path.Combine(Path.GetDirectoryName(dte.Solution.FullName), projectName)+".csproj";
            //dte.Solution.ProjectItemsTemplatePath(ProjectKinds.)
            var project = dte.Solution.AddFromTemplate("...some template of class lib", projectFile, projectName);
            project.ProjectItems.AddFromFile("MyExtension1.cs create templated file in temp");
            */
        }
        private bool BrowseDll(ExtensionItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetService<DTE, DTE>();
            var dlg = new OpenFileDialog();
            if (dte.Solution != null)
                dlg.InitialDirectory = Path.GetDirectoryName(dte.Solution.FileName);
            dlg.Title = "Choose extension DLL";
            dlg.DefaultExt = ".dll";
            dlg.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
            dlg.CheckFileExists = true;
            dlg.FileName = item.DllPath;
            //if (!string.IsNullOrEmpty(dlg.FileName))
            //    dlg.InitialDirectory = Path.GetDirectoryName(dlg.FileName);
            if (dlg.ShowDialog() != true)
                return false;
            ExtensionManager.SetDllPath(item, dlg.FileName);
            if (string.IsNullOrEmpty(item.ClassName))
                item.ClassName = ExtensionManager.FindExtensionClassesInDll(item.DllPath).FirstOrDefault();
            ExtensionManager.EnsureTitle(item);
            return true;
        }


        //------------
        void IExtensionsService.UpdateItemFromDll(ExtensionItem item)
        {
            ExtensionManager.SetItemTitleFromMethod(item);
            ExtensionManager.SetArgumentFromClass(item);
        }

        void IExtensionsService.Save(ExtensionsModel model)
        {
            if (ExtensionManager.GetCfgFilePath() == null)
                return;
            Package.Log("Saving");
            ExtensionManager.SaveFile(model);
            SyncToDTE();
        }

        void IExtensionsService.Run(ExtensionItem item, bool debug)
        {
            if (debug && !ExtensionDebugger.ValidateBreakpoint(item, Package, ExtensionManager))
            {
                if (MessageBox.Show($"No breakpoint found.\n" +
                    $"There should be breakpoint in your extension to stop debugger there.\n" +
                    $"Or add System.Diagnostics.Debugger.Break(); to your code.\n" +
                    $"Do you want to continue?",
                    "Breakpoint missing",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Hand) != MessageBoxResult.Yes)
                    return;
            }
            if (!ExtensionManager.AskArgumentIfNeeded(item, out var argument))
                return;
            try
            {
                Package.Log($"Running extension '{item.Title}' with argument '{argument}' and flags {String.Join(",", item.GetFlags())} from {Path.GetFileName(item.DllPath)},{item.ClassName}");
                if (ExtensionManager.CompileIfNeeded(item))
                    Package.Log($"Extension recompiled");
                if (debug || item.OutOfProcess)
                    ExtensionDebugger.RunExtension(item, argument, Package, ExtensionManager, debug);
                else
                    ExtensionManager.RunExtension(item, argument);
                Package.Log($"Done.");
            }
            catch (Exception ex)
            {
                var title = $"Error {(debug ? "running" : "debugging")} extension '{item.Title}'";
                Package.AddToOutputPane($"{title}:\nfrom:{item.DllPath}\n" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
                this.ShowException(ex, "See output pane for details", title);
            }
        }

        bool IExtensionsService.ShowBrowseDll(ExtensionItem item, bool force)
        {
            if (!force && File.Exists(ExtensionManager.GetRealPath(item.DllPath)))
                return true;
            return BrowseDll(item);
        }

        string[] IExtensionsService.FindExtensionClasses(ExtensionItem item)
        {
            return ExtensionManager.FindExtensionClassesInDll(item.DllPath);
        }
        string IExtensionsService.GetValidation(ExtensionItem item)
        {
            if (String.IsNullOrWhiteSpace(item.DllPath))
                return $"DLL path is empty";
            if (!ExtensionManager.IsDllExists(item))
                return $"DLL file does not exist, maybe you must compile it first?";
            if (!ExtensionManager.IsDllPathInSolutionScope(item))
                if (!ExtensionManager.IsDllPathSelf(item))
                    return $"Warning: DLL path is not in Solution (sub)folder";

            var check = ExtensionManager.CheckItemCode(item);
            switch (check)
            {
                case ExtensionManager.CheckResult.ClassNotFound:
                    return $"Class '{item.ClassName}' not found in DLL";
                case ExtensionManager.CheckResult.RunMethodNotFound:
                    return $"Class '{item.ClassName}' must have 'Run' method";
                case ExtensionManager.CheckResult.ArgumentPropertyNotFound:
                    return $"Class '{item.ClassName}' should have 'Argument' property";
            }
            return null;
        }

    }
}