using System;
using System.Runtime.InteropServices;

namespace SolutionExtensions.ToolWindows
{
    [Guid("D4B5F1E3-8F2A-4C6A-9D3E-2B1C6F7E8A9B")]
    public class ExtensionsListToolWindowPane : ToolWindowPaneBase<ExtensionsListToolWindow>
    {
        public static string CAPTION = "Solution extensions";

        public ExtensionsListToolWindowPane() : base(CAPTION, new ExtensionsListToolWindow())
        {
        }
    }
}