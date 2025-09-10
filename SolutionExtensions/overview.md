# Solution extensions
Extension to Visual Studio to allow programmers to very simply create own extensions **in solution scope**.
It is highly inspired by VSCommands (by Vlasov Studio), but uses another approach. 
Your extension has real source file with intellisense and allows debugging.
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

### Todo
 * [ ] how to find package argument obj in Launcher from DTE ?
 * [ ] how to add exe to vsix ?
 * [ ] add images to doc
 * [ ] nest file is not unnesting
