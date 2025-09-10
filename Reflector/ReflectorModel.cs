using SolutionExtensions;
using SolutionExtensions.Reflector;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Reflector
{
    public class ReflectorNode : SimpleDataObject
    {
        public ReflectorNode Parent { get; private set; }
        public ObservableCollection<ReflectorNode> Children { get; set; } = new ObservableCollection<ReflectorNode>();

        #region Error property
        private string _error;
        public string Error
        {
            get => _error;
            set => Set(ref _error, value);
        }
        #endregion

        public ReflectorNode()
        {
            Children.CollectionChanged += Children_CollectionChanged;
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (ReflectorNode node in e.OldItems)
                node.Parent = null;
            foreach (ReflectorNode node in e.NewItems)
                node.Parent = this;
        }
    }

    public abstract class ReflectorTypeNode : ReflectorNode
    {
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

        #region CanExpandEnumerable property
        private bool _canExpandEnumerable;
        public bool CanExpandEnumerable
        {
            get => _canExpandEnumerable;
            set => Set(ref _canExpandEnumerable, value);
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
    public class ReflectorInterface : ReflectorTypeNode
    {
        #region IsCOM property
        private bool _isCom;
        public bool IsCOM
        {
            get => _isCom;
            set => Set(ref _isCom, value);
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


    public class ReflectorFactory
    {
        public ReflectionCOM COM { get; } = new ReflectionCOM();
        public ReflectionBuilderCS Builder { get; } = new ReflectionBuilderCS();
        public void SetRoot(ReflectorRoot root, string rootType, object value)
        {
            root.RootType = rootType;
            SetValue(root, value);
        }

        private void SetValue(ReflectorValueNode node, object value)
        {
            node.Value = value;
            if (value == null)
            {
                node.IsNull = true;
                node.IsSimpleType = true;
                return;
            }
            node.IsNull = false;
            SetValueType(node, value);
            if (!node.IsSimpleType)
            {
                node.CanExpandEnumerable = node.Value is IEnumerable;
            }
            else
            {
                node.ValueSimpleText = value.ToString();
            }
        }

        private void SetValueType(ReflectorTypeNode node, object value)
        {
            node.ValueType = value.GetType();
            node.ValueTypeName = Builder.GetTypeName(node.ValueType);
            node.IsSimpleType = node.ValueType == typeof(string) || node.ValueType.IsPrimitive;
            if (!node.IsSimpleType)
            {
                node.CanExpandMethods = true;
                node.CanExpandProperties = true;
                node.CanExpandInterfaces = node.ValueType.GetInterfaces().Length > 0 || ReflectionCOM.IsCOMObjectType(node.ValueType);
            }
        }

        public void ExpandMethods(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandMethods)
                return;
            parent.CanExpandMethods = false;
            foreach (var mi in parent.ValueType.GetMethods())
            {
                var node = new ReflectorMethod()
                {
                    MethodInfo = mi,
                    MethodName = mi.Name,
                    Signature = Builder.GetMethodSignature(mi)
                };
                parent.Children.Add(node);
            }
        }
        public void ExpandProperties(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandProperties)
                return;
            parent.CanExpandProperties = false;
            foreach (var pi in parent.ValueType.GetProperties())
            {
                var node = new ReflectorPropertyValue()
                {
                    PropertyInfo = pi,
                    PropertyName = pi.Name,
                    PropertyType = pi.PropertyType,
                    PropertyTypeName = Builder.GetTypeName(pi.PropertyType),
                };
                if (parent is ReflectorValueNode parentv)
                {
                    try
                    {
                        var propValue = pi.GetValue(parentv.Value);
                        SetValue(node, propValue);
                        node.HasValue = true;
                    }
                    catch (Exception ex)
                    {
                        AddError(node, ex);
                    }
                }
                parent.Children.Add(node);
            }
        }
        public void ExpandEnumerable(ReflectorValueNode parent)
        {
            if (!parent.CanExpandEnumerable || !(parent.Value is IEnumerable))
                return;
            parent.CanExpandEnumerable = false;
            int index = 0;
            try
            {
                foreach (var item in parent.Value as IEnumerable)
                {
                    var node = new ReflectorEnumItem()
                    {
                        Index = index++,
                    };
                    SetValue(node, item);
                    parent.Children.Add(node);
                }
            }
            catch (Exception ex)
            {
                AddError(parent, ex);
            }
        }
        public void ExpandInterfaces(ReflectorTypeNode parent)
        {
            if (!parent.CanExpandInterfaces)
                return;
            parent.CanExpandInterfaces = false;
            foreach (var intfType in parent.ValueType.GetInterfaces())
            {
                var node = new ReflectorInterface()
                {
                    ValueType = intfType,
                    ValueTypeName = Builder.GetTypeName(intfType),
                    CanExpandMethods = true,
                    CanExpandProperties = true
                };
                parent.Children.Add(node);
            }
            if (parent is ReflectorPropertyValue parentv)
            {
                var com = COM.GetInterfaces(parentv.Value);
                foreach (var intfType in com)
                {
                    var node = new ReflectorInterface()
                    {
                        IsCOM = true,
                        ValueType = intfType,
                        ValueTypeName = Builder.GetTypeName(intfType),
                        CanExpandMethods = true,
                        CanExpandProperties = true
                    };
                    parent.Children.Add(node);
                }
            }

        }
        public void AddError(ReflectorNode node, Exception ex)
        {
            if (!string.IsNullOrEmpty(node.Error))
            {
                node.Error += "\n";
            }
            node.Error += ex.Message;
        }

        internal string BuildNodeSource(ReflectorNode node)
        {
            //interface->full intf
            if (node is ReflectorInterface nodei)
            {
                return Builder.GenerateInterface(nodei.ValueType);
            }
            //class->abstract class,
            //if (node is ReflectorTypeNode typeNode)
            //{
            //    return Builder.GenerateAbstractClass(typeNode.ValueType);
            //}
            if (node is ReflectorPropertyValue propNode)
            {
                //property with value ->class of value type
                if (propNode.ValueType != null)
                    return Builder.GenerateAbstractClass(propNode.ValueType);
                else
                    //property without value - containing class (pi.declaringtype)
                    return Builder.GenerateAbstractClass(propNode.PropertyInfo.DeclaringType);
            }
            //method -> containing class
            if (node is ReflectorMethod methodNode)
            {
                return Builder.GenerateAbstractClass(methodNode.MethodInfo.DeclaringType);
            }
            //enum item->class of value
            if (node is ReflectorEnumItem enumNode)
            {
                return Builder.GenerateAbstractClass(enumNode.ValueType);
            }
            return null;
        }
    }
}
