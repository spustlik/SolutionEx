using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolutionExtensions.Model
{
    public static class SimpleDataObjectBinderExtensions
    {
        public static Binder OnAnyChanged(this INotifyPropertyChanged obj, OnChange onChanged)
        {
            var b = new Binder();
            b.Init(onChanged, obj);
            b.Subscribe();
            return b;
        }

        public static Binder OnAnyChanged<T>(this ObservableCollection<T> collection, OnChange onChanged)
        {
            var b = new Binder();
            b.Init(onChanged, collection);
            b.Subscribe();
            return b;
        }

        internal static bool IsSystem(this Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type.Assembly == typeof(System.Object).Assembly;
        }
        internal static bool IsBindable(this PropertyInfo prop)
        {
            return /*prop.PropertyType.IsBindable() && to neni pravda, na property typu string se mohu navesit*/
                prop.GetIndexParameters().Length == 0;
        }

        internal static bool IsBindable(this Type type)
        {
            return !type.IsPrimitive && !type.IsSystem();
        }
    }

    public enum ChangeAction
    {
        PropertyChanged,
        CollectionChanged,
    }
    public class ChangeEventArgs
    {
        public ChangeAction Action { get; }
        //list of part of path, last item contains name of changed property
        public string[] PathList { get; }
        //returns parhlist, but with collection items as "[]"
        public string[] GetNormalizedPathList()
        {
            string Normalize(string s)
            {
                if (s.StartsWith("[") && s.EndsWith("]"))
                    return "[]";
                return s;
            }
            return PathList.Select(x => Normalize(x)).ToArray();
        }
        public string PathText => String.Join(".", PathList);
        //if action is CollectionChanged, this contains original event args of NotifyCollectionChanged event
        public NotifyCollectionChangedEventArgs CollectionChangedArgs { get; }
        public ChangeEventArgs(ChangeAction action, 
            string[] path, string name,
            NotifyCollectionChangedEventArgs collectionChangedArgs = null)
        {
            Action = action;
            if (name != null)
                path = path.Concat(new[] { name }).ToArray();
            PathList = path;
            CollectionChangedArgs = collectionChangedArgs;
        }
    }
    public delegate void OnChange(object sender, ChangeEventArgs e);

    public class Binder
    {

        [Conditional("DEBUG")]
        private void Log(string msg) { System.Diagnostics.Debug.WriteLine($"[BINDER]:{msg}"); }
        protected OnChange OnChanged { get; private set; }
        private WeakReference _target;
        public object Target => _target.Target;

        protected string[] PathList { get; private set; } = new string[0];
        protected string PathText => String.Join(".", PathList);
        public void Init(OnChange onChanged, object obj)
        {
            this.OnChanged = onChanged;
            _target = new WeakReference(obj);
        }
        public void Init(Binder parent, object target, string name)
        {
            Init(parent.OnChanged, target);
            PathList = parent.PathList;
            PathList = PathList.Concat(new[] { name }).ToArray();
        }

        protected (object value, PropertyInfo propertyInfo) GetPropertyValueByName(object o, string propertyName)
        {
            var propertyInfo = o.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                return default;
            var value = propertyInfo.GetValue(o, null);
            return (value, propertyInfo);
        }

        private Binder GetBinder(object value, string name)
        {
            if (value == null || !value.GetType().IsBindable())
                return null;
            var b = new Binder();
            b.Init(this, value, name);
            b.Subscribe();
            return b;
        }

        public void Subscribe()
        {
            Log($"Subscribe {Target}");
            SubscribeToTarget(true);
        }

        public void Unsubscribe()
        {
            Log($"Unsubscribe {Target}");
            SubscribeToTarget(false);
        }
        class Disposer : IDisposable
        {
            private Binder binder;
            public Disposer(Binder binder) => this.binder = binder;
            void IDisposable.Dispose() => binder.Unsubscribe();
        }
        public IDisposable UnsubscribeDisposer()
        {
            return new Disposer(this);
        }
        private void UnsubscribeBinders<TK>(Dictionary<TK, Binder> binders)
        {
            var old = binders.Values.ToArray();
            binders.Clear();
            foreach (var o in old)
            {
                o.Unsubscribe();
            }
        }

        private void SubscribeToTarget(bool onOff)
        {
            if (Target is INotifyPropertyChanged pc)
            {
                if (onOff)
                    pc.PropertyChanged += OnPropertyChanged;
                else
                    pc.PropertyChanged -= OnPropertyChanged;
            }
            if (Target != null && Target.GetType().IsBindable())
            {
                if (Target is IList)
                {
                    SubscribeToCollection(onOff);
                }
                else
                {
                    BindToProperties(onOff);
                }
            }
        }

        #region Properties
        private Dictionary<string, Binder> _propertyBinders = new Dictionary<string, Binder>();
        private void BindToProperties(bool onOff)
        {
            if (onOff)
            {
                //bind object and collection props
                var obj = Target;
                foreach (var pi in obj.GetType().GetProperties())
                {
                    if (pi.IsBindable())
                    {
                        var propertyValue = pi.GetValue(obj);
                        BindToProperty(pi.Name, propertyValue, true);
                    }
                }
            }
            else
            {
                UnsubscribeBinders(_propertyBinders);
            }
        }
        private void BindToProperty(string propertyName, object propertyValue, bool onOff)
        {
            if (onOff)
            {
                var binder = GetBinder(propertyValue, propertyName);
                if (binder != null)
                {
                    Log($"  BindToProperty {propertyName}");
                    _propertyBinders[propertyName] = binder;
                }
                return;
            }
            else
            {
                if (_propertyBinders.TryGetValue(propertyName, out var binder))
                    binder.Unsubscribe();
            }
        }
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var p = GetPropertyValueByName(sender, e.PropertyName);
            if (sender is IList && (e.PropertyName == "Count" || e.PropertyName == "Item[]"))
                return; //Ignore Count,Item[] on collection
            if (p == default)
                throw new InvalidOperationException($"PropertyChanged fired on unknown property");
            BindToProperty(e.PropertyName, null, false);
            var args = new ChangeEventArgs(
                ChangeAction.PropertyChanged,
                PathList,
                e.PropertyName);
            OnChanged(sender, args);
            BindToProperty(e.PropertyName, p.value, true);
        }
        #endregion

        #region Collection
        private void SubscribeToCollection(bool onOff)
        {
            if (Target is INotifyCollectionChanged cc)
            {
                if (onOff)
                    cc.CollectionChanged += OnCollectionChanged;
                else
                    cc.CollectionChanged -= OnCollectionChanged;
            }
            if (onOff && Target is IList list)
            {
                BindToCollectionItems(list, 0, true);
            }
        }

        private Dictionary<int, Binder> _collectionBinders = new Dictionary<int, Binder>();
        private void BindToCollectionItems(IList list, int startIndex, bool onOff)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                BindToCollectionItem(list[i], i + startIndex, onOff);
            }
        }
        private void BindToCollectionItem(object item, int index, bool onOff)
        {
            if (onOff)
            {
                if (item == null) return;
                var binder = GetBinder(item, $"[{index}]");
                if (binder != null)
                {
                    Log($"  BindToCollectionItem {index}");
                    _collectionBinders[index] = binder;
                }
                return;
            }
            else
            {
                if (_collectionBinders.TryGetValue(index, out var binder))
                {
                    binder.Unsubscribe();
                    _collectionBinders.Remove(index);
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var args = new ChangeEventArgs(
                ChangeAction.CollectionChanged,
                PathList,
                name: null,
                e);
            OnChanged(sender, args);
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                MoveBinders(e);
                return;
            }
            BindToCollectionItems(e.OldItems, e.OldStartingIndex, false);
            BindToCollectionItems(e.NewItems, e.NewStartingIndex, true);
        }

        private void MoveBinders(NotifyCollectionChangedEventArgs e)
        {
            // 012  5 7
            // ..XYZ..abc. 2 -> 7
            // ..abc..XYZ. 
            // ..bc.XY...  5 -> 2
            // ..XY.bc... 
            // ..XYZa....  2 -> 3
            // ..abc......
            // ..aXYZ....
            var indexOld = e.OldStartingIndex; //original index
            var indexNew = e.NewStartingIndex; //new index
            //oldItems and newIterms are same (moved items)
            var count = e.OldItems.Count;
            //save XYZ
            var oldBinders = new List<Binder>();
            for (int i = 0; i < count; i++)
                oldBinders.Add(_collectionBinders[indexOld + i]);
            //move 'abc'
            for (int i = 0; i < count; i++)
                _collectionBinders[indexOld + i] = _collectionBinders[indexNew + i];
            //move 'XYZ'
            for (int i = 0; i < count; i++)
                _collectionBinders[indexNew + i] = oldBinders[i];
            //throw new NotImplementedException($"Move event {indexOld} => {indexNew} of collection not implemented for now");
        }
        #endregion
    }
}