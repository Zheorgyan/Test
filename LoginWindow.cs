using Microsoft.EntityFrameworkCore;
using System;
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
using WpfApp.ViewModels;

namespace WpfApp
{
    /// <summary>
    /// Диалоговое окно авторизации в системе
    /// </summary>
    public partial class LoginWindow : EditorWindow
    {
        public LoginWindow(DbContext dbContext, DataObjectBase model) : base(dbContext, model) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
        }

        RadPasswordBox passwordControl;

        protected override UIElement CreateControl(PropertyInfo property) {
            if (property.Name == "Password") {
                passwordControl = new RadPasswordBox();
                passwordControl.Password = ((LoginViewModel)model.BusinessObject).Password;
                return passwordControl;
            }
            return base.CreateControl(property);
        }

        protected override void OnClosing(CancelEventArgs e) {
            ((LoginViewModel)model.BusinessObject).Password = passwordControl.Password;
            base.OnClosing(e);
        }
    }
}
