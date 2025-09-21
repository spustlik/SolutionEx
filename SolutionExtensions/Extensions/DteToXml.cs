using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using EnvDTE90a;
using Microsoft.VisualStudio.Shell;
using Model;
using SolutionExtensions.Reflector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using VSLangProj;

namespace SolutionExtensions.Extensions
{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public class DteToXml
    {
        private readonly ReflectionBuilderXml reflectionDumper;
        private readonly Stopwatch watches = new Stopwatch();

        public DteToXml()
        {
            reflectionDumper = new ReflectionBuilderXml();
            reflectionDumper.SkipTypes.AddRange(
                typeof(System.Globalization.CultureInfo),
                typeof(System.Threading.Thread),
                typeof(Task),
                typeof(Microsoft.VisualStudio.Threading.JoinableTaskFactory));
            reflectionDumper.ComReflection.RegisterInterfacesFromAppDomain();
            /*
                        //Microsoft.VisualStudio.RpcContracts
                        reflectionDumper.ComReflection.RegisterInterfaces(typeof(Microsoft.VisualStudio.VisualStudioServices).Assembly);
                        //Microsoft.VisualStudio.Interop
                        reflectionDumper.ComReflection.RegisterInterfaces(typeof(EnvDTE.BuildEventsClass).Assembly);
                        //Microsoft.VisualStudio.Shell.Framework
                        reflectionDumper.ComReflection.RegisterInterfaces(typeof(Microsoft.VisualStudio.Shell.AccountPickerOptions).Assembly);
            */
        }
        public XElement Dump(EnvDTE.DTE dte, AsyncPackage package, bool more)
        {
            reflectionDumper.AddDumper(typeof(Url), (o, parent) =>
            {
                parent.Add(new XAttribute("value", o));
            });
            reflectionDumper.AddDumper<EnvDTE.Property>((p, parent) =>
            {
                parent.Attribute("_type")?.Remove();
                parent.Attribute("_interfaces")?.Remove();
                parent.Add(new XAttribute("Name", p.Name));
                if (p.NumIndices != 0)
                    parent.Add(new XAttribute("NumIndices", p.NumIndices));
                if (p.Object != null)
                    parent.Add(UntypedObject(p.Object));
                var value = p.Value;
                if (value == null)
                    return;
                if (value is string || value.GetType().IsPrimitive)
                    parent.Add(new XAttribute("Value", p.Value));
                else
                    parent.Add(reflectionDumper.DumpUntypedObject(value, "Value"));
            });
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package;
            this.dte = package.GetService<DTE, DTE>() as DTE2;
            this.cmdSvc = (package as IServiceProvider).GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dumpRoot = new XElement("Dump");
            watches.Restart();
            DumpDte(dumpRoot, more);
            reflectionDumper.DumpUsedTypes(dumpRoot);
            return dumpRoot;
        }

        public string Save(XElement root)
        {
            var fn = Path.Combine(Path.GetTempPath(), "dumpDTE.xml");
            root.Save(fn);
            return fn;
        }

        AsyncPackage package;
        DTE2 dte;
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
            //dte.StatusBar
            //dte.SourceControl            
            //dte.CommandBars
            //--dte.Macros
            //dte.Properties            
            //DumpProperties(parent, dte.Properties as object as Properties);
            //dte.ContextAttributes

            DumpSolution(parent);
            AddElapsed(parent);
            DumpDocuments(parent);
            AddElapsed(parent);
            DumpGlobals(parent, dte.Globals);
            AddElapsed(parent);
            DumpWindows(parent, dte);
            AddElapsed(parent);
            if (more)
            {
                DumpCommands(parent);
                AddElapsed(parent);
                DumpMenuCommands(parent);
                AddElapsed(parent);
                DumpDebugger(parent);
                AddElapsed(parent);
                DumpToolWindows(parent);
                AddElapsed(parent);
            }
        }

        private void AddElapsed(XElement parent)
        {
            watches.Stop();
            parent.Add(new XComment($"elapsed {watches.ElapsedMilliseconds}ms"));
            watches.Restart();
        }
        private void DumpToolWindows(XElement parent)
        {
            var e = new XElement("ToolWindows");
            parent.Add(e);
            //not working:var list= dte.ToolWindows as IEnumerable;
            //not interesting interfaces: parent.Add(reflectionDumper.DumpUntypedObject(dte.ToolWindows, "_ReflectedToolWindows"));
            //OK:var w01 = dte.ToolWindows.GetToolWindow("Solution explorer");
            //null:var w1 = dte.ToolWindows.GetToolWindow(ExtensionsListToolWindowPane.CAPTION);
            //err:var w2 = dte.ToolWindows.GetToolWindow(typeof(ExtensionsListToolWindowPane).GUID.ToString());
            //dte.ToolWindows.GetToolWindow(name)
            //dte.ToolWindows.OutputWindow
            //dte.ToolWindows.CommandWindow
            //dte.ToolWindows.ErrorList
            //dte.ToolWindows.TaskList

            //??? dte.ToolWindows.ToolBox.ToolBoxTabs
            DumpSolutionExplorer(e);
        }

