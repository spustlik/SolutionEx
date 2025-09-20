using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SolutionExtensions
{
    public class ExtensionsModel : SimpleDataObject
    {
        public ObservableCollection<ExtensionItem> Extensions { get; } = new ObservableCollection<ExtensionItem>();

    }

    public class ExtensionItem : SimpleDataObject
    {
        #region Title property
        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        #endregion

        #region DllPath property
        private string _dllPath;
        public string DllPath
        {
            get => _dllPath;
            set => Set(ref _dllPath, value);
        }
        #endregion

        #region ClassName property
        private string _className;
        public string ClassName
        {
            get => _className;
            set => Set(ref _className, value);
        }
        #endregion

        #region ShortCutKey property
        private string _shortCutKey;
        public string ShortCutKey
        {
            get => _shortCutKey;
            set => Set(ref _shortCutKey, value);
        }
        #endregion

        #region Argument property
        private string _argument;
        public string Argument
        {
            get => _argument;
            set => Set(ref _argument, value);
        }
        #endregion

        #region OutOfProcess property
        private bool _outOfProcess;
        public bool OutOfProcess
        {
            get => _outOfProcess;
            set => Set(ref _outOfProcess, value);
        }
        #endregion

        public IEnumerable<(string name, bool value, Action<bool> setter)> GetFlagInfo()
        {
            yield return (name: nameof(OutOfProcess), OutOfProcess, (v) => OutOfProcess = v); ;
        }

        public IEnumerable<string> GetFlags() => GetFlagInfo().Where(fi => fi.value).Select(fi => fi.name);
    }
}
