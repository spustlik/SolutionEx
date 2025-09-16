# Solution extensions
Extension to Visual Studio to allow programmers to very simply create own extensions **in solution scope**.
It is highly inspired by VSCommands (by Vlasov Studio), but uses another approach. 
Your extension has real source file with intellisense and allows somehow debugging.
Config of extensions (path, title, shortcut) is stored in file in solution scope, so shared in team.

## Writing extension
Your extension is just compiled assembly and should be in your solution. 
* create new class library
* add nuget package `Microsoft.VisualStudio.Interop` to reference DTE interfaces
* create class with `Run` method (can be static)
```c#
    public class MyExtension
    {
        public void Run(DTE dte, object package)
        {
            //add your code here 
        }
    }
```
* compile it
* Use menu `Extensions` -> `Solution Extensions` -> `Show`
* add your new extension, pick dll, choose class name, change title, add shortcut if you want
* execute your extension using `Run` button or using `Debug`
* commit and share with others in team
* WARNING: `package` argument is null when debugging, `AsyncPackage` : `Package`: `IServiceProvider` else
* You can use DTE Inspector to inspect DTE objects

## DTE Inspector
* visualizes DTE object as tree, expands using buttons on demand
* uses reflection and special COM objects reflection
* `[{i}]` - implementing interfaces, including COM interfaces (_see note below_)
* `[m()]` - method of interface or reflected type
* `[.p]` - properties of interface or properties with value of reflected object
* `[e]` - enumerates items, if 0, nothing happens
* you can generate XML with expanded nodes, csharp source, or copy text to clipboard
* some functions are in context menu
_note:_ COM interface is late-bound and uses another approach, so interface GUID must be known to get it. Inspector is using ALL types loaded in current AppDomain (it is OK, Visual Studio has loaded all used interfaces) and asks COM object if it is implementing each of them.


### Internal extensions
* there are some internal extensions, which you can use directly or only for inspiration
* set dll path to $(SELF) and then pickup class name.
* `DumpExtension`,`DumpExtensionMore` - dumps common props and collection to xml file

### Version history
* 1.0 - initial version

### Ideas
* publish from cli
    * https://learn.microsoft.com/en-us/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension?view=vs-2022
    * https://learn.microsoft.com/en-us/visualstudio/extensibility/walkthrough-publishing-a-visual-studio-extension-via-command-line?view=vs-2022
* Add new extension project using wizard
* remove Community.Toolkit and solve theming by another way
* extension `Run` method with another type of parameters like Document, ProjectItem, etc.
* allow to use /bin/debug or /bin/release using /**/extensionName.dll
* rebuild extension dll if not found


#### Notes
* Debugging
    * DTE is in global Runtime Objects Table, under some moniker name, it is used to get instance of DTE in launcher process
    * this process is attached to debugger from IDE
    * user should add breakpoint to his code, or call `Debugger.Break()`
* Launcher
    * execute extension in separate process - for debugging (or to allow reload assembly in future)
    * Merged using ILMerge to one exe because referenced dlls are incompatible with vsix
    * merged exe is copied to lib folder and copyied with project
    * some classes from package are linked, not referenced
    * how to add exe to vsix?
* extension assembly
    * cannot be reloaded, so it is using copy dll to new path, change of assembly name (using Mono.Cecil)
    * it will not work if it is using some other re-compiled assembly
    * but can work only for methods reflection
* Mono.Cecil
    * referenced only one dll, not whole package
* Nest file extension
    * Cannot find how to un-nest item, there are missing methods for that
```c#
    var project = dte.Solution.Projects[0] as EnvDTE.Project;
    var item = project.ProjectItems[0] as EnvDTE.ProjectItem;
    var srcFile = item.Name;
    // not interesting : .Object as VSLangProj.VSProjectItem)
    var nestedItem = item.ProjectItems[0] as EnvDTE.ProjectItem;
    var file2 = nestedItem.Name;
    //how to remove it?
    //no Remove() on ProjectItems
```
 

### Todo
 * [ ] VS colors on treeview expader icon
 * [ ] how to add exe to vsix ?
 * [ ] add images to doc
 * [ ] how to find package argument obj in Launcher from DTE ?
    * probably not posible, without registering assembly to allow marshalling (usage from another process)
    * for same reson IServiceProvider cannot be used (marshalling)
 * [ ] custom variables in cfg like $(MyTemplates)=$(SolutionDir)/MyTemplates
 * [ ] nest file is not unnesting
 * [ ] some support of events 
    - in extension, some event handlers will be added
    - extension must not be destroyed
    - but can be destroyed when runs again or in new version
    - optionaly exec something like Destroy() method
    - add autoRun to extension options?
    - that extensions should be destroyed on solution unload
 