namespace SolutionExtensions._DesignData
{
    public class ExtensionListVM : SolutionExtensions.ToolWindows.VM
    {
        public ExtensionListVM()
        {
            ValidationMessage = "Some validation";
            Model = new ExtensionsModel();
            Model.Extensions.Add(new ExtensionItem()
            {
                ClassName = "Class1",
                Title = "Extension 1",
                ShortCutKey = "CTRL+X",
                DllPath = "$(SolutuionDir)\\MyExtension\\bin\\debug\\MyExtension.dll",
            });
            Model.Extensions.Add(new ExtensionItem()
            {
                ClassName = "Class2",
                Title = "Extension 2",
                DllPath = "$(SELF)",
            });
            SelectedItem = Model.Extensions[1];
        }
    }
}