        private void DumpToolWindow(XElement parent, object w)
        {
            if (w == null) return;
            parent.Add(reflectionDumper.DumpUntypedObject(w, "_ToolWindow"));
        }

        private void DumpSolutionExplorer(XElement parent)
        {
            var s = dte.ToolWindows?.SolutionExplorer;
            if (s == null)
                return;
            var e = new XElement("SolutionExplorer", Attr(new
            {
                SelectedItemsCount = (s.SelectedItems as object[]).Length,
            }));
            parent.Add(e);
            DumpSolutionExplorerHierarchy(e, s.UIHierarchyItems);
        }

        private void DumpSolutionExplorerHierarchy(XElement parent, UIHierarchyItems items)
        {
            foreach (UIHierarchyItem item in items)
            {
                var e = new XElement("UIHierarchyItem", Attr(new
                {
                    item.Name,
                    item.IsSelected
                }),
                    UntypedObject(item.Object, dte, item.Collection));
                parent.Add(e);
                DumpSolutionExplorerHierarchy(e, item.UIHierarchyItems);
            }
        }

        private XObject UntypedObject(object o, params object[] known)
        {
            var knownObjects = new HashSet<object>(known);
            return reflectionDumper.DumpUntypedObject(o, "Object", knownObjects);
        }

        private void DumpWindows(XElement parent, DTE2 dte)
        {
            var e = new XElement("Windows");
            parent.Add(e);
            foreach (EnvDTE.Window w in dte.Windows)
            {
                var we = new XElement("Window", Attr(new { w.Caption, w.Kind, w.ObjectKind, w.Visible }));
                e.Add(we);
            }
        }

        private void DumpDebugger(XElement parent)
        {
            var dbg = dte.Debugger as Debugger5;
            if (dbg == null)
                return;
            var e = new XElement("Debugger", Attr(new { dbg.CurrentMode }));
            parent.Add(e);
            foreach (Breakpoint3 b in dbg.Breakpoints)
            {
                e.Add(new XElement("Breakpoint", Attr(new
                {
                    b.Name,
                    b.Message,
                    b.Language,

                    b.File,
                    b.FileLine,

                    b.FunctionName,
                    b.FunctionLineOffset,
                    b.FunctionColumnOffset,

                    b.LocationType,
                    b.Type
                })));
            }
            foreach (EnvDTE.Process p in dbg.LocalProcesses)
            {
                var pe = new XElement("Process", Attr(new { p.Name, p.ProcessID }));
                e.Add(pe);
                foreach (Program pr in p.Programs)
                {
                    pe.Add(new XElement("Program", Attr(new { pr.Name, pr.IsBeingDebugged, Threads = pr.Threads.Count })));
                }
            }
            foreach (Transport t in dbg.Transports)
            {
                var te = new XElement("Transport", Attr(new { t.ID, t.Name }));
                e.Add(te);
                foreach (Engine en in t.Engines)
                {
                    te.Add(new XElement("Engine", Attr(new { en.ID, en.Name, en.AttachResult })));
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
                //var d = v as IDispatch;
                //var r = v as IReflect;
                //if (r != null)
                //{
                //    var type = r.UnderlyingSystemType;
                //    var methods = r.GetMethods(BindingFlags.Default);
                //    var properties = r.GetProperties(BindingFlags.Default);
                //}
                //nothing is possible via reflection
                var interfaces = v.GetType().GetInterfaces();

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
                }),
                UntypedObject(proj.Object));
            var vsProject = proj.Object as VSProject;

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
            }),
                UntypedObject(item.Object)
                );
            var vsProjectItem = item.Object as VSProjectItem;
            if (vsProjectItem != null)
            {
                var dte1 = vsProjectItem.ContainingProject.DTE;
                var dte2 = (vsProjectItem as dynamic).ContainingProject.DTE;
                var dte1_t = ReflectionCOM.QueryInterface<DTE>(dte1);
                var dte2_t = ReflectionCOM.QueryInterface<DTE>(dte2);
                var v = dte1.Version;
            }
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
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
}
