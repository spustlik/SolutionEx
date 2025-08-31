using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace SolutionExtensions.ToolWindows
{
    [Guid("D4B5F1E3-8F2A-4C6A-9D3E-2B1C6F7E8A9B")]
    public class ExtensionsListToolWindowPane : ToolWindowPaneBase<ExtensionsListToolWindow>
    {
        public ExtensionsListToolWindowPane() : base("Extensions List", new ExtensionsListToolWindow())
        {
        }
    }

    public partial class ExtensionsListToolWindow : UserControl
    {
        public ExtensionsListToolWindow()
        {
            InitializeComponent();
        }
        ToolWindowPane ToolWindowPane => this.Tag as ToolWindowPane;
    }
}
