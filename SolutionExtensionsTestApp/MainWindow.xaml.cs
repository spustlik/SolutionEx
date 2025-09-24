using SolutionExtensions.UI;
using SolutionExtensions.UI.Themes;
using System.Diagnostics;
using System.IO;
using System.Windows;

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
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (MessageBox.Show(e.Exception.Message + "\nDo you want to exit?", "Unexpected error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) != MessageBoxResult.Yes)
            {
                e.Handled = true;
                return;
            }
            Application.Current.Shutdown();
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
            //MessageBox.Show(s, "Copied to clipboard");
            var fn = Path.Combine(Path.GetTempPath(), "theme.xaml");
            File.WriteAllText(fn, s);
            Process.Start("notepad.exe", fn);
        }
    }
}
