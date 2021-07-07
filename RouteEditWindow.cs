using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WpfApp.Core;
using WpfApp.Models;

namespace WpfApp {
    class RouteEditWindow : EditorWindow {
        public RouteEditWindow(DbContext dbContext, DataObjectBase item) : base(dbContext, item) { }

        protected override bool GridAfterNewItemInitialized(PropertyInfo collectionProperty, object createdItem) {
            if (collectionProperty.Name == "RouteCity") {
                (createdItem as RouteCity).Route = model.BusinessObject as Route;
                (createdItem as RouteCity).RouteOrder = (model.BusinessObject as Route).RouteCity.Count + 1;
            }
            if (collectionProperty.Name == "BusRoutes") {
                (createdItem as BusRoute).Route = model.BusinessObject as Route;
            }
            return base.GridAfterNewItemInitialized(collectionProperty, createdItem);
        }

        protected override Control CreateGrid(PropertyInfo property) {
            GridControl grid = base.CreateGrid(property) as GridControl;
            if (property.Name == "BusRoutes") {
                grid.Columns.RemoveAt(0);
            }
            return grid;
        }
    }
}
