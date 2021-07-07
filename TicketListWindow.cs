using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Core;
using WpfApp.Models;

namespace WpfApp {
    class TicketListWindow : CollectionEditorWindow {
        public TicketListWindow(Type itemType, DbContext dbContext, bool isSelectionRequired = false) 
            : base(itemType, dbContext, isSelectionRequired) {
            grid.AddButtonText = "Продажа билета...";
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        protected override void GridDataRefresh() {
            grid.ItemsSource = EFHelper.GetObjectCollection(dbContext, itemType).Cast<Ticket>().OrderByDescending(t => t.Id).ToList();
        }
    }
}
