using System;
using System.Runtime.InteropServices;

namespace SolutionExtensions.ToolWindows
{
    [Guid("2016A00B-1AF6-4C0A-9F58-77E201374A87")]
    public class ReflectorToolWindowPane : ToolWindowPaneBase<ReflectorToolWindow>
    {
        public static string CAPTION = "DTE reflection";

        public ReflectorToolWindowPane() : base(CAPTION, new ReflectorToolWindow())
        {
        }
    }
}
