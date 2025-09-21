using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Model;
using SolutionExtensions._DesignData;
using SolutionExtensions.WPF;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolutionExtensions.ToolWindows
{

    public class VM : SimpleDataObject
    {

        #region ValidationMessage property
        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => Set(ref _validationMessage, value);
        }
        #endregion

        #region SelectedItem property
        private ExtensionItem _selectedItem;
        public ExtensionItem SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }
        #endregion

        #region Model property
        private ExtensionsModel _model;
        public ExtensionsModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }
        #endregion

        #region IsDebug property
        private bool _isDebug;
        public bool IsDebug
        {
            get => _isDebug;
            set => Set(ref _isDebug, value);
        }
        #endregion

    }

    public partial class ExtensionsListToolWindow : UserControl
    {
        public VM ViewModel => this.DataContext as VM;
        private SolutionExtensionsPackage Package;
        private ExtensionManager ExtensionManager => this.Package.ExtensionManager;
        private MoveCollectionHelper mover;
        public ExtensionsListToolWindow()
        {
            InitializeComponent();
            this.DataContext = new VM();
#if DEBUG
            ViewModel.IsDebug = true;
#endif
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        //assigned when created ToolWindowPane 
        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Package = SolutionExtensionsPackage.GetFor(this);
            ViewModel.Model = Package.Model;
            ViewModel.Model.Extensions.OnCollectionItemChanged(null, ViewModelExtensions_PropertyChanged);
            ViewModel.Model.Extensions.CollectionChanged += ViewModelExtensions_CollectionChanged;
            mover = MoveCollectionHelper.Create(this, ViewModel.Model.Extensions);
        }
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.SelectedItem))
            {
                this.Validate();
            }
        }
        private void ViewModelExtensions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Package.Log($"ViewModelExtensions_PropertyChanged: {e.PropertyName}");
            var item = sender as ExtensionItem;
            if (e.PropertyName == nameof(ExtensionItem.ClassName))
            {
                ExtensionManager.SetItemTitleFromMethod(item);
            }
            ThrottleValidate();
            ThrottleUpdateModel();
        }

        private void ViewModelExtensions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //Package.Log($"ViewModelExtensions_CollectionChanged: {e.Action}");
            ThrottleUpdateModel();
        }

        private ThrottleTimer _validator = new ThrottleTimer(TimeSpan.FromSeconds(0.3));
        private void ThrottleValidate()
        {
            _validator.Invoke(() => Validate());
        }
        private ThrottleTimer _saver = new ThrottleTimer(TimeSpan.FromSeconds(0.3));

        private void ThrottleUpdateModel()
        {
            if (ExtensionManager.GetCfgFilePath() == null)
                return;
            _saver.Invoke(() =>
            {
                Package.Log("Saving");
                ExtensionManager.SaveFile(ViewModel.Model);
                SyncToDTE();
            });
        }


        private void AddItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ViewModel.Model.Extensions.Add(new ExtensionItem());
            this.ViewModel.SelectedItem = this.ViewModel.Model.Extensions.Last();
        }

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.ViewModel.SelectedItem == null)
                return;
            this.ViewModel.Model.Extensions.Remove(this.ViewModel.SelectedItem);
        }

        private void Run_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ExtensionItem ?? ViewModel.SelectedItem;
            if (item == null)
                return;
            if (!ExtensionManager.AskArgumentIfNeeded(item, out var argument))
                return;
            try
            {
                Package.Log($"Running extension '{item.Title}' with argument '{argument}' and flags {String.Join(",", item.GetFlags())} from {Path.GetFileName(item.DllPath)},{item.ClassName}");
                if (ExtensionManager.CompileIfNeeded(item))
                    Package.Log($"extension recompiled");
                if (item.OutOfProcess)
                    ExtensionDebugger.RunExtension(item, argument, Package, ExtensionManager, false);
                else
                    ExtensionManager.RunExtension(item, argument);
                Package.Log($"Done.");
            }
            catch (Exception ex)
            {
                var title = $"Error running extension '{item.Title}'";
                Package.AddToOutputPane($"{title}:\nfrom:{item.DllPath}\n" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
                this.ShowException(ex, "See output pane for details", title);
            }
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            if (!ExtensionDebugger.ValidateBreakpoint(item, Package, ExtensionManager))
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
                ExtensionDebugger.RunExtension(item, argument, Package, ExtensionManager, true);
            }
            catch (Exception ex)
            {
                var title = $"Error running extension '{item.Title}' in DEBUG";
                Package.AddToOutputPane($"{title}:\nfrom:{item.DllPath}\n" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
                this.ShowException(ex, "See output pane for details", title);
            }
        }
        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                ExtensionManager.LoadFile(ViewModel.Model);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
        private void SyncToDte_Click(object sender, RoutedEventArgs e)
        {
            SyncToDTE();
        }

        private void SyncToDTE()
        {
            try
            {
                Package.Log($"Syncing to DTE");
                ExtensionManager.SyncToDte(ViewModel.Model);
            }
            catch (Exception ex)
            {
                Package.AddToOutputPane($"Error syncing to DTE:" + ex);
                _ = Package.ShowStatusBarErrorAsync(ex.Message);
            }
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.SaveFile(ViewModel.Model);
        }

        private void PickClass_Opened(object sender, EventArgs e)
        {
            var item = this.ViewModel.SelectedItem;
            if (!File.Exists(ExtensionManager.GetRealPath(item.DllPath)))
            {
                if (!BrowseDll())
                    return;
            }
            var cb = sender as ComboBox;
            var classes = ExtensionManager.FindExtensionClassesInDll(item.DllPath);
            cb.ItemsSource = classes;
        }

        private void BrowseDll_Click(object sender, RoutedEventArgs e)
        {
            BrowseDll();
        }

        private bool BrowseDll()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetService<DTE, DTE>();
            var item = this.ViewModel.SelectedItem;
            var dlg = new OpenFileDialog();
            if (dte.Solution != null)
                dlg.InitialDirectory = Path.GetDirectoryName(dte.Solution.FileName);
            dlg.Title = "Choose extension DLL";
            dlg.DefaultExt = ".dll";
            dlg.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
            dlg.CheckFileExists = true;
            dlg.FileName = this.ViewModel.SelectedItem.DllPath;
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

        private void Validate()
        {
            var item = this.ViewModel.SelectedItem;
            if (item == null)
            {
                ViewModel.ValidationMessage = null;
                return;
            }
            ViewModel.ValidationMessage = GetValidation(item);
        }

        private string GetValidation(ExtensionItem item)
        {
            if (String.IsNullOrWhiteSpace(item.DllPath))
                return $"DLL path is empty";
            if (!ExtensionManager.IsDllPathInSolutionScope(item))
                if (!ExtensionManager.IsDllPathSelf(item))
                    return $"Warning: DLL path is not in Solution (sub)folder";
            if (!ExtensionManager.IsDllExists(item))
                return $"DLL file does not exist, maybe you must compile it first?";

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

        private void Shortcut_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            if (this.ViewModel.SelectedItem == null)
                return;
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.System || IsModifierKey(key))
                return;
            var g = e.KeyboardDevice.Modifiers == ModifierKeys.None ?
                new KeyGesture(key, ModifierKeys.Control) : //none not supported
                new KeyGesture(key, e.KeyboardDevice.Modifiers);
            this.ViewModel.SelectedItem.ShortCutKey = g.GetDisplayStringForCulture(System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool IsModifierKey(Key key)
        {
            return new[] {
                Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift,
                Key.CapsLock, Key.NumLock, Key.LWin, Key.RWin,
                }.Contains(key);
        }

        private void SetSelf_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            item.DllPath = ExtensionManager.SELF;
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

        private void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            foreach (var item in ViewModel.Model.Extensions)
            {
                var msg = GetValidation(item);
                if (msg != null)
                    sb.AppendLine($"Extension #{ViewModel.Model.Extensions.IndexOf(item) + 1} '{item.Title}': {msg}");
            }
            if (sb.Length == 0)
                MessageBox.Show("All extensions look valid");
            else
                MessageBox.Show(sb.ToString(), "Validation results", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ButtonMore_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).OpenContextMenu();
        }

        private void AddConfig_Click(object sender, RoutedEventArgs e)
        {
            this.Package.AddConfigToSolutionItem();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            var copy = new ExtensionItem()
            {
                Title = $"{item.Title} (copy)",
                ClassName = item.ClassName,
                DllPath = item.DllPath,
                //ShortCutKey=src.ShortCutKey
            };
            ViewModel.Model.Extensions.Add(copy);
            ViewModel.SelectedItem = copy;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void ButtonDump_Click(object sender, RoutedEventArgs e)
        {
            var p = this.Package;
            try
            {
                await p.ShowToolWindowAsync(typeof(ReflectorToolWindowPane), 0, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }        
#pragma warning restore VSTHRD100 // Avoid async void methods

        private void Develop_Click(object sender, RoutedEventArgs e)
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

        private void ArgumentAsk_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            item.Argument = "?";
        }

        private void ExtensionItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mover.ProcessMouseEvent(sender, e);
        }
        private void ExtensionItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mover.ProcessMouseEvent(sender, e);
        }
        private void ExtensionItem_MouseMove(object sender, MouseEventArgs e)
        {
            mover.ProcessMouseEvent(sender, e);
        }

        private void MoverAttach_Loaded(object sender, RoutedEventArgs e)
        {
            mover.AttachToMouseEvents(sender as UIElement);
        }
    }
}