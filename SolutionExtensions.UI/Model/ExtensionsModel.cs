using System.Collections.ObjectModel;

namespace SolutionExtensions.Model
{
    public class ExtensionsModel : SimpleDataObject
    {
        public ObservableCollection<ExtensionItem> Extensions { get; } = new ObservableCollection<ExtensionItem>();

    }
}
