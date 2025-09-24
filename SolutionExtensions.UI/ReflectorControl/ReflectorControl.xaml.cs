using SolutionExtensions.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensions.Reflector
{
    /// <summary>
    /// Interaction logic for ReflectorControl.xaml
    /// </summary>
    public partial class ReflectorControl : UserControl
    {
        public ReflectorFactory Factory { get; }
        public ReflectorControl()
        {
            InitializeComponent();
            DataContext = new ReflectorVM();
            Factory = new ReflectorFactory();
            Factory.COM.RegisterInterfacesFromAppDomain();
        }

        public ReflectorVM ViewModel { get => DataContext as ReflectorVM; }
        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            var iconButton = TryFindResource("iconButton") as Style;
            if (iconButton == null) return;
            var basedOn = TryFindResource("LikeVsActionButton") as Style;
            if (basedOn == null) return; //needed only in VS, defining only colors
            if (!iconButton.IsSealed)
                iconButton.BasedOn = basedOn;
        }
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedNode = (sender as TreeView).SelectedItem as ReflectorNode; ;
        }

        private void CallExpand<T>(object sender, Action<T> action) where T : class
        {
            var ctx = (sender as FrameworkElement).DataContext;
            var node = ctx as T;
            if (node == null)
                return;
            action(node);
            var container = treeView.ItemContainerGenerator.ContainerFromItem(node);
            if (container == null)
                container = GetContainerFromNode(treeView.ItemContainerGenerator, node as ReflectorNode);
            var item = container as TreeViewItem;
            if (item != null)
                item.IsExpanded = true;
        }

        private DependencyObject GetContainerFromNode(ItemContainerGenerator generator, ReflectorNode node)
        {
            node.GetPath(out var path);
            var g = generator;
            foreach (var n in path)
            {
                var container = g.ContainerFromItem(n);
                if (container == null || !(container is ItemsControl ic))
                    return null;
                g = ic.ItemContainerGenerator;
                if (g == null)
                    return null;
                if (n == node)
                    return container;
            }
            return null;
        }

        private void ExpandProperties_Click(object sender, RoutedEventArgs e)
        {
            CallExpand<ReflectorTypeNode>(sender, node => Factory.ExpandProperties(node));
        }

        private void ExpandInterfaces_Click(object sender, RoutedEventArgs e)
        {
            CallExpand<ReflectorValueNode>(sender, node => Factory.ExpandInterfaces(node));
        }

        private void ExpandMethods_Click(object sender, RoutedEventArgs e)
        {
            CallExpand<ReflectorTypeNode>(sender, node => Factory.ExpandMethods(node));
        }

        private void ExpandEnumerable_Click(object sender, RoutedEventArgs e)
        {
            CallExpand<ReflectorValueNode>(sender, node => Factory.ExpandEnumerable(node));
        }

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            var dataCtx = (e.OriginalSource as FrameworkElement)?.DataContext;
            var mnu = ContextMenu;
            if (mnu != null && dataCtx is ReflectorNode node)
            {
                mnu.DataContext = dataCtx;
                ViewModel.SelectedNode = node;
            }
        }

        public event EventHandler<OpenDocumentEventArgs> OnOpenDocument;
        private void GenerateXmlTree_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var x = Factory.BuildNodeXml(ViewModel);
                OpenDoc(x.ToString(), "tree.xml", append: false);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }
        private void OpenDoc(string content, string fileName, bool append)
        {
            if (OnOpenDocument == null)
                throw new Exception($"OnOpenDocument is null");
            OnOpenDocument?.Invoke(this, new OpenDocumentEventArgs()
            {
                Append = append,
                FileName = fileName,
                Content = content
            });
        }

        private void GenerateText_Click(object sender, RoutedEventArgs e)
        {
            var node = ViewModel.SelectedNode;
            if (node == null)
                return;
            var s = Factory.BuilderText.Build(node);
            if (String.IsNullOrEmpty(s))
                return;
            Clipboard.SetText(s);
        }

        private void GenerateCs_Click(object sender, RoutedEventArgs e)
        {
            var node = ViewModel.SelectedNode;
            if (node == null)
                return;
            try
            {
                Factory.Builder.Clear();
                var usedTypes = Factory.Builder.UsedTypes;
                var doneTypes = new HashSet<Type>();
                void makeDone(Type t)
                {
                    doneTypes.Add(t);
                    usedTypes.Remove(t);
                }
                int counter = 1;
                var s = Factory.BuildNodeSource(node);
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
                        s += $"\n// MetadataToken={t.MetadataToken}\n" + Factory.Builder.BuildDeclaration(t);
                    makeDone(t);
                }
                if (s == null)
                    return;
                OpenDoc(s, "dump.cs", append: true);
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
                Factory.Builder.Clear();
                var root = ViewModel.Children.First();
                string s = "";
                void recurse(ReflectorNode node)
                {
                    if (!String.IsNullOrEmpty(s))
                        s += "\n";
                    s += Factory.BuildNodeSource(node);
                    foreach (var item in node.Children)
                    {
                        recurse(item);
                    }
                }
                recurse(root);
                OpenDoc(s, "dump.cs", append: true);
            }
            catch (Exception ex)
            {
                this.ShowException(ex);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var node = ViewModel.SelectedNode;
            if (node == null)
                return;
            Factory.ClearChildren(node);
        }

    }

    public class OpenDocumentEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public string Content { get; set; }
        public bool Append { get; set; }

    }
}
