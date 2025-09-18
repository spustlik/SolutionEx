using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExtensionSamples.Extra
{
    public class ExperimentEvents
    {
        public void Run(DTE dte)
        {
            //dte.Events.SelectionEvents.OnChange += SelectionEvents_OnChange;
            dte.Events.TextEditorEvents.LineChanged += TextEditorEvents_LineChanged;
            //dte.Events.DocumentEvents.DocumentOpened
            AddToOutputPane(dte, "Started and listening event");
        }

        private void TextEditorEvents_LineChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            var textDoc = startPoint.Parent;
            var doc = textDoc.Parent;
            if (doc.ActiveWindow.Kind == EnvDTE.Constants.vsWindowKindOutput)
                return;
            var edit = startPoint.CreateEditPoint();
            var txt = edit.GetText(endPoint.CreateEditPoint());
            //logging to outputpane is slow, and maybe recursive
            var log = Path.Combine(Path.GetTempPath(), "_exlog.log");
            File.AppendAllText(log, $"LineChanged at {doc.Name} {startPoint.Line}:{startPoint.LineCharOffset} - {endPoint.Line}:{endPoint.LineCharOffset} ('{txt}') received...");
            if (txt == "STOP")
            {
                textDoc.DTE.Events.TextEditorEvents.LineChanged -= TextEditorEvents_LineChanged;
                AddToOutputPane(textDoc.DTE, $"Removed listener");
                return;
            }
        }
        private void AddToOutputPane(DTE dte, string message)
        {
            var dte2 = dte as DTE2;
            var pane = dte2.ToolWindows.OutputWindow.ActivePane;
            pane.Activate();
            pane.OutputString(message + "\n");
        }
        private void SelectionEvents_OnChange()
        {
            
        }
    }
}
