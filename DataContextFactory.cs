using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Core;
using WpfApp.Models;

namespace WpfApp {
    class DataContextFactory : IDataContextFactory {
        public DbContext CreateDbContext() {
            return new DataContext();
        }
    }
}
