using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace WpfApp.Core {

    /// <summary>
    /// Базовый класс окна редактирования списка
    /// </summary>
    public partial class CollectionEditorWindow : Window {

        protected DbContext dbContext;
        protected Type itemType;
        protected bool isSelectionRequired;

        public CollectionEditorWindow(Type itemType, DbContext dbContext, bool isSelectionRequired = false) {
            InitializeComponent();
            this.dbContext = dbContext;
            this.itemType = itemType;
            this.isSelectionRequired = isSelectionRequired;
            grid.AutoCreateColumns(itemType);
            grid.SetupSecurity(itemType);
            grid.DataRefresh += ((sender, args) => {
                GridDataRefresh();
            });
            grid.CreateNewClick += ((sender, args) => {
                GridCreateNewItem();
            });
            grid.EditClick += ((sender, args) => {
                GridItemEdit();
            });
            grid.DeleteClick += ((sender, args) => {
                GridItemDelete();
            });
            this.Title = GetWindowTitle();
            SetupSecurity();
        }

        protected virtual void SetupSecurity() {
            if (SecurityHelper.IsReadAllowed(itemType) == false) {
                throw new UnauthorizedAccessException(string.Format("Недостаточно прав доступа для просмотра списка объектов '{0}'.", ReflectionHelper.GetTypeName(itemType)));
            }
        }

        protected virtual string GetWindowTitle() {
            return string.Format("{0} - [справочник]", ReflectionHelper.GetTypeName(itemType));
        }

        protected virtual void GridDataRefresh() {            
            grid.ItemsSource = EFHelper.GetObjectCollection(dbContext, itemType).ToList();
        }

        protected virtual void GridCreateNewItem() {
            UIHelper.CreateObject(itemType, this, dbContext, true);
        }

        protected virtual void GridItemEdit() {
            if (grid.SelectedItem != null) {
                UIHelper.EditObject((DataObjectBase)grid.SelectedItem, this, dbContext, true);
            }
        }

        protected virtual void GridItemDelete() {
            if (grid.SelectedItem != null) {
                UIHelper.DeleteObject((DataObjectBase)grid.SelectedItem, dbContext, true);
            }
        }
    }
}
