using SolutionExtensions.Model;

namespace SolutionExtensions.UI.Extensions
{
    public interface IExtensionsService
    {
        void UpdateItemFromDll(ExtensionItem item);
        void Save(ExtensionsModel model);
        void Run(ExtensionItem item, bool debug);
        bool ShowBrowseDll(ExtensionItem item, bool force);
        string[] FindExtensionClasses(ExtensionItem item);
        string GetValidation(ExtensionItem item);
        void OpenContextMenu(ExtensionItem item, object menuTag);
    }
}
