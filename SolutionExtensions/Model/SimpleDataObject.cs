using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Markup.Localizer;
using System.Xml.Serialization;

namespace SolutionExtensions
{
    public abstract class SimpleDataObject : INotifyPropertyChanged
    {
        protected virtual void DoPropertyChanged([CallerMemberName]string propertyName = null)
        {
            IsChanged = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void CallPropertyChanged(string propertyName)
        {
            DoPropertyChanged(propertyName);
        }

        public void ClearChanged()
        {
            IsChanged = false;
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            //T origValue = field;
            field = value;
            DoPropertyChanged(propertyName);
            return true;
        }

        [XmlIgnore]
        public bool IsChanged { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class SimpleValidableDataObject : SimpleDataObject, IDataErrorInfo, INotifyDataErrorInfo
    {
        #region IDataErrorInfo implementation
        string IDataErrorInfo.Error
        {
            //WPF is not using this
            get { throw new NotImplementedException(); }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get { return GetError(columnName); }
        }

        #endregion

        private Dictionary<string, string> _errors = new Dictionary<string, string>();

        /// <summary>
        /// adds error to collection of errors, but doesn't fire errorinfo event
        /// </summary>
        protected void ReportError(string propertyName, string error)
        {
            _errors[propertyName] = error;
        }

        protected void AddError(string propertyName, string error)
        {
            bool savedHasErrors = HasErrors;
            ReportError(propertyName, error);
            RaiseErrorsChanged(new DataErrorsChangedEventArgs(propertyName));
            if (savedHasErrors != HasErrors)
            {
                DoPropertyChanged(nameof(HasErrors));
                DoPropertyChanged(nameof(IsValid));
            }
        }

        protected void ClearError(string propertyName)
        {
            AddError(propertyName, null);
        }

        protected void ClearErrors()
        {
            foreach (var err in ErrorList)
            {
                ClearError(err.Key);
            }
        }

        protected IEnumerable<KeyValuePair<string, string>> ErrorList { get { return _errors.ToArray(); } }

        protected virtual string GetError(string columnName)
        {
            string error;
            _errors.TryGetValue(columnName, out error);
            return error;
        }

        #region INotifyDataErrorInfo implementation
        private event EventHandler<DataErrorsChangedEventArgs> _errorsChanged;

        protected void RaiseErrorsChanged(DataErrorsChangedEventArgs args)
        {
            if (_errorsChanged != null)
                _errorsChanged(this, args);
        }

        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add { _errorsChanged += value; }
            remove { _errorsChanged -= value; }
        }

        System.Collections.IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            var error = GetError(propertyName);
            if (error == null)
                return new object[0];
            return new[] { error };
        }

        public bool HasErrors
        {
            get { return _errors.Count(x => x.Value != null) > 0; }
        }
        #endregion

        public bool IsValid
        {
            get { return !HasErrors; }
        }
    }

    public class SimpleDataObjectWithValidation : SimpleValidableDataObject
    {
        private bool _validating = false;

        protected override void DoPropertyChanged(string propertyName)
        {
            base.DoPropertyChanged(propertyName);
            if (!_validating)
            {
                _validating = true;
                try
                {
                    Validate();
                }
                finally
                {
                    _validating = false;
                }
            }
        }

        public void Validate()
        {
            BeginValidation();
            DoValidation();
            EndValidation();
        }

        protected virtual void DoValidation()
        {
        }

        protected void BeginValidation()
        {
            foreach (var item in ErrorList)
            {
                ReportError(item.Key, null);
            }
        }

        protected void EndValidation()
        {
            foreach (var item in ErrorList)
            {
                RaiseErrorsChanged(new DataErrorsChangedEventArgs(item.Key));
            }
            DoPropertyChanged(nameof(HasErrors));
            DoPropertyChanged(nameof(IsValid));
        }
    }

    public static class SimpleDataObjectExtensions
    {
        public static void OnCollectionItemChanged<T>(this ObservableCollection<T> collection)
        {
            void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                }                        
            }
            collection.CollectionChanged += Collection_CollectionChanged;
        }

    }
}
