using Microsoft.VisualStudio.VCProjectEngine;
using SolutionExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace SolutionExtensions
{

    public abstract class ReflectorNode : SimpleDataObject
    {
        public ReflectorNode Parent { get; private set; }
        public void GetPath(out List<ReflectorNode> path)
        {
            if (Parent == null)
                path = new List<ReflectorNode>();
            else
                Parent.GetPath(out path);
            path.Add(this);
        }
        public ObservableCollection<ReflectorNode> Children { get; } = new ObservableCollection<ReflectorNode>();

        #region Error property
        private string _error;
        public string Error
        {
            get => _error;
            set => Set(ref _error, value);
        }
        #endregion

        //bound to IsSelected of TreeViewItem, set to select
        #region IsSelected property
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }
        #endregion
        //bound to IsExpanded
        #region IsExpanded property
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }
        #endregion

        public ReflectorNode()
        {
            Children.CollectionChanged += Children_CollectionChanged;
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (ReflectorNode node in e.OldItems)
                    node.Parent = null;
            if (e.NewItems != null)
                foreach (ReflectorNode node in e.NewItems)
                    node.Parent = this;
        }

        #region CanExpandEnumerable property
        private bool _canExpandEnumerable;
        public bool CanExpandEnumerable
        {
            get => _canExpandEnumerable;
            set => Set(ref _canExpandEnumerable, value);
        }
        #endregion

        #region CanExpandProperties property
        private bool _canExpandProperties;
        public bool CanExpandProperties
        {
            get => _canExpandProperties;
            set => Set(ref _canExpandProperties, value);
        }
        #endregion

        #region CanExpandMethods property
        private bool _canExpandMethods;
        public bool CanExpandMethods
        {
            get => _canExpandMethods;
            set => Set(ref _canExpandMethods, value);
        }
        #endregion

        #region CanExpandInterfaces property
        private bool _canExpandInterfaces;
        public bool CanExpandInterfaces
        {
            get => _canExpandInterfaces;
            set => Set(ref _canExpandInterfaces, value);
        }
        #endregion
    }

    public abstract class ReflectorTypeNode : ReflectorNode
    {
        #region ValueType property
        private Type _valueType;
        public Type ValueType
        {
            get => _valueType;
            set => Set(ref _valueType, value);
        }
        #endregion

        #region ValueTypeName property
        private string _valueTypeName;
        public string ValueTypeName
        {
            get => _valueTypeName;
            set => Set(ref _valueTypeName, value);
        }
        #endregion

        #region IsSimpleType property
        private bool _isSimpleType;
        public bool IsSimpleType
        {
            get => _isSimpleType;
            set => Set(ref _isSimpleType, value);
        }
        #endregion
    }
    public abstract class ReflectorValueNode : ReflectorTypeNode
    {
        #region Value property
        private object _value;
        public object Value
        {
            get => _value;
            set => Set(ref _value, value);
        }
        #endregion

        #region ValueSimpleText property
        private string _valueSimpleText;
        public string ValueSimpleText
        {
            get => _valueSimpleText;
            set => Set(ref _valueSimpleText, value);
        }
        #endregion

        #region IsNull property
        private bool _isNull;
        public bool IsNull
        {
            get => _isNull;
            set => Set(ref _isNull, value);
        }
        #endregion
    }

    public class ReflectorPropertyValue : ReflectorValueNode
    {
        #region HasValue property
        private bool _myProperty;
        public bool HasValue
        {
            get => _myProperty;
            set => Set(ref _myProperty, value);
        }
        #endregion

        #region PropertyInfo property
        private PropertyInfo _propertyInfo;
        public PropertyInfo PropertyInfo
        {
            get => _propertyInfo;
            set => Set(ref _propertyInfo, value);
        }
        #endregion

        #region PropertyModifiers property
        private string _propertyModifiers;
        public string PropertyModifiers
        {
            get => _propertyModifiers;
            set => Set(ref _propertyModifiers, value);
        }
        #endregion

        #region PropertyName property
        private string _propertyName;
        public string PropertyName
        {
            get => _propertyName;
            set => Set(ref _propertyName, value);
        }
        #endregion

        #region PropertyType property
        private Type _propertyType;
        public Type PropertyType
        {
            get => _propertyType;
            set => Set(ref _propertyType, value);
        }
        #endregion

        #region PropertyTypeName property
        private string _propertyTypeName;
        public string PropertyTypeName
        {
            get => _propertyTypeName;
            set => Set(ref _propertyTypeName, value);
        }
        #endregion

    }

    public class ReflectorEnumItem : ReflectorValueNode
    {
        #region Index property
        private int _index;
        public int Index
        {
            get => _index;
            set => Set(ref _index, value);
        }
        #endregion

        #region ItemDefaultName property
        private string _itemDefaultName;
        public string ItemDefaultName
        {
            get => _itemDefaultName;
            set => Set(ref _itemDefaultName, value);
        }
        #endregion

        #region ItemDefaultValue property
        private string _itemDefaultValue;
        public string ItemDefaultValue
        {
            get => _itemDefaultValue;
            set => Set(ref _itemDefaultValue, value);
        }
        #endregion

    }
    public class ReflectorMethod : ReflectorNode
    {
        #region MethodInfo property
        private MethodInfo _methodInfo;
        public MethodInfo MethodInfo
        {
            get => _methodInfo;
            set => Set(ref _methodInfo, value);
        }
        #endregion

        #region MethodName property
        private string _methodName;
        public string MethodName
        {
            get => _methodName;
            set => Set(ref _methodName, value);
        }
        #endregion

        #region Signature property
        private string _signature;
        public string Signature
        {
            get => _signature;
            set => Set(ref _signature, value);
        }
        #endregion
    }
    public class ReflectorInterface : ReflectorValueNode
    {
        #region IsCOM property
        private bool _IsCOM;
        public bool IsCOM
        {
            get => _IsCOM;
            set => Set(ref _IsCOM, value);
        }
        #endregion

    }
    public class ReflectorRoot : ReflectorValueNode
    {
        #region RootType property
        private string _rootType;
        public string RootType
        {
            get => _rootType;
            set => Set(ref _rootType, value);
        }
        #endregion
    }
    public class ReflectorVM : SimpleDataObject
    {
        public ObservableCollection<ReflectorNode> Children { get; } = new ObservableCollection<ReflectorNode>();

        //set from TV event, or you canuse it to select node
        #region SelectedNode property
        private ReflectorNode _selectedNode;
        public ReflectorNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (value != null) 
                    value.IsSelected = true;
                else 
                    _selectedNode.IsSelected = false;
                Set(ref _selectedNode, value);
            }
        }
        #endregion

    }
}
