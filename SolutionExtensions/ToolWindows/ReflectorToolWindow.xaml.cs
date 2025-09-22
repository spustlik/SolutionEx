using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionExtensions.Reflector;
using SolutionExtensions.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensions.ToolWindows
{

    public partial class ReflectorToolWindow : UserControl
    {
        public ReflectorToolWindow()
        {
            InitializeComponent();
#if DEBUG
            debugMenuItem.Visibility = Visibility.Visible;
#endif
        }
        private SolutionExtensionsPackage Package;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Package = SolutionExtensionsPackage.GetFor(this);
            reflectorControl.OnOpenDocument += ReflectorControl_OnOpenDocument;
        }

        private void ReflectorControl_OnOpenDocument(object sender, OpenDocumentEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var fn = Path.Combine(Path.GetTempPath(), Path.GetFileName(e.FileName));
                if (e.Append)
                    File.AppendAllText(fn, e.Content);
                else
                    File.WriteAllText(fn, e.Content);
                var dte = Package.GetService<DTE, DTE>();
#pragma warning disable VSTHRD010
                var found = dte.Documents.Cast<Document>()
                    .FirstOrDefault(d => d.FullName.Equals(fn, StringComparison.OrdinalIgnoreCase));
#pragma warning restore VSTHRD010
                if (found != null)
                {
                    var sel = found.Selection as TextSelection;
                    if (sel != null)
                    {
                        sel.MoveToAbsoluteOffset(1);
                        return;
                    }
                }
                dte.Documents.Open(fn);
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
            this.DoZoomerClick();
        }

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).OpenContextMenu();
        }

        #region Dumpers

        private void DumpObj(string name, Func<object> objFactory)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                var obj = objFactory();
                SetRootObject(name, obj);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
#pragma warning disable VSTHRD010
        private void DumpDTE_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("DTE", () => Package.GetService<DTE, DTE>());
        }

        private void DumpAD_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("Active document", () =>
            {
                var dte = Package.GetService<DTE, DTE>();
                return dte.ActiveDocument;
            });
            //ActiveWindow is always this 
            //DumpObj("Active window", () => dte.ActiveWindow);
        }

        void DumpEM_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("Extension manager", () =>
                Package.GetService<SExtensionManager, SExtensionManager>()
                );
        }
        void DumpVSShell_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("VsShell", () =>
            {
                var shell = Package.GetService<SVsShell, IVsShell>();
                var queryService = Package.GetService<SVsPackageInfoQueryService, IVsPackageInfoQueryService>();
                var info = queryService.GetPackageInfo(Package.GetType().GUID);
                var dump = new
                {
                    shell,
                    queryService,
                    info,
                    packages = shell.GetPackages().ToArray(),
                };
                return dump;
            });
        }

        void DumpPackage_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("test package", () =>
            {
                var dte = Package.GetService<DTE, DTE>();
                Package.GetService<SExtensionManager, SExtensionManager>();
                //var em = Package.GetService< SExtensionManager,IExtensionMa >
                var svc = dte.GetOLEServiceProvider(true);
                var shell = svc.QueryService<SVsShell>() as IVsShell;
                var packages = shell.GetPackages().ToArray();
                var found = packages.FirstOrDefault(p => p.GetType().GUID == typeof(SolutionExtensionsPackage).GUID);
                return new
                {
                    found,
                    packages
                };
            });
        }

        void DumpToolWindows_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("Tool Windows", () =>
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
                return new { found = w, caption, guid };
            });
        }
        private void DumpVsStyles_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("VsStyles", () =>
            {
                var s = FindResource(VsResourceKeys.ThemedDialogButtonStyleKey) as Style;
                return new
                {
                    ThemedDialogButtonStyle = s
                };
            });
        }
#pragma warning restore VSTHRD010
        #endregion


    }
}
