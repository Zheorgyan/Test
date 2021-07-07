using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using WpfApp.Core;

namespace WpfApp.Core {

    /// <summary>
    /// Базовый класс окна редактирования элемента
    /// </summary>
    public partial class EditorWindow : Window {

        protected EditorFormDataModel model;
        protected DbContext dbContext;

        const int ControlMargin = 2;
        const int ControlHeight = 28;
        const int DefaultGridHeight = 300;

        public EditorWindow(DbContext dbContext, DataObjectBase item) {
            this.dbContext = dbContext;
            model = new EditorFormDataModel() {
                BusinessObject = item,
                IsNewItem = item.IsNew
            };
            DataContext = model;
            InitializeComponent();
            CreateControls();
            SetupSecurity();
            Title = GetWindowTitle();
        }

        public EditorWindow() {
            InitializeComponent();
        }

        protected virtual void SetupSecurity() {
            bool? tmp = SecurityHelper.IsEditOrCreateAllowed(model.BusinessObject.GetType());
            bool isEditAllowed = LiftToTrue(tmp);
            if (tmp == null) {
                PropertyInfo[] props = ReflectionHelper.GetVisibleProperties(model.BusinessObject);
                foreach (var prop in props) {
                    tmp = SecurityHelper.IsEditAllowed(prop);
                    if (LiftToTrue(tmp)) {
                        isEditAllowed = true;
                        break;
                    }
                }
            }
            btnOk.IsEnabled = isEditAllowed;
        }

        protected virtual void CreateControls() {
            PropertyInfo[] props = ReflectionHelper.GetVisibleProperties(model.BusinessObject);
            if (props.Length == 0) {
                throw new InvalidOperationException(string.Format("Для типа {0} не определены свойства для отображения с помощью атрибута DisplayAttribute.", model.BusinessObject.GetType().Name));
            }
            List<UIElement> controls = new List<UIElement>();
            List<UIElement> labels = new List<UIElement>();
            List<UIElement> tabItems = new List<UIElement>();
            foreach (var property in props) {
                FieldVisibility vis = ReflectionHelper.GetVisibility(property);
                if (!vis.HasFlag(FieldVisibility.Form)) {
                    continue;
                }
                UIElement control = CreateControl(property);
                if (control != null) {
                    bool isSeparateTab = IsPlaceOnSeparateTab(property);
                    if (!isSeparateTab) {
                        if (control is Control && !(control is System.Windows.Controls.Label)) {
                            ((Control)control).Margin = new Thickness(ControlMargin, ControlMargin, ControlMargin, ControlMargin);
                        }
                        controls.Add(control);
                        Control label = CreateLabel(property);
                        labels.Add(label);
                    } else {
                        ContentControl tabItem = CreateTabItem(property);
                        DockPanel dockPanel = new DockPanel();
                        dockPanel.LastChildFill = true;
                        dockPanel.Children.Add(control);
                        tabItem.Content = dockPanel;
                        tabItems.Add(tabItem);
                    }
                }
            }

            for (int i = 0; i < controls.Count; i++) {
                UIElement control = controls[i];
                UIElement label = labels[i];
                RowDefinition rowDef = new RowDefinition();
                rowDef.Height = GridLength.Auto;
                layoutGrid.RowDefinitions.Insert(0, rowDef);
                layoutGrid.Children.Add(control);
                control.SetValue(Grid.ColumnProperty, 1);
                control.SetValue(Grid.RowProperty, layoutGrid.RowDefinitions.Count - 2);
                layoutGrid.Children.Add(label);
                label.SetValue(Grid.ColumnProperty, 0);
                label.SetValue(Grid.RowProperty, layoutGrid.RowDefinitions.Count - 2);
            }

            if (tabItems.Count > 0) {
                RowDefinition rowDef = new RowDefinition();
                layoutGrid.RowDefinitions.Insert(layoutGrid.RowDefinitions.Count - 1, rowDef);
                ItemsControl tabControl = CreateTabControl();
                layoutGrid.Children.Add(tabControl);
                tabControl.SetValue(Grid.RowProperty, layoutGrid.RowDefinitions.Count - 2);
                tabControl.SetValue(Grid.ColumnSpanProperty, 2);
                for (int i = 0; i < tabItems.Count; i++) {
                    tabControl.Items.Add(tabItems[i]);
                }
            } else {
                RowDefinition rowDef = new RowDefinition();
                layoutGrid.RowDefinitions.Insert(layoutGrid.RowDefinitions.Count - 1, rowDef);
            }

            layoutGridLabelColumn.Width = GridLength.Auto;
            buttonsPanel.SetValue(Grid.RowProperty, layoutGrid.RowDefinitions.Count - 1);
            MinHeight = buttonsPanel.MinHeight + controls.Count * ControlHeight + ((tabItems.Count > 0) ? DefaultGridHeight : 0);
        }

