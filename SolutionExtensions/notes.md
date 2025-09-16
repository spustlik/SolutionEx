### Nest file extension
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
 

 ###  package argument 
    * is out-of-process 
    * is it somehow possible to find package argument obj in Launcher from DTE ?
    * probably not posible, without registering assembly to allow marshalling (usage from another process)
    * for same reson IServiceProvider cannot be used (marshalling)
