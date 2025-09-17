# Solution extensions
Extension to Visual Studio to allow programmers to very simply create own extensions **in solution scope**.
It is highly inspired by VSCommands (by Vlasov Studio), but uses another approach. 
Your extension has real source file with intellisense and allows somehow debugging.
Config of extensions (path, title, shortcut) is stored in file in solution scope, so shared in team.

You can find new menu in `Extensions` -> `Solution Extensions` -> 
![Menu](images/menu.png "Menu")

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
* Use `Show...` menu item, it will show new tool menu with list of extensions
![List of extensions](images/extlist.png "List of extensions")
* config of extensions is saved next to solution file (.extensions.cfg) and can be added to solution/source control, so it is loaded on other machines and allows to run extensions for others
* add your new extension, pick dll, choose class name, change title, add shortcut if you want
* execute your extension using `Run` button, or assigned key shortcut, or using `Debug`
### More
* add `[Description("caption")]` attribute to Run method or class to allow inspection of caption
* commit and share with others in team, it is possible to share compiled assembly also
* WARNING: `package` argument is null when debugging, `AsyncPackage` : `Package`: `IServiceProvider` else
* You can use __DTE Inspector__ to inspect DTE objects

## DTE Inspector
![](images/dtereflection.png)
* visualizes DTE objects as tree, you can expand nodes using buttons on demand
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

### Todo
 * [ ] custom variables in cfg like $(MyTemplates)=$(SolutionDir)/MyTemplates
 * [ ] nest file is not unnesting
 * [ ] some support of events 
    - in extension, some event handlers will be added
    - extension must not be destroyed
    - but can be destroyed when runs again or in new version
    - optionaly exec something like Destroy() method
    - add autoRun to extension options?
    - that extensions should be destroyed on solution unload
 