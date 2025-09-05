### Ideas
* 
* publish from cli
* Debugger using exe launcher, ROT 
* switch launcher as win app
* Add new extension project using wizard
* remove Community.Toolkit and change theme
* extension `Run` method with another type of parameters like Document, ProjectItem, etc.
* allow to use /bin/debug or /bin/release using /**/extensionName.dll
* rebuild extension dll if not found
* add [run] on list item*

#### Notes
* Debugger
    * DTE is in global Runtime Objects Table, under some moniker name, this is used to get instance of DTE in debugger process
    * this process is attached to debugger from IDE
    * TODO: somehow navigate debugger to first line of extension
    * TODO: how to find package argument obj?
* Launcher
    * Merged using ILMerge to one exe because referenced dlls are incompatible with vsix
    * merged exe is copied to lib folder and copyied with project
    * how to add exe to vsix?
* Mono.Cecil
    * referenced only one dll, not whole package
