### Ideas
* 
* publish from cli
* Add new extension project using wizard
* remove Community.Toolkit and change theme
* extension `Run` method with another type of parameters like Document, ProjectItem, etc.
* allow to use /bin/debug or /bin/release using /**/extensionName.dll
* rebuild extension dll if not found


#### Notes
* Debugger
    * DTE is in global Runtime Objects Table, under some moniker name, this is used to get instance of DTE in debugger process
    * this process is attached to debugger from IDE
    * user should add breakpoint to his code
* Launcher
    * Merged using ILMerge to one exe because referenced dlls are incompatible with vsix
    * merged exe is copied to lib folder and copyied with project
    * some classes from package are linkes, not referencesd
    * how to add exe to vsix?
* Mono.Cecil
    * referenced only one dll, not whole package

### Todos
 * [ ] how to find package argument obj in launcher from DTE?
 * [ ] how to add exe to vsix ?
