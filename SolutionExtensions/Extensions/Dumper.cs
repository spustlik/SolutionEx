using EnvDTE;
using EnvDTE100;
using Microsoft.VisualStudio.Shell;
using SolutionExtensions.Runner;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SolutionExtensions.Extensions
{

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public class Dumper
    {
        public XElement Dump(EnvDTE.DTE dte, AsyncPackage package, bool more)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package;
            this.dte = dte;
            this.cmdSvc = (package as IServiceProvider).GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dumpRoot = new XElement("Dump");
            DumpDte(dumpRoot, more);
            return dumpRoot;
        }

        public string Save(XElement root)
        {
            var fn = Path.Combine(Path.GetTempPath(), "dumpDTE.xml");
            root.Save(fn);
            return fn;
        }

        AsyncPackage package;
        DTE dte;
        OleMenuCommandService cmdSvc;
        private void DumpDte(XElement parent, bool more)
        {
            var p = System.Diagnostics.Process.GetCurrentProcess();
            parent.Add(Attr(new
            {
                dte.Name,
                dte.LocaleID,
                dte.Mode,
                dte.DisplayMode,
                dte.Edition,
                dte.FileName,
                dte.Version,
                dte.RegistryRoot,
            }));
            parent.Add(new XElement("Process", Attr(new
            {
                p.Id,
                p.ProcessName,
                p.MainModule.ModuleName,
                p.MainModule.FileName,

            }), new XElement("FileInfo", Attr(p.MainModule.FileVersionInfo))));
            //dte.Windows
            //dte.StatusBar
            //dte.SourceControl            
            //dte.CommandBars
            //dte.Macros
            //dte.Properties
            
            //DumpProperties(parent, dte.Properties as object as Properties);
            DumpSolution(parent);
            DumpDocuments(parent);
            DumpGlobals(parent, dte.Globals);
            if (more)
            {
                DumpCommands(parent);
                DumpMenuCommands(parent);
                DumpDebugger(parent);
            }
        }

        private void DumpDebugger(XElement parent)
        {
            var dbg = dte.Debugger as Debugger5;
            if (dbg == null)
                return;
            var e = new XElement("Debugger", Attr(new { dbg.CurrentMode }));
            parent.Add(e);
            foreach (Process p in dbg.LocalProcesses)
            {
                var pe = new XElement("Process", Attr(new { p.Name, p.ProcessID }));
                e.Add(pe);
                foreach (Program pr in p.Programs)
                {
                    var pre = new XElement("Program", Attr(new { pr.Name, pr.IsBeingDebugged, Threads = pr.Threads.Count }));
                    pe.Add(pre);
                }
            }
        }

        private void DumpDocuments(XElement parent)
        {
            var e = new XElement("Documents", Attr(new { count = dte.Documents.Count }));
            parent.Add(e);
            foreach (Document doc in dte.Documents)
            {
                DumpDocument(e, doc);
            }
        }

        private void DumpDocument(XElement parent, Document doc)
        {
            var e = new XElement("Document", Attr(new { doc.Name, doc.Kind, doc.Language, doc.Type, doc.Path, doc.FullName }));
            parent.Add(e);
        }

        private void DumpSolution(XElement parent)
        {
            if (dte.Solution == null)
                return;
            var sol = dte.Solution;
            var e = new XElement("Solution",
                Attr(new
                {
                    sol.FullName,
                    sol.IsOpen,
                    //templatePath = (sol as dynamic).TemplatePath
                }));
            parent.Add(e);
            DumpExtenders(e, sol);
            DumpGlobals(e, sol.Globals);
            DumpProperties(e, sol.Properties);
            DumpProjects(e, sol);
            DumpSolutionBuild(e, sol);
        }

        private void DumpSolutionBuild(XElement parent, Solution sol)
        {
            var b = sol.SolutionBuild;
            if (b == null)
                return;
            var e = new XElement("SolutionBuild", Attr(new { b.BuildState }));
            if (b.BuildState != vsBuildState.vsBuildStateNotStarted)
            {
                e.Add(Attr(new { b.LastBuildInfo }));
            }

            parent.Add(e);
        }

        private object[] Attr(object o)
        {
            var result = new List<XAttribute>();
            var props = o.GetType().GetProperties();
            foreach (var prop in props)
            {
                var v = prop.GetValue(o);
                if (v != null && !String.Empty.Equals(v))
                {
                    v = GetValue(v);
                    result.Add(new XAttribute(prop.Name, v));
                }
            }
            return result.ToArray();
        }

        private static object GetValue(object v)
        {
            //        <Property name="Publish" value="System.__ComObject" />

            if (v == null) return null;
            if (v.GetType().IsCOMObject)
            {
                //nothing is possible via reflection
            }
            if (v.GetType() == typeof(object[]))
            {
                //what to do?
            }
            if (v is string[] sa)
            {
                v = string.Join(",", sa);
            }
            return v;
        }

        private void DumpExtenders(XElement e, /*Solution*/
                dynamic parent)
        {
            if (!String.IsNullOrEmpty(parent.ExtenderCATID))
                e.Add(new XAttribute("extenderCATID", parent.ExtenderCATID));
            if (parent.ExtenderNames != null)
            {
                var names = parent.ExtenderNames as string[];
                foreach (var n in names)
                {
                    var ee = new XElement("Extender", new XAttribute("name", n));
                    e.Add(ee);
                    var ex = parent.Extender[n];
                    ee.Add(new XAttribute("value", ex));
                }
            }
        }

        private void DumpProjects(XElement parent, Solution sol)
        {
            var e = new XElement("Projects");
            parent.Add(e);
            //dte.Documents.Add()
            foreach (Project proj in sol)
            {
                DumpProject(e, proj);
            }

        }

        private void DumpProject(XElement parent, Project proj)
        {
            var e = new XElement("Project",
                Attr(new
                {
                    proj.Name,
                    proj.FileName,
                    proj.FullName,
                    proj.Kind,
                    proj.UniqueName,
                    proj.CodeModel,
                    objectType = proj.Object
                }));
            parent.Add(e);
            //DumpExtenders(e, proj);
            DumpGlobals(e, proj.Globals); //possible same as sol?
            DumpProperties(e, proj.Properties);
            foreach (ProjectItem item in proj.ProjectItems)
            {
                DumpProjectItem(e, item);
            }
        }

        private void DumpProjectItem(XElement parent, ProjectItem item)
        {
            var files = String.Join(";", item.GetFiles());
            var e = new XElement("ProjectItem", Attr(new
            {
                item.Name,
                item.Kind,
                item.FileCodeModel,
                Files = files,
                //item.IsDirty,
                //item.Saved,
                objectType = item.Object
            }));
            parent.Add(e);
            //DumpExtenders(e, item);
            DumpProperties(e, item.Properties);
            if (item.SubProject != null)
            {
                DumpProject(e, item.SubProject);
            }
        }

        private void DumpGlobals(XElement e, Globals globals)
        {
            if (globals == null)
                return;
            var variables = (globals.VariableNames as object[]).Cast<string>().ToArray();
            if (variables.Length == 0)
                return;
            var ge = new XElement("Globals");
            e.Add(ge);
            foreach (var variable in variables)
            {
                var ve = new XElement("Variable", new XAttribute("name", variable));
                ge.Add(ve);
                try
                {
                    var v = globals[variable];
                    ve.Add(new XAttribute("value", v));
                }
                catch (Exception ex)
                {
                    ve.Add(new XAttribute("error", ex.Message));
                }
            }
        }

        private void DumpMenuCommands(XElement parent)
        {
            var e = new XElement("Menu", new XAttribute("count", cmdSvc.Verbs.Count));
            parent.Add(e);
            foreach (MenuCommand verb in cmdSvc.Verbs)
            {
                DumpMenuCommand(e, verb);
            }
            //? problem of menuteims is that service is mnu from package, but cmd is from dte
            foreach (Command c in dte.Commands)
            {
                var mnu = cmdSvc.FindCommand(c.GetCommandID());
                if (mnu != null)
                {
                    DumpMenuCommand(e, mnu);
                }
            }
        }

        private void DumpMenuCommand(XElement parent, MenuCommand cmd)
        {
            var e = new XElement("Menu",
                       Attr(new
                       {
                           cmd.CommandID,
                           cmd.Visible
                       }));
            if (cmd is DesignerVerb verb) //:MenuCommand
            {
                e.Add(Attr(new { type = nameof(DesignerVerb), verb.Text, verb.Description }));
            }
            if (cmd is OleMenuCommand mnu) //:MenuCommand
            {
                e.Add(Attr(new { type = nameof(OleMenuCommand), mnu.Text, mnu.ParametersDescription, mnu.AutomationName }));
            }
            DumpProperties(e, cmd.Properties);
            parent.Add(e);
        }

        private void DumpProperties(XElement parent, Properties properties)
        {
            if (properties == null || properties.Count == 0)
                return;
            DumpProperties<Property>(parent, properties, p => p.Name, p => p.Value);
        }
        private void DumpProperties(XElement parent, IDictionary properties)
        {
            if (properties.Count == 0)
                return;
            DumpProperties<string>(parent, properties.Keys, k => k, k => properties[k]);
        }
        private void DumpProperties<T>(
            XElement parent, 
            IEnumerable list, 
            Func<T, string> nameGeter, 
            Func<T, object> valueGetter)
            where T : class
        {
            foreach (var item in list)
            {
                T prop = item as T;
                var pe = new XElement("Property", new XAttribute("name", nameGeter(prop)));
                parent.Add(pe);
                try
                {
                    var value = valueGetter(prop);
                    if (value != null)
                    {
                        value = GetValue(value);
                        pe.Add(new XAttribute("value", value));
                    }
                }
                catch (Exception ex)
                {
                    //error="Došlo k výjimce. (Exception from HRESULT: 0x80020009 (DISP_E_EXCEPTION))" />
                    //The method or operation is not implemented
                    pe.Add(new XAttribute("_error", ex.Message));
                }

            }
        }


        private void DumpCommands(XElement parent)
        {
            var e = new XElement("Commands", Attr(new { dte.Commands.Count }));
            parent.Add(e);
            foreach (var g in dte.Commands.Cast<Command>().GroupBy(c => c.Guid))
            {
                var ge = new XElement("GroupSet", new XAttribute("guid", g.Key));
                e.Add(ge);
                foreach (var cmd in g)
                {
                    var ce = new XElement("Command", new XAttribute("id", "0x" + cmd.ID.ToString("X")));
                    ge.Add(ce);
                    if (!string.IsNullOrEmpty(cmd.Name))
                        ce.Add(new XAttribute("name", cmd.Name));
                    var b = cmd.Bindings as object[]; //array of strings in format like "VC Dialog Editor::F9"
                    if (b.Length > 0)
                        ce.Add(new XAttribute("bindings", string.Join("|", b)));
                }
            }
        }
    }
}
