using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SolutionExtensions.ToolWindows
{
    [Guid("2016A00B-1AF6-4C0A-9F58-77E201374A87")]
    public class ReflectorToolWindowPane : ToolWindowPaneBase<ReflectorToolWindow>
    {
        public static string CAPTION = "DTE reflection";

        public ReflectorToolWindowPane() : base(CAPTION, new ReflectorToolWindow())
        {
        }
    }

    public partial class ReflectorToolWindow : UserControl
    {
        public ReflectorToolWindow()
        {
            InitializeComponent();
        }
        ToolWindowPane ToolWindowPane => this.Tag as ToolWindowPane;
        SolutionExtensionsPackage Package;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Package = ToolWindowPane.Package as SolutionExtensionsPackage;
        }

        private void DTE_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetService<DTE, DTE>();
            SetRootObject("DTE", dte);
        }

        private void TW1_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = Package.GetService<DTE, DTE>() as DTE2;
                var caption = ExtensionsListToolWindowPane.CAPTION;
                var guid = typeof(ExtensionsListToolWindowPane).GUID;
                var w = dte.ToolWindows.GetToolWindow(caption);
                if (w == null)
                    w = dte.ToolWindows.GetToolWindow(guid.ToString("B"));
                if (w == null)
                    w = dte.ToolWindows.GetToolWindow(guid.ToString());
                if (w == null)
                    caption += "=null";
                SetRootObject(caption, w);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

        private void SetRootObject(string caption, object obj)
        {
            reflectorControl.ViewModel.Children.Clear();
            reflectorControl.ViewModel.Children.Add(reflectorControl.Factory.CreateRoot(caption, obj));
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            LayoutTransform = new ScaleTransform(2.0, 2.0);            
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var name = "VsFont.EnvironmentFontSize";
            var r1 = TryFindResource(name);
            var r2 = Application.Current.TryFindResource(name);
            foreach (var key in Application.Current.Resources.Keys)
            {
                Package.AddToOutputPane($"{key}={Application.Current.Resources[key]}");
            }

        }
    }
}