        protected virtual string GetWindowTitle() {
            if (dbContext != null) {
                if (model.IsNewItem) {
                    return string.Format("{0} - [новая запись]", ReflectionHelper.GetTypeName(model.BusinessObject));
                } else {
                    return string.Format("{0} - [редактирование]", ReflectionHelper.GetTypeName(model.BusinessObject));
                }
            } else {
                return ReflectionHelper.GetTypeName(model.BusinessObject);
            }
        }

        protected virtual UIElement CreateControl(PropertyInfo property) {
            Type type = property.PropertyType;
            if (type == typeof(string)) {
                return CreateTextBox(property);
            }
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)
                || type == typeof(decimal?) || type == typeof(double?) || type == typeof(float?)) {
                return CreateNumericUpDown(property, true);
            }
            if (type == typeof(int) || type == typeof(uint) || type == typeof(byte) || type == typeof(short) || type == typeof(long) || type == typeof(ulong)
                || type == typeof(int?) || type == typeof(uint?) || type == typeof(byte?) || type == typeof(short?) || type == typeof(long?) || type == typeof(ulong?)) {
                return CreateNumericUpDown(property, false);
            }
            if (type == typeof(DateTime) || type == typeof(DateTime?)) {
                return CreateDateTimePicker(property);
            }
            if (type == typeof(bool) || type == typeof(bool?)) {
                return CreateCheckBox(property);
            }
            if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType) {
                return CreateGrid(property);
            } else if (!type.IsValueType) {
                return CreateComboBox(property);
            }
            throw new NotSupportedException();
        }

        protected bool IsPlaceOnSeparateTab(PropertyInfo property) {
            Type type = property.PropertyType;
            if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType) {
                return true;
            }
            return false;
        }

        protected virtual Control CreateLabel(PropertyInfo property) {
            var control = new System.Windows.Controls.Label();
            control.Content = ReflectionHelper.GetPropertyName(property);
            if (ReflectionHelper.IsPropertyRequired(property)) {
                control.Content += "*";
            }
            control.Content += ":";
            return control;
        }

        protected virtual Control CreateTextBox(PropertyInfo property) {
            bool isReadOnly = ReflectionHelper.IsPropertyReadonly(property);
            if (!isReadOnly) {
                TextBox control = new TextBox();
                int maxLength = ReflectionHelper.GetPropertyTextMaxLength(property);
                if (maxLength > 0) {
                    control.MaxLength = maxLength;
                }
                SetupBinding(control, TextBox.TextProperty, property);
                return control;
            } else {
                var control = new System.Windows.Controls.Label();
                control.BorderThickness = new Thickness(1);
                control.BorderBrush = new SolidColorBrush(Color.FromRgb(240,240,240));
                control.Padding = new Thickness(1);
                control.Margin = new Thickness(2);
                SetupBinding(control, System.Windows.Controls.Label.ContentProperty, property);
                return control;
            }
        }

        protected virtual Control CreateNumericUpDown(PropertyInfo property, bool allowFractional) {
            bool isReadOnly = ReflectionHelper.IsPropertyReadonly(property);
            if (isReadOnly) {
                return CreateTextBox(property);
            }
            RadNumericUpDown control = new RadNumericUpDown();
            control.IsReadOnly = ReflectionHelper.IsPropertyReadonly(property);
            if (allowFractional) {
                control.NumberDecimalDigits = 2;
                string format = ReflectionHelper.GetPropertyDisplayFormat(property);
                if (format != null && format.StartsWith("F")) {
                    format = format.Remove(0, 1);
                    control.NumberDecimalDigits = Int32.Parse(format);
                }
            } else {
                control.NumberDecimalDigits = 0;
            }
            SetupBinding(control, RadNumericUpDown.ValueProperty, property);
            return control;
        }

        protected virtual Control CreateCheckBox(PropertyInfo property) {
            CheckBox control = new CheckBox();
            control.IsEnabled = !ReflectionHelper.IsPropertyReadonly(property);
            control.VerticalAlignment = VerticalAlignment.Center;
            SetupBinding(control, CheckBox.IsCheckedProperty, property);
            return control;
        }

        protected virtual UIElement CreateComboBox(PropertyInfo property) {
            Control editorControl;
            bool isReadOnly = ReflectionHelper.IsPropertyReadonly(property);
            bool? isAllowEditList = SecurityHelper.IsEditOrCreateAllowed(property.PropertyType);
            if (isAllowEditList == null) {
                isAllowEditList = true;
            }
            if (isReadOnly) {
                isAllowEditList = false;
            }
            if (!isReadOnly) {
                RadComboBox control = new RadComboBox();
                List<DataObjectBase> items = GetCollectionFromDbContext(property.PropertyType);
                control.ItemsSource = items.OrderBy(t => t.ToString());
                if (items.Count > 10) {
                    control.TextSearchMode = TextSearchMode.Contains;
                    control.IsEditable = true;
                    control.IsFilteringEnabled = true;
                    control.OpenDropDownOnFocus = true;
                }
                SetupBinding(control, RadComboBox.SelectedItemProperty, property);
                editorControl = control;
            } else {
                editorControl = CreateTextBox(property);
            }

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(editorControl);
            editorControl.SetValue(Grid.ColumnProperty, 0);
            editorControl.Margin = new Thickness(ControlMargin, ControlMargin, ControlMargin, ControlMargin);

            if (isAllowEditList == true || isAllowEditList == null) {
                var editButton = new RadButton();
                editButton.Content = "...";
                editButton.Width = 32;
                editButton.ToolTip = "Редактировать список";
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                grid.Children.Add(editButton);
                editButton.SetValue(Grid.ColumnProperty, 2);
                editButton.Margin = new Thickness(ControlMargin, ControlMargin, ControlMargin, ControlMargin);
                editButton.Click += ((sender, e) => {
                    UIHelper.EditCollection(property.PropertyType, this);
                    if (!isReadOnly) {
                        List<DataObjectBase> items = GetCollectionFromDbContext(property.PropertyType);
                        ((RadComboBox)editorControl).ItemsSource = items;
                        OnModelChanged(model.BusinessObject, property.Name);
                    }
                });
            }

            if (!isReadOnly) {
                var clearButton = new RadButton();
                clearButton.Content = "Х";
                clearButton.Width = 32;
                clearButton.ToolTip = "Очистить поле";
                clearButton.Click += new RoutedEventHandler((sender, e) => {
                    editorControl.SetValue(RadComboBox.SelectedItemProperty, null);
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                clearButton.SetValue(Grid.ColumnProperty, 1);
                clearButton.Margin = new Thickness(ControlMargin, ControlMargin, ControlMargin, ControlMargin);
                grid.Children.Add(clearButton);
            }

            return grid;
        }

        protected virtual Control CreateDateTimePicker(PropertyInfo property) {
            string format = ReflectionHelper.GetPropertyDisplayFormat(property);
            if (string.IsNullOrEmpty(format) || !format.Contains("HH")) {
                RadDatePicker control = new RadDatePicker();
                control.IsReadOnly = ReflectionHelper.IsPropertyReadonly(property);
                SetupBinding(control, RadDatePicker.SelectedValueProperty, property);
                control.DateTimeWatermarkContent = "";
                return control;
            } else {
                if(format.Contains("dd") || format.Contains("MM") || format.Contains("yy")) {
                    RadDateTimePicker control = new RadDateTimePicker();
                    control.IsReadOnly = ReflectionHelper.IsPropertyReadonly(property);
                    control.DateTimeWatermarkContent = "";
                    SetupBinding(control, RadDateTimePicker.SelectedValueProperty, property);
                    return control;
                } else {
                    RadTimePicker control = new RadTimePicker();
                    control.IsReadOnly = ReflectionHelper.IsPropertyReadonly(property);
                    control.DateTimeWatermarkContent = "";
                    SetupBinding(control, RadDateTimePicker.SelectedValueProperty, property);
                    return control;
                }
            }
        }

        protected virtual Control CreateGrid(PropertyInfo property) {
            Type gridItemType = property.PropertyType.GenericTypeArguments.FirstOrDefault();
            GridControl control = new GridControl(gridItemType);
            SetupBinding(control, ItemsControl.ItemsSourceProperty, property);
            control.MinHeight = 80;
            control.DataRefresh += ((sender, args) => {
                GridDataRefresh(control, property, gridItemType);
            });
            control.CreateNewClick += ((sender, args) => {
                GridCreateNewItem(control, property, gridItemType);
                OnModelChanged(model.BusinessObject, property.Name);
            });
            control.EditClick += ((sender, args) => {
                GridItemEdit(control, property, gridItemType);
                OnModelChanged(model.BusinessObject, property.Name);
            });
            control.DeleteClick += ((sender, args) => {
                GridItemDelete(control, property, gridItemType);
                OnModelChanged(model.BusinessObject, property.Name);
            });
            control.AllowAdd = control.AllowDelete = LiftToTrue(SecurityHelper.IsEditAllowed(property));
            control.AllowEdit = true;
            return control;
        }

        bool LiftToTrue(bool? value) {
            return (value == true || value == null);
        }

        protected virtual void GridDataRefresh(GridControl grid, PropertyInfo itemsSourceProperty, Type gridItemType) {
            var collection = (itemsSourceProperty.GetValue(model.BusinessObject) as IEnumerable);
            grid.ItemsSource = new List<object>(collection.Cast<object>());
        }

        protected virtual void GridCreateNewItem(GridControl grid, PropertyInfo itemsSourceProperty, Type gridItemType) {
            var collection = (itemsSourceProperty.GetValue(model.BusinessObject));
            if (collection == null) {
                return;
            }
            DataObjectBase item = UIHelper.CreateObject(gridItemType, this, dbContext, false, (t) => {
                return GridAfterNewItemInitialized(itemsSourceProperty, t);
            });
            if (item != null) {
                if (collection is IList) {
                    ((IList)collection).Add(item);
                } else {
                    MethodInfo addMethod = itemsSourceProperty.PropertyType.GetMethod("Add", new Type[] { gridItemType });
                    if (addMethod != null) {
                        addMethod.Invoke(collection, new object[] { item });
                    }
                }
            }
        }

        protected virtual bool GridAfterNewItemInitialized(PropertyInfo collectionProperty, object createdItem) {
            return true;
        }

        protected virtual void GridItemEdit(GridControl grid, PropertyInfo itemsSourceProperty, Type gridItemType) {
            if (grid.SelectedItem != null) {
                UIHelper.EditObject((DataObjectBase)grid.SelectedItem, this, dbContext, false);
            }
        }

        protected virtual void GridItemDelete(GridControl grid, PropertyInfo itemsSourceProperty, Type gridItemType) {
            if (grid.SelectedItem != null) {
                var collection = (itemsSourceProperty.GetValue(model.BusinessObject));
                if (collection == null) {
                    return;
                }
                if (collection is IList) {
                    ((IList)collection).Remove(grid.SelectedItem);
                } else {
                    MethodInfo removeMethod = itemsSourceProperty.PropertyType.GetMethod("Remove", new Type[] { gridItemType });
                    if (removeMethod != null) {
                        removeMethod.Invoke(collection, new object[] { grid.SelectedItem });
                    }
                }
            }
        }

        protected virtual ItemsControl CreateTabControl() {
            TabControl control = new TabControl();
            control.Margin = new Thickness(0, 16, 0, 0);
            return control;
        }

        protected virtual ContentControl CreateTabItem(PropertyInfo property) {
            TabItem tabItem = new TabItem();
            tabItem.Header = ReflectionHelper.GetPropertyName(property);
            if (ReflectionHelper.IsPropertyRequired(property)) {
                tabItem.Header += "*";
            }
            return tabItem;
        }

        protected void SetupBinding(Control control, DependencyProperty dependencyProperty, PropertyInfo property) {
            Binding binding = CreateBinding(property);
            control.SetBinding(dependencyProperty, binding);
            control.SourceUpdated += Control_SourceUpdated;
        }

        protected Binding CreateBinding(PropertyInfo property) {
            Binding binding = new Binding("BusinessObject." + property.Name);
            binding.NotifyOnSourceUpdated = true;
            binding.Source = model;
            if (!property.CanWrite) {
                binding.Mode = BindingMode.OneWay;
            }
            return binding;
        }

        private void Control_SourceUpdated(object sender, DataTransferEventArgs e) {
            BindingExpression expr = (e.TargetObject as Control).GetBindingExpression(e.Property);
            object source = expr.ResolvedSource;
            string propertyName = expr.ResolvedSourcePropertyName;
            OnModelChanged(source, propertyName);
        }

        protected virtual List<DataObjectBase> GetCollectionFromDbContext(Type entityType) {
            return EFHelper.GetObjectCollection(dbContext, entityType);
        }

        protected virtual void OnModelChanged(object source, string propertyName) {
            model.BusinessObject.OnPropertyChanged(dbContext, propertyName);
            model.NotifyModelChanged();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }


    public class EditorFormDataModel : INotifyPropertyChanged {
        public bool IsNewItem { get; set; }
        public DataObjectBase BusinessObject { get; set; }        
        public void NotifyModelChanged() {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs("BusinessObject"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
