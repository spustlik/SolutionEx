using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensions.ToolWindows
{
    [Guid("D4B5F1E3-8F2A-4C6A-9D3E-2B1C6F7E8A9B")]
    public class ExtensionsListToolWindowPane : ToolWindowPaneBase<ExtensionsListToolWindow>
    {
        public ExtensionsListToolWindowPane() : base("Solution extensions", new ExtensionsListToolWindow())
        {
        }
    }

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

        #region IsSelected        
        public bool IsSelected
        {
            get => SelectedItem != null;
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

        protected override void DoPropertyChanged(string propertyName = null)
        {
            base.DoPropertyChanged(propertyName);
            if (propertyName == nameof(SelectedItem))
            {
                DoPropertyChanged(nameof(IsSelected));
            }
        }

        //public ObservableCollection<string> ClassList { get; } = new ObservableCollection<string>();

    }

    public partial class ExtensionsListToolWindow : UserControl
    {
        public ExtensionsListToolWindow()
        {
            InitializeComponent();
            this.DataContext = new VM();
        }
        public VM ViewModel => this.DataContext as VM;
        //assigned when created ToolWindowPane 
        ToolWindowPane ToolWindowPane => this.Tag as ToolWindowPane;
        SolutionExtensionsPackage Package;
        ExtensionManager ExtensionManager => this.Package.ExtensionManager;
        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Package = ToolWindowPane.Package as SolutionExtensionsPackage;
            ViewModel.Model = Package.Model;
            //subscribe to VM?
            try
            {
                ExtensionManager.LoadFile(ViewModel.Model, true);
            }
            catch (Exception)
            {
                //ignore
            }
        }
        private void AddItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ViewModel.Model.Extensions.Add(new ExtensionItem() { Title = "New extension" });
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
            try
            {
                ExtensionManager.RunExtension(this.ViewModel.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error running extension", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.LoadFile(ViewModel.Model, false);
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtensionManager.SaveFile(ViewModel.Model, true);
        }

        private void PickClass_Opened(object sender, EventArgs e)
        {
            var item = this.ViewModel.SelectedItem;
            if (!File.Exists(item.DllPath))
            {
                if (!BrowseDll())
                    return;
            }
            var saveCurrent = item.ClassName;
            var cb = sender as ComboBox;
            var classes = ExtensionManager.FindExtensionClassesInDll(item.DllPath);
            cb.ItemsSource = classes;
            cb.SelectedValue = saveCurrent;
            //item.ClassName = saveCurrent;
        }


        private void BrowseDll_Click(object sender, RoutedEventArgs e)
        {
            BrowseDll();
        }

        private bool BrowseDll()
        {
            var item = this.ViewModel.SelectedItem;
            var dlg = new OpenFileDialog();
            dlg.Title = "Choose extension DLL";
            dlg.DefaultExt = ".dll";
            dlg.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
            dlg.CheckFileExists = true;
            dlg.FileName = this.ViewModel.SelectedItem.DllPath;
            if (dlg.ShowDialog() != true)
                return false;
            item.DllPath = dlg.FileName;
            if (string.IsNullOrEmpty(item.ClassName))
                item.ClassName = ExtensionManager.FindExtensionClassesInDll(item.DllPath).FirstOrDefault();
            ViewModel.ValidationMessage = null;
            if (!ExtensionManager.IsDllPathInSolutionScope(item))
            {
                ViewModel.ValidationMessage = $"Warning: Dll path is not in Solution (sub)folder";
            }
            ExtensionManager.EnsureTitle(item);
            return true;
        }

    }
}
