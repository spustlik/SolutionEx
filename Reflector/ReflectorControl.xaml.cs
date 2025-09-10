using SolutionExtensions.Reflector;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Reflector
{
    /// <summary>
    /// Interaction logic for ReflectorControl.xaml
    /// </summary>
    public partial class ReflectorControl : UserControl
    {
        public ReflectorControl()
        {
            InitializeComponent();
            ViewModel = new ReflectorRoot();
            Factory.COM.RegisterInterfacesFromAppDomain();
        }

        public ReflectorRoot ViewModel
        {
            get => DataContext as ReflectorRoot;
            set => DataContext = value;
        }
        public ReflectorFactory Factory { get; } = new ReflectorFactory();

        private void CallExpand<T>(object sender, Action<T> action) where T : class
        {
            var node = (sender as FrameworkElement).DataContext as T;
            if (node == null)
                return;
            action(node);
            var item = treeView.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
            if (item != null)
                item.IsExpanded = true;
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
            CallExpand<ReflectorValueNode>(sender, node => Factory.ExpandMethods(node));
        }

        private void ExpandEnumerable_Click(object sender, RoutedEventArgs e)
        {
            CallExpand<ReflectorValueNode>(sender, node => Factory.ExpandInterfaces(node));
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            var node = (sender as FrameworkElement).DataContext as ReflectorNode;
            if (node == null) return;
            var s = Factory.BuildNodeSource(node);
            if (s == null)
                return;
            Clipboard.SetText(s);
            MessageBox.Show($"Generated source copied to clipboard.");
        }
    }
}
