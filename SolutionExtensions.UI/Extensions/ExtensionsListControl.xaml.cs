using SolutionExtensions.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SolutionExtensions.UI.Extensions
{
    public class VM : SimpleDataObject
    {

        #region ValidationMessage property
        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => Set(ref _validationMessage, value);
        }
        #endregion

        #region SelectedItem property
        private ExtensionItem _selectedItem;
        public ExtensionItem SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }
        #endregion

        #region Model property
        private ExtensionsModel _model;
        public ExtensionsModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }
        #endregion

        public ObservableCollection<Control> MenuItems { get; private set; } = new ObservableCollection<Control>();
        public void AddMenuItem(string header, RoutedEventHandler click, string tooltip = null)
        {
            if (header == "-")
            {
                MenuItems.Add(new Separator());
                return;
            }
            var item = new MenuItem() { Header = header };
            if (click != null) item.Click += click;
            if (tooltip != null) item.ToolTip = tooltip;
            MenuItems.Add(item);
        }
    }

    public interface IExtensionsService
    {
        void UpdateItemFromDll(ExtensionItem item);
        void Save(ExtensionsModel model);
        void Run(ExtensionItem item, bool debug);
        bool ShowBrowseDll(ExtensionItem item, bool force);
        string[] FindExtensionClasses(ExtensionItem item);
        string GetValidation(ExtensionItem item);
    }

    public partial class ExtensionsListControl : UserControl
    {
        private ThrottleTimer _validator = new ThrottleTimer(TimeSpan.FromSeconds(0.3));
        private ThrottleTimer _saver = new ThrottleTimer(TimeSpan.FromSeconds(0.3));
        public VM ViewModel => (VM)DataContext;
        private MoveCollectionHelper _mover;
        public IExtensionsService ExtensionsService { get; private set; }
        public ExtensionsListControl()
        {
            InitializeComponent();
            DataContext = new VM();
            debugBtn.Visibility = Visibility.Collapsed;
#if DEBUG
            debugBtn.Visibility = Visibility.Visible;
#endif
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public void Init(ExtensionsModel model, IExtensionsService extensionsService)
        {
            ExtensionsService = extensionsService;
            ViewModel.Model = model;
            ViewModel.Model.Extensions.OnCollectionItemChanged(null, ViewModelExtensions_PropertyChanged);
            ViewModel.Model.Extensions.CollectionChanged += ViewModelExtensions_CollectionChanged;
            _mover = MoveCollectionHelper.Create(this, ViewModel.Model.Extensions);
            _mover.MoveCompleted += mover_MoveCompleted;
        }

        private void mover_MoveCompleted(object sender, MoveItemArgs e)
        {
            ViewModel.SelectedItem = e.Item as ExtensionItem;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.SelectedItem))
            {
                this.Validate();
            }
        }
        private void ViewModelExtensions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Package.Log($"ViewModelExtensions_PropertyChanged: {e.PropertyName}");
            var item = sender as ExtensionItem;
            if (e.PropertyName == nameof(ExtensionItem.ClassName))
            {
                ExtensionsService.UpdateItemFromDll(item);
            }
            ThrottleValidate();
            ThrottleUpdateModel();
        }

        private void ViewModelExtensions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //Package.Log($"ViewModelExtensions_CollectionChanged: {e.Action}");
            ThrottleUpdateModel();
        }
        private void ThrottleValidate()
        {
            _validator.Invoke(() => Validate());
        }

        private void ThrottleUpdateModel()
        {
            _saver.Invoke(() =>
            {
                ExtensionsService.Save(ViewModel.Model);
            });
        }

        private void AddItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ViewModel.Model.Extensions.Add(new ExtensionItem());
            this.ViewModel.SelectedItem = this.ViewModel.Model.Extensions.Last();
        }

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.ViewModel.SelectedItem == null)
                return;
            this.ViewModel.Model.Extensions.Remove(this.ViewModel.SelectedItem);
        }

        private void Run_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as ExtensionItem ?? ViewModel.SelectedItem;
            if (item == null)
                return;
            ExtensionsService.Run(item, debug: false);
        }
        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            ExtensionsService.Run(item, debug: true);
        }
        private void PickClass_Opened(object sender, EventArgs e)
        {
            var item = this.ViewModel.SelectedItem;
            if (item == null)
                return;
            if (!ExtensionsService.ShowBrowseDll(item, force: false))
                return;
            var classes = ExtensionsService.FindExtensionClasses(item);
            var cb = sender as ComboBox;
            cb.ItemsSource = classes;
        }
        private void BrowseDll_Click(object sender, RoutedEventArgs e)
        {
            var item = this.ViewModel.SelectedItem;
            if (item == null)
                return;
            ExtensionsService.ShowBrowseDll(item, force: true);
        }

        private void Validate()
        {
            var item = this.ViewModel.SelectedItem;
            if (item == null)
            {
                ViewModel.ValidationMessage = null;
                return;
            }
            ViewModel.ValidationMessage = ExtensionsService.GetValidation(item);
        }
        private void Shortcut_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            if (this.ViewModel.SelectedItem == null)
                return;
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                return;
            //TODO:gestures?
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.System || key.IsModifierKey())
                return;
            var g = e.KeyboardDevice.Modifiers == ModifierKeys.None ?
                new KeyGesture(key, ModifierKeys.Control) : //none not supported
                new KeyGesture(key, e.KeyboardDevice.Modifiers);
            this.ViewModel.SelectedItem.ShortCutKey = g.GetDisplayStringForCulture(System.Globalization.CultureInfo.InvariantCulture);
        }
        private void SetSelf_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            item.DllPath = "$(SELF)";
        }
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            var copy = new ExtensionItem()
            {
                Title = $"{item.Title} (copy)",
                ClassName = item.ClassName,
                DllPath = item.DllPath,
                //ShortCutKey=src.ShortCutKey
            };
            ViewModel.Model.Extensions.Add(copy);
            ViewModel.SelectedItem = copy;
        }
        private void ArgumentAsk_Click(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem;
            if (item == null)
                return;
            item.Argument = "?";
        }
        private void OpenContextMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).OpenContextMenu();
        }

        private void dragBorder_Initialized(object sender, EventArgs e)
        {
            _mover.AttachToMouseEvents(sender as UIElement);
        }
    }
}
