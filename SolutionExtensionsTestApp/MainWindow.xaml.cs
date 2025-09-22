using SolutionExtensions.UI;
using SolutionExtensions.UI.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace SolutionExtensionsTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            grid.DoZoomerClick();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //{ "Zadání hodnoty v System.Windows.Markup.StaticResourceHolder vyvolalo výjimku. Číslo řádku 16 a pozice na řádku 31."}
            var key = ThemeKeys.TreeViewItem_TreeArrow_Static_Fill;
            var r = TryFindResource(ThemeKeys.TreeViewItem_TreeArrow_Static_Fill);
            //ThemeKeys.ReplaceWithOriginals(x =>
            //{
            //    return Guid.NewGuid().ToString();
            //});
        }

        private void DumpTheme_Click(object sender, RoutedEventArgs e)
        {
            var s = ThemeKeys.DumpCurrentValues(this);
            Clipboard.SetText(s);
            MessageBox.Show(s, "Copied to clipboard");
        }
    }
}
