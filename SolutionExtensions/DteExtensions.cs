using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public static class DteExtensions
    {
        private const string SOLUTION_ITEMS_FOLDER_NAME = "Solution Items";
        private const string MISC_FILES_NAME = "Miscellaneous Files";

        public static Project FindSolutionFolder(this Solution solution, bool addIfNotExists = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project proj in solution.Projects)
            {
                if (proj.UniqueName == EnvDTE.Constants.vsSolutionItemsProjectUniqueName)
                    return proj;
                if (proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    return proj;
            }
            if (addIfNotExists)
                return AddSolutionFolder(solution, SOLUTION_ITEMS_FOLDER_NAME);
            return null;
        }
        public static Project AddSolutionFolder(this Solution solution, string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var s2 = solution as Solution2;
            var folder = s2.AddSolutionFolder(SOLUTION_ITEMS_FOLDER_NAME);
            //folder.Kind= EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder
            return folder;
        }

        public static ProjectItem FindProjectItem(this Project project, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (string.Equals(item.Name, Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }
        public static Project FindProjectMiscItems(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project item in solution.Projects)
            {
                if (item.Kind == EnvDTE.Constants.vsProjectKindMisc || item.UniqueName == EnvDTE.Constants.vsMiscFilesProjectUniqueName)
                    return item;
            }
            return null;
        }

        public static Project AddProjectMiscItems(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var sol2 = solution as Solution2;
            var template = solution.ProjectItemsTemplatePath(EnvDTE.Constants.vsProjectKindMisc);
            var proj = solution.AddFromTemplate(template, null, MISC_FILES_NAME);
            return proj;
        }
        /*
                if (File.Exists(filePath))
                    return project.ProjectItems.AddFromFile(filePath);
                else
                    return project.ProjectItems.AddFromFileCopy(filePath);
        */

        public static OutputWindowPane GetOutputWindowPane(this DTE dte, string name, bool createIfNotExists = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.Window win = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            OutputWindow ow = win.Object as OutputWindow;
            var pane = FindOutputPaneByName(ow, name);
            if (createIfNotExists && pane == null)
                pane = ow.OutputWindowPanes.Add(name);
            return pane;
        }

        public static OutputWindowPane FindOutputPaneByName(this OutputWindow ow, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (OutputWindowPane outputPane in ow.OutputWindowPanes)
            {
                if (outputPane.Name == name)
                    return outputPane;
            }
            return null;
        }

        public static void AddToOutputPane(this DTE dte, string msg, string name, bool clear = false, bool activate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outputPane = dte.GetOutputWindowPane(name, true);
            if (clear)
                outputPane.Clear();
            if (activate)
                outputPane.Activate();
            if (!msg.EndsWith("\n"))
                msg += "\n";
            outputPane.OutputString(msg);
        }

        public static Task ShowStatusBarErrorAsync(this AsyncPackage package, string message)
        {
            return package.ShowStatusBarAsync(message, isError: true);
        }
        public static async Task ShowStatusBarAsync(
            this AsyncPackage package,
            string message,
            bool isError = false,
            bool isImportant = false,
            int waitMs = 0)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            IVsStatusbar statusBar = await package.GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            if (statusBar == null) return;
            var flash = isError || isImportant;
            if (flash && waitMs == 0)
                waitMs = 3000;
            //nor coloring nor highlight is working in VS
            uint? fcolor = null;
            uint? bcolor = null;
            if (isError)
            {
                //fcolor = (uint?)VSColorTheme.GetThemedColor(CommonDocumentColors.StatusBannerErrorTextColorKey).ToArgb();
                //bcolor = (uint?)VSColorTheme.GetThemedColor(CommonDocumentColors.StatusBannerErrorColorKey).ToArgb();                
            }
            if (isImportant)
            {
                //fcolor = 0xFF8080FF;
                //bcolor = 0xFF0000;
            }
            if (fcolor == null && bcolor == null)
                statusBar.SetText(message);
            else
                statusBar.SetColorText(message, fcolor.GetValueOrDefault(), bcolor.GetValueOrDefault());

            if (waitMs > 0)
            {
                statusBar.FreezeOutput(0); // Unfreeze if previously frozen
                statusBar.FreezeOutput(1); // Freeze to highlight the message
                await Task.Delay(waitMs);  // Keep it frozen for time
                statusBar.FreezeOutput(0); // Unfreeze again
            }
        }
        public static async Task SwitchToUiThreadAsync(this AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        }

        public static CommandID GetCommandID(this Command cmd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return new CommandID(new Guid(cmd.Guid), cmd.ID);
        }
        public static async Task<OleMenuCommandService> GetMenuCommandServiceAsync(this AsyncPackage package)
        {
            var svc = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (svc == null) throw new Exception($"Cannot create IMenuCommandService");
            return svc;
        }

        public static void AddShortCutToCommand(this Command command, string shortCut, bool clear = true)
        {
            if (String.IsNullOrEmpty(shortCut))
                return;
            ThreadHelper.ThrowIfNotOnUIThread();
            //var command = GetCommandByName(dte2, "SolutionExtensions.MyCommand");
            // Assign shortcut if not already present
            var bindings = ((object[])command.Bindings).Cast<string>().ToList();
            if (clear)
                bindings.Clear();
            //what about "Text Editor::Ctrl+E, Ctrl+E" ?
            if (!shortCut.Contains("::"))
                shortCut = "Global::" + shortCut;
            if (!bindings.Contains(shortCut))
            {
                bindings.Add(shortCut);
                try
                {
                    command.Bindings = bindings.Cast<object>().ToArray();
                }
                catch (Exception ex)
                {
                    //silent error, because of bad format etc...
                }
            }
        }

        public static Command GetCommandByName(this DTE dte, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var commands = dte.Commands;
            var command = commands.Item(name, 0);
            return command;
        }

        public static T CreateToolWindow<T>(this Package package) where T : ToolWindowPane
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ToolWindowPane pane = package.FindToolWindow(typeof(T), 0, true);
            if (pane == null || pane.Frame == null)
                throw new NotSupportedException($"Cannot create tool window {typeof(T).Name}");

            IVsWindowFrame windowFrame = (IVsWindowFrame)pane.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            return pane as T;
        }

        public static IEnumerable<string> GetFiles(this ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            for (var i = 1; i <= item.FileCount; i++)
            {
                yield return item.FileNames[(short)i];
            }
        }


        static void addFile(DTE dte, string fn)
        {
            //not working, nees template or file to create new project item
            ThreadHelper.ThrowIfNotOnUIThread();
            //ok:var template1 = dte.Solution.ProjectItemsTemplatePath("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            //err:var template = dte.Solution.ProjectItemsTemplatePath("{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}");// EnvDTE.Constants.vsProjectKindMisc);
            try
            {
                var proj = dte.Solution.FindProjectMiscItems() ?? dte.Solution.AddProjectMiscItems();
                var pi = proj.FindProjectItem(fn);
                if (pi != null)
                    pi = proj.ProjectItems.AddFromFile(fn);
                dte.Documents.Open(fn);
                //var doc = dte.Documents.Add(null);// arg invalid
                //var doc = dte.Documents.Add(EnvDTE.Constants.vsDocumentKindText);//arg invalid
                //var doc = dte.Documents.Add(EnvDTE.Constants.vsDocumentKindText.Trim('{', '}'));//arg invalid
                var doc = dte.Documents.Add(fn);
                doc.Activate();
                //(doc.Selection as EnvDTE.TextSelection).Insert(dumpRoot.ToString());
            }
            catch (Exception ex)
            {
                dte.AddToOutputPane($"Error:" + ex, typeof(SolutionExtensionsPackage).Namespace);
            }

            //dte.Solution.AddFromFile(fn);//needs path to template

        }
    }
}
