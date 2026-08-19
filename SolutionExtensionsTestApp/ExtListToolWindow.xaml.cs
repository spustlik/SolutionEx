using Microsoft.Win32;
using SolutionExtensions;
using SolutionExtensions.Model;
using SolutionExtensions.UI.Extensions;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensionsTestApp
{
    /// <summary>
    /// Interaction logic for ExtListToolWindow.xaml
    /// </summary>
    public partial class ExtListToolWindow : UserControl, IExtensionsService
    {
        public ExtListToolWindow()
        {
            InitializeComponent();
            extensionList.ViewModel.AddMenuItem("Reload", Load_Click);
            extensionList.ViewModel.AddMenuItem("(dbg) Change VM", ChangeVM_Click);
        }
        private void UserControl_Initialized(object sender, EventArgs e)
        {
            extensionList.Init(new ExtensionsModel(), this);
            Load(extensionList.ViewModel.Model);
        }

        /** added menu **/
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Load(extensionList.ViewModel.Model);
        }
        private void ChangeVM_Click(object sender, RoutedEventArgs e)
        {
            extensionList.ViewModel.Model.Extensions[1].Argument = "Arg";
            extensionList.ViewModel.Model.Extensions[1].Title= "Tit";
            extensionList.ViewModel.Model.Extensions[1].ArgumentTitle= "ArgTit";
            extensionList.ViewModel.Model.Extensions[1].ClassName = "MyClass";
            extensionList.ViewModel.Model.Extensions[1].CompileBeforeRun = true;
            extensionList.ViewModel.Model.Extensions[1].DllPath = "MyDll.dll";
            extensionList.ViewModel.Model.Extensions[1].ShortCutKey = "ALT+1";
            extensionList.ViewModel.Model.Extensions[1].CompileBeforeRun = true;
            extensionList.ViewModel.Model.Extensions[1].Files.Add("karel");
            extensionList.ViewModel.Model.Extensions[1].Files[0]="pepa";
        }
        /** svc interface **/
        void IExtensionsService.UpdateItemFromDll(ExtensionItem item)
        {
            if(item.Title==null)
                item.Title = item.ClassName + " extension";
            if (item.ClassName == "Class1")
            {
                item.ArgumentTitle = "My Parameter";
            }
            else
            {
                item.ArgumentTitle = null;
            }
            
        }

        private string GetFileName() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../solex.cfg");

        private void Load(ExtensionsModel model)
        {
            var fn = GetFileName();
            if (!File.Exists(fn))
                return;
            ExtensionsSerialization.LoadFromFile(model, fn);
        }

        void IExtensionsService.Save(ExtensionsModel model)
        {
            ExtensionsSerialization.SaveToFile(model, GetFileName());
        }

        void IExtensionsService.Run(ExtensionItem item, bool debug)
        {
            if (item.IsGenerator)
            {
                item.Files.Add("newfile.xxx");
                MessageBoxEx.Instance.Show("File added");
                return;
            }
            MessageBoxEx.Instance.Show(debug ? "Debug extension" : "Run extension");
        }

        bool IExtensionsService.ShowBrowseDll(ExtensionItem item, bool force)
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = item.DllPath;
            if (!force && File.Exists(item.DllPath))
                return true;
            if (dlg.ShowDialog() == false)
                return false;
            item.DllPath = dlg.FileName;
            return true;
        }

        string[] IExtensionsService.FindExtensionClasses(ExtensionItem item)
        {
            return new[] { "Class1", "Class2" };
        }

        string IExtensionsService.GetValidation(ExtensionItem item)
        {
            if (item.Title != null && item.Title.Contains("bad"))
                return "Bad title";
            return null;
        }

    }
}
