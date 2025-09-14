using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SolutionExtensions
{
    /// <summary>
    /// Interaction logic for ReflectorControl.xaml
    /// </summary>
    public partial class ReflectorControl : UserControl
    {
        public ReflectorControl()
        {
            InitializeComponent();
            ViewModel = new ReflectorVM();
            Factory.COM.RegisterInterfacesFromAppDomain();
        }

        public ReflectorVM ViewModel
        {
            get => DataContext as ReflectorVM;
            set => DataContext = value;
        }
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedNode = (sender as TreeView).SelectedItem as ReflectorNode; ;
        }
        public ReflectorFactory Factory { get; } = new ReflectorFactory();

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


    }
}
