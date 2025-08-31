using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;

namespace SolutionExtensions
{
    //on ancestor, add [guid] attribute
    //on your package add [ProvideToolWindow(typeof(yourClass))]
    public abstract class ToolWindowPaneBase<TC> : ToolWindowPane where TC : UserControl
    {
        protected ToolWindowPaneBase(string caption, TC content) : base(null)
        {
            this.Caption = caption;
            this.Content = content;
            content.Tag = this;
        }
        public new TC Content { get => base.Content as TC; set => base.Content = value; }
    }


}
