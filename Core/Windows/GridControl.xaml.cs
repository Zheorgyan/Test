using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace WpfApp.Core
{
    /// <summary>
    /// Пользовательский элемент управления для отображения табличного списка
    /// </summary>
    public partial class GridControl : UserControl
    {
        public static readonly DependencyProperty AllowAddProperty =
            DependencyProperty.Register("AllowAdd", typeof(bool), typeof(GridControl),
                                        new PropertyMetadata(false));

        public static readonly DependencyProperty AllowEditProperty =
            DependencyProperty.Register("AllowEdit", typeof(bool), typeof(GridControl),
                                        new PropertyMetadata(false));

        public static readonly DependencyProperty AllowDeleteProperty =
            DependencyProperty.Register("AllowDelete", typeof(bool), typeof(GridControl),
                                        new PropertyMetadata(false));

        public static readonly DependencyProperty AllowPrintProperty =
            DependencyProperty.Register("AllowPrint", typeof(bool), typeof(GridControl),
                                        new PropertyMetadata(false));

        public static readonly DependencyProperty AllowFindProperty =
            DependencyProperty.Register("AllowFind", typeof(bool), typeof(GridControl),
                                        new PropertyMetadata(false));

        public static readonly DependencyProperty ToolbarVisibilityProperty =
            DependencyProperty.Register("ToolbarVisibility", typeof(Visibility), typeof(GridControl),
                                        new PropertyMetadata(Visibility.Visible));

        public bool AllowAdd
        {
            get { return (bool)GetValue(AllowAddProperty); }
            set { SetValue(AllowAddProperty, value); }
        }

        public bool AllowDelete
        {
            get { return (bool)GetValue(AllowDeleteProperty); }
            set { SetValue(AllowDeleteProperty, value); }
        }

        public bool AllowEdit {
            get { return (bool)GetValue(AllowEditProperty); }
            set { SetValue(AllowEditProperty, value); }
        }

        public bool AllowPrint
        {
            get { return (bool)GetValue(AllowPrintProperty); }
            set { SetValue(AllowPrintProperty, value); }
        }

        public bool AllowFind
        {
            get { return (bool)GetValue(AllowFindProperty); }
            set { SetValue(AllowFindProperty, value); }
        }

        public Visibility ToolbarVisibility {
            get { return (Visibility)GetValue(ToolbarVisibilityProperty); }
            set { SetValue(ToolbarVisibilityProperty, value); }
        }

        public bool AllowFiltering
        {
            get
            {
                return Grid.IsFilteringAllowed;
            }
            set
            {
                Grid.IsFilteringAllowed = value;
            }
        }

        public String AddButtonText { get; set; }

        public String EditButtonText { get; set; }

        public String DeleteButtonText { get; set; }

        public String FindText { get; set; }

        public String FindLabelText { get; set; }

        public Visibility AddVisibility
        {
            get
            {
                return AllowAdd ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility EditVisibility
        {
            get
            {
                return AllowEdit ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility DeleteVisibility
        {
            get
            {
                return AllowDelete ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        public Visibility PrintVisibility
        {
            get
            {
                return AllowPrint ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility FindVisibility
        {
            get
            {
                return AllowFind ? Visibility.Visible : Visibility.Collapsed;
            }
        }       


        public RadGridView Grid
        {
            get
            {
                return dataGrid;
            }
        }


        public Telerik.Windows.Controls.GridViewColumnCollection Columns
        {
            get
            {
                return Grid.Columns;
            }
        }


        public object SelectedItem
        {
            get
            {
                return dataGrid.SelectedItem;
            }
            set
            {
                dataGrid.SelectedItem = value;
            }
        }


        public object CurrentItem
        {
            get
            {
                return dataGrid.CurrentItem;
            }
            set
            {
                dataGrid.CurrentItem = value;
            }
        }


        public object ItemsSource
        {
            get
            {
                return dataGrid.ItemsSource;
            }
            set
            {
                int rowIndex = GetRowIndex();
                dataGrid.ItemsSource = value;
                SetRowIndex(rowIndex);
            }
        }


        public event EventHandler CreateNewClick;

        public event EventHandler EditClick;

        public event EventHandler DeleteClick;

        public event EventHandler PrintClick;

        public event EventHandler DataRefresh;



        public GridControl()
        {
            InitializeComponent();
            AllowDelete = true;
            AllowAdd = true;
            AllowEdit = true;
            AllowFind = true;
            AddButtonText = "Создать...";
            EditButtonText = "Редактировать";
            DeleteButtonText = "Удалить";            
            FindLabelText = "Поиск:";
        }


        public GridControl(Type itemType) : this() {
            AutoCreateColumns(itemType);
            SetupSecurity(itemType);
            AllowFind = false;
        }

        public virtual void AutoCreateColumns(Type itemType) {
            Columns.Clear();
            if (itemType == null) {
                return;
            }
            PropertyInfo[] properties = ReflectionHelper.GetVisibleProperties(itemType);
            if (properties.Length == 0) {
                throw new InvalidOperationException(string.Format("Для типа {0} не определены свойства для отображения с помощью атрибута DisplayAttribute.", itemType.Name));
            }
            foreach (var property in properties) {
                FieldVisibility vis = ReflectionHelper.GetVisibility(property);
                if (!vis.HasFlag(FieldVisibility.List)) {
                    continue;
                }
                Type type = property.PropertyType;
                if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) {
                    GridViewBoundColumnBase col = CreateColumn(property);
                    Columns.Add(col);
                    if (!type.IsValueType && type != typeof(string)) {
                        col.FilterMemberPath = string.Format("{0}.StringRepresentation", property.Name);
                    }
                }
            }
        }

        public virtual void SetupSecurity(Type itemType) {
            AllowAdd = LiftToTrue(SecurityHelper.IsCreateAllowed(itemType));
            AllowEdit = true;
            AllowDelete = LiftToTrue(SecurityHelper.IsDeleteAllowed(itemType));
        }

        bool LiftToTrue(bool? value) {
            return (value == true || value == null);
        }

        protected virtual GridViewBoundColumnBase CreateColumn(PropertyInfo property) {
            Type type = property.PropertyType;
            var col = new GridViewBoundColumnBase();
            col.Header = ReflectionHelper.GetPropertyName(property);
            col.Width = GridViewLength.Auto;
            col.DataMemberBinding = new Binding(property.Name);
            string format = ReflectionHelper.GetPropertyDisplayFormat(property);
            if (!string.IsNullOrEmpty(format)) {
                col.DataFormatString = format;
            }
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)
                || type == typeof(decimal?) || type == typeof(double?) || type == typeof(float?)) {
                col.TextAlignment = TextAlignment.Right;
            }
            if (type == typeof(int) || type == typeof(uint) || type == typeof(byte) || type == typeof(short) || type == typeof(long) || type == typeof(ulong)
                || type == typeof(int?) || type == typeof(uint?) || type == typeof(byte?) || type == typeof(short?) || type == typeof(long?) || type == typeof(ulong?)) {
                col.TextAlignment = TextAlignment.Right;
            }
            if (type == typeof(DateTime) || type == typeof(DateTime?)) {
                col.TextAlignment = TextAlignment.Center;
                if (string.IsNullOrEmpty(format)) {
                    col.DataFormatString = "dd.MM.yyyy";
                }
            }
            if (type == typeof(bool) || type == typeof(bool?)) {
                col.TextAlignment = TextAlignment.Center;
            }
            SortDirectionAttribute sortAttr = ReflectionHelper.GetSortDirection(property);
            if (sortAttr != null) {
                ColumnSortDescriptor csd = new ColumnSortDescriptor() {
                    Column = col,
                    SortDirection = sortAttr.Direction
                };
                Grid.SortDescriptors.Add(csd);
            }
            return col;
        }

        private int GetRowIndex()
        {
            if (dataGrid.SelectedItem != null)
            {
                return dataGrid.Items.IndexOf(dataGrid.SelectedItem);
            }
            else
            {
                return -1;
            }
        }


        private void SetRowIndex(int index)
        {
            if (index >= 0 && dataGrid.Items.Count > 0)
            {
                if (dataGrid.Items.Count > index)
                {
                    dataGrid.SelectedItem = dataGrid.Items[index];
                }
                else
                {
                    dataGrid.SelectedItem = dataGrid.Items[dataGrid.Items.Count - 1];
                }
            }
        }


        private void btnCreateNew_Click(object sender, RoutedEventArgs e)
        {
            if (CreateNewClick != null)
            {
                try
                {
                    Cursor = Cursors.Wait;
                    CreateNewClick(this, e);
                    if (DataRefresh != null)
                    {
                        DataRefresh(this, e);
                    }
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (EditClick != null)
            {
                if (SelectedItem != null)
                {
                    try
                    {
                        Cursor = Cursors.Wait;
                        EditClick(this, e);
                        if (DataRefresh != null)
                        {
                            Cursor = Cursors.Wait;
                            DataRefresh(this, e);
                        }
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
                else
                {
                    MessageBox.Show("Необходимо выбрать элемент для редактирования.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteClick != null)
            {
                if (SelectedItem != null)
                {
                    try
                    {
                        Cursor = Cursors.Wait;
                        DeleteClick(this, e);
                        if (DataRefresh != null)
                        {
                            Cursor = Cursors.Wait;
                            DataRefresh(this, e);
                        }
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
                else
                {
                    MessageBox.Show("Необходимо выбрать элемент для удаления.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void txbFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindText = txbFind.Text;
            if (DataRefresh != null)
            {
                try
                {
                    Cursor = Cursors.Wait;
                    DataRefresh(this, e);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EditClick != null && AllowEdit)
            {
                if (SelectedItem != null)
                {
                    try
                    {
                        Cursor = Cursors.Wait;
                        EditClick(this, e);
                        if (DataRefresh != null)
                        {
                            Cursor = Cursors.Wait;
                            DataRefresh(this, e);
                        }
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private void dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataRefresh != null)
            {
                DataRefresh(this, e);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                if (PrintClick != null)
                {
                    try
                    {
                        Cursor = Cursors.Wait;
                        PrintClick(this, e);
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
            }
            else
            {
                MessageBox.Show("Необходимо выбрать элемент для печати.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
