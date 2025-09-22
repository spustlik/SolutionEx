using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Markup.Primitives;

namespace SolutionExtensionsTestApp
{
    /// <summary>
    /// Interaction logic for ReflToolWindow.xaml
    /// </summary>
    public partial class ReflToolWindow : UserControl
    {
        public ReflToolWindow()
        {
            InitializeComponent();
        }

        private void DumpMaionWindow_Click(object sender, RoutedEventArgs e)
        {
            DumpObj("Main window", Application.Current.MainWindow);
        }

        private void DumpObj(string caption, object obj)
        {
            reflectorControl.ViewModel.Children.Clear();
            reflectorControl.ViewModel.Children.Add(reflectorControl.Factory.CreateRoot(caption, obj));
        }

        private void DumpStyle_Click(object sender, RoutedEventArgs e)
        {
            var style = FindResource("myStyle");
            DumpObj("Style", style);
            //MarkupWriter.GetMarkupObjectFor(style);
            //var wr = new System.Xaml.XamlWriter();
            //var xaml = Markup.XamlWriter.Save(style);
            //Clipboard.SetText(xaml);
        }
    }
}
