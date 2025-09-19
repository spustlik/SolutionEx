using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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

        private void SetRootObject(string caption, object obj)
        {
            reflectorControl.ViewModel.Children.Clear();
            reflectorControl.ViewModel.Children.Add(reflectorControl.Factory.CreateRoot(caption, obj));
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            this.DoZoomerClick();
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
        private void TestToolwindows()
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

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).OpenContextMenu();
        }

        private void GenerateCs_Click(object sender, RoutedEventArgs e)
        {
            var node = reflectorControl.ViewModel.SelectedNode;
            if (node == null)
                return;
            try
            {
                reflectorControl.Factory.Builder.Clear();
                var usedTypes = reflectorControl.Factory.Builder.UsedTypes;
                var doneTypes = new HashSet<Type>();
                void makeDone(Type t)
                {
                    doneTypes.Add(t);
                    usedTypes.Remove(t);
                }
                int counter = 1;
                var s = reflectorControl.Factory.BuildNodeSource(node);
                makeDone(usedTypes.First());
                while (usedTypes.Count > 0)
                {
                    counter++;
                    if (counter >= 100)
                    {
                        s += $"\n// MAXIMUM {counter} reached";
                        break;
                    }
                    var t = usedTypes.First();
                    var skip = doneTypes.Contains(t) ||
                        doneTypes.Any(x => x.MetadataToken == t.MetadataToken) ||
                        doneTypes.Any(x => x.GUID == t.GUID) ||
                        String.IsNullOrEmpty(t.Namespace) ||
                        t.Namespace == nameof(System) ||
                        t.Namespace.StartsWith(nameof(System) + ".");
                    if (!skip)
                        s += $"\n// MetadataToken={t.MetadataToken}\n" + reflectorControl.Factory.Builder.BuildDeclaration(t);
                    makeDone(t);
                }
                if (s == null)
                    return;
                ProcessSource(s);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
        private void GenerateCsTree_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                reflectorControl.Factory.Builder.Clear();
                var root = reflectorControl.ViewModel.Children.First();
                string s = "";
                void recurse(ReflectorNode node)
                {
                    if (!String.IsNullOrEmpty(s))
                        s += "\n";
                    s += reflectorControl.Factory.BuildNodeSource(node);
                    foreach (var item in node.Children)
                    {
                        recurse(item);
                    }
                }
                recurse(root);
                ProcessSource(s);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
        private void ProcessSource(string s)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var fn = Path.Combine(Path.GetTempPath(), "dump.cs");
            if (File.Exists(fn))
            {
                var current = File.ReadAllText(fn);
                s += "//-------\n" + current;
            }
            File.WriteAllText(fn, s);
            var dte = Package.GetService<DTE, DTE>();
#pragma warning disable VSTHRD010
            var found = dte.Documents.Cast<Document>().FirstOrDefault(d => d.FullName.Equals(fn, StringComparison.OrdinalIgnoreCase));
#pragma warning restore VSTHRD010
            try
            {
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

        private void DumpObj(string formula, string name)
        {
            var dte = Package.GetService<DTE, DTE>();
            var fb = new Formula.ReflectionBinder();
            //not working, because it is needed to cast/queryIntf to com
            try
            {
                var result = fb.Evaluate(new { dte }, formula);
                SetRootObject(name, result);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

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
        private void DumpDTE_Click(object sender, RoutedEventArgs e)
        {
            var dte = Package.GetService<DTE, DTE>();
            DumpObj("DTE", () => dte);
        }


        private void DumpAD_Click(object sender, RoutedEventArgs e)
        {
            var dte = Package.GetService<DTE, DTE>();
#pragma warning disable VSTHRD010
            DumpObj("Active document", () => dte.ActiveDocument);
#pragma warning restore VSTHRD010
            //ActiveWindow is always this 
            //DumpObj("Active window", () => dte.ActiveWindow);
        }

        void DumpEM_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var obj = Package.GetService<SExtensionManager, SExtensionManager>();
                DumpObj("Extension manager", () => obj);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
        private void DumpVSShell_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
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
                DumpObj("VsShell", () => dump);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

        void DumpTest_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = Package.GetService<DTE, DTE>();
                Package.GetService<SExtensionManager, SExtensionManager>();
                //var em = Package.GetService< SExtensionManager,IExtensionMa >
                var svc = dte.GetOLEServiceProvider(true);
                var shell = svc.QueryService<SVsShell>() as IVsShell;
                var packages = shell.GetPackages().ToArray();
                var found = packages.FirstOrDefault(p => p.GetType().GUID == typeof(SolutionExtensionsPackage).GUID);
                if (found != null)
                {
                    DumpObj("found self", () => found);
                }
                else
                {
                    DumpObj("Packages", () => packages);
                }
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

        private void GenerateXmlTree_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var x = reflectorControl.Factory.BuildNodeXml(reflectorControl.ViewModel);
                var fn = Path.Combine(Path.GetTempPath(), "tree.xml");
                x.Save(fn);
                var dte = Package.GetService<DTE, DTE>();
                dte.Documents.Open(fn);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

        private void GenerateText_Click(object sender, RoutedEventArgs e)
        {
            var node = reflectorControl.ViewModel.SelectedNode;
            if (node == null)
                return;
            var s = reflectorControl.Factory.BuilderText.Build(node);
            if (String.IsNullOrEmpty(s))
                return;
            Clipboard.SetText(s);
            ThreadHelper.ThrowIfNotOnUIThread();
            _ = Package.ShowStatusBarAsync("Copied to clipboard", isImportant: true);
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var node = reflectorControl.ViewModel.SelectedNode;
            if (node == null)
                return;
            reflectorControl.Factory.ClearChildren(node);
        }

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            var dataCtx = (e.OriginalSource as FrameworkElement)?.DataContext;
            var mnu = reflectorControl.ContextMenu;
            if (mnu != null && dataCtx is ReflectorNode node)
            {
                mnu.DataContext = dataCtx;
                reflectorControl.ViewModel.SelectedNode = node;
            }
        }

    }
}
