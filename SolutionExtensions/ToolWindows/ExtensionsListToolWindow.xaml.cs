using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using SolutionExtensions.ColorDump;
using SolutionExtensions.Model;
using SolutionExtensions.UI;
using SolutionExtensions.UI.Extensions;
using System;
using System.Collections.Generic;
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
            list.ViewModel.AddMenuItem("Assign selected file(s) to generator", AssignToGenerator_Click);
#if DEBUG
            //<Separator/>
            list.ViewModel.AddMenuItem("(DBG)Reload", Load_Click);
            list.ViewModel.AddMenuItem("(DBG)Sync to VS", SyncToDte_Click);
            list.ViewModel.AddMenuItem("(DBG)Save", Save_Click);
            list.ViewModel.AddMenuItem("(DBG)Show colors", ShowColors_Click);
            list.ViewModel.AddMenuItem("(DBG)Set and run code generator", RunCodeGenerator_Click);
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

        private void AssignToGenerator_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var item = (sender as FrameworkElement).DataContext as ExtensionItem ?? list.ViewModel.SelectedItem;
            if (item == null)
                return;
            if (!item.IsGenerator)
                throw new Exception($"Selected extension is not generator");
            var dte = Package.GetService<DTE, DTE>();
            if (dte.SelectedItems.Count <= 0)
                throw new Exception($"No item(s) selected");

        }
        #region debug methods
        private void RunCodeGenerator_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var item = (sender as FrameworkElement).DataContext as ExtensionItem ?? list.ViewModel.SelectedItem;
            if (item == null)
                return;
            try
            {
                var dte = Package.GetService<DTE, DTE>();
                if (dte.SelectedItems.Count <= 0)
                    throw new Exception($"No item(s) selected");
                var selected = dte.SelectedItems.Item(1).ProjectItem;
                if (selected == null)
                    throw new Exception($"Selected item is not ProjectItem");
                var customTool = selected.Properties.Item("CustomTool").Value as string;
                if (customTool != nameof(SolutionFileGenerator))
                {
                    Package.Log($"Adding custom tool to item");
                    selected.Properties.Item("CustomTool").Value = nameof(SolutionFileGenerator);
                }
                Package.Log($"Running CustomTool");
                dynamic o = selected.Object;
                o.RunCustomTool();
                //but here SFG needs to known what real generator to call
                // without explicit info from code above

                //so logic can be
                // if clicking Run on Extension of Generator type,
                // run custom tool
                // it must get right generator from file name and call it
                /*
                Package.Log($"Running extension as custom tool generator on current DTE item, '{item.Title}' from {Path.GetFileName(item.DllPath)},{item.ClassName}");
                if (ExtensionManager.CompileIfNeeded(item))
                    Package.Log($"Extension recompiled");
                var (method, type) = ExtensionManager.FindExtensionMethod(item, methodName: ExtensionObject.GENERATE_METHOD, throwIfNotFound: true);
                var instance = method.IsStatic ? null : Activator.CreateInstance(type);
                var parameters = new object[method.GetParameters().Length];
                parameters[0] = dte;
                parameters[1] = "filecontent";
                parameters[2] = "filename";
                parameters[3] = "namespace";
                //instance? getProperty or field Extension
                //(extension can be changed during generation?)
                try
                {
                    var result = method.Invoke(instance, parameters);
                }
                catch (TargetInvocationException tex)
                {
                    throw tex.InnerException;
                }
                Package.Log($"Done.");
                */
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.SaveFile(list.ViewModel.Model);
        }
        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.LoadFile(list.ViewModel.Model);
        }
        #endregion 


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
                MessageBox.Show("All extensions look valid", "Imnformation", MessageBoxButton.OK, MessageBoxImage.Information);
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
            ExtensionManager.UpdateItemFromDll(item);
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
                if (item.IsGenerator)
                    GenerateInner(item, debug, argument);
                else
                    RunInner(item, debug, argument);
            }
            catch (Exception ex)
            {
                var title = $"Error {(debug ? "running" : "debugging")} extension '{item.Title}'";
                Package.AddToOutputPane($"{title}:\nfrom:{item.DllPath}\n" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
                this.ShowException(ex, "See output pane for details", title);
            }
        }

        private void GenerateInner(ExtensionItem item, bool debug, string argument)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning disable VSTHRD010 
            Package.Log($"extension '{item.Title}' Generate() with argument '{argument}' and flags {String.Join(",", item.GetFlags())} from {Path.GetFileName(item.DllPath)},{item.ClassName}");
            if (ExtensionManager.CompileIfNeeded(item))
                Package.Log($"Extension recompiled");
            //if (debug || item.OutOfProcess)
            //    ExtensionDebugger.RunExtension(item, argument, Package, ExtensionManager, debug);
            //else
            //    ExtensionManager.RunExtension(item, argument);
            var dte = Package.GetService<DTE, DTE>();
            var selectedItems = dte.SelectedItems
                .Cast<SelectedItem>()
                .Where(si => si.ProjectItem != null)
                .Where(si => si.ProjectItem.Kind == Constants.vsProjectItemKindPhysicalFile)
                .Select(si => new
                {
                    FileName = ExtensionManager.GetSolutionRelativePath(si.ProjectItem.FileNames[0]),
                    ProjectItem = si.ProjectItem
                })
                .ToArray();
            if (selectedItems.Length == 0)
                throw new Exception("There are no selected item(s) in solution explorer.\nPlease select one or more and than Run again.");
            if (selectedItems.Length > 1)
            {
                if (MessageBox.Show(
                    $"There are {selectedItems.Length} selected items in Solution explorer.\n" +
                    $"{String.Join(", ", selectedItems.Select(si => Path.GetFileName(si.FileName)))}\n" +
                    $"Do you want to assign them to generator '{item.Title}' ?\n" +
                    $"(Generator will not run, only assigned)",
                    "Confirm",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                var (removed, added) = ExtensionManager.AddFilesToItem(item, Package.Model.Extensions, selectedItems.Select(si => si.FileName).ToArray());
                if (removed > 0)
                    MessageBox.Show($"{removed} items was already used in another generator and removed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                SetCustomTool(selectedItems.Select(si => si.ProjectItem));
                return;
            }
            {
                //run generator with one file
                var (removed, added) = ExtensionManager.AddFilesToItem(item, Package.Model.Extensions, selectedItems.Select(si => si.FileName).ToArray());
                if (removed > 0)
                    MessageBox.Show($"{removed} items was already used in another generator and removed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                SetCustomTool(selectedItems.Select(si => si.ProjectItem));
                dynamic o = selectedItems.First().ProjectItem.Object;
                o.RunCustomTool();
                Package.Log($"Done.");
            }
#pragma warning restore VSTHRD010
        }

        private void SetCustomTool(IEnumerable<ProjectItem> projectItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning disable VSTHRD010 
            foreach (var projectItem in projectItems)
            {
                var customTool = projectItem.Properties.Item("CustomTool").Value as string;
                if (customTool != nameof(SolutionFileGenerator))
                {
                    Package.Log($"Adding custom tool to item '{projectItem.Name}'");
                    projectItem.Properties.Item("CustomTool").Value = nameof(SolutionFileGenerator);
                }
            }
#pragma warning restore VSTHRD010

        }

        private void RunInner(ExtensionItem item, bool debug, string argument)
        {
            Package.Log($"extension '{item.Title}' Run() with argument '{argument}' and flags {String.Join(",", item.GetFlags())} from {Path.GetFileName(item.DllPath)},{item.ClassName}");
            if (ExtensionManager.CompileIfNeeded(item))
                Package.Log($"Extension recompiled");
            if (debug || item.OutOfProcess)
                ExtensionDebugger.RunExtension(item, argument, Package, ExtensionManager, debug);
            else
                ExtensionManager.RunExtension(item, argument);
            Package.Log($"Done.");
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
                case ExtensionManager.CheckResult.MethodNotFound:
                    return $"Class '{item.ClassName}' must have 'Run' or 'Generate' method";
                case ExtensionManager.CheckResult.ArgumentPropertyNotFound:
                    return $"Class '{item.ClassName}' should have 'Argument' property";
            }
            return null;
        }

    }
}