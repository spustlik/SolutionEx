using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;

namespace SolutionExtensions
{
    //on ancestor, add [guid] attribute
    //on your package add [ProvideToolWindow(typeof(yourClass))]
    public abstract class ToolWindowPaneBase : ToolWindowPane
    {
        protected ToolWindowPaneBase(string caption, UserControl content) : base(null)
        {
            this.Caption = caption;
            this.Content = content;
        }
    }
}
