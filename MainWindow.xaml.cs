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
using WpfApp.Models;
using WpfApp.Reports;
using WpfApp.ViewModels;

namespace WpfApp {
    
    /// <summary>
    /// Главное окно
    /// </summary>
    public partial class MainWindow : Window {
        MainViewModel Model { get; set; }

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Model = new MainViewModel();
            DataContext = Model;
        }

        /// <summary>
        /// Маршруты
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Route), this);
        }

        /// <summary>
        /// Населенные пункты
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(City), this);
        }

        /// <summary>
        /// Автобусы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Bus), this);
        }

        /// <summary>
        /// Кассиры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Kassir), this);
        }

        /// <summary>
        /// Пассажиры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Passenger), this);
        }

        /// <summary>
        /// Продажа билетов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_5(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Ticket), this);
        }

        /// <summary>
        /// Водители
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_6(object sender, RoutedEventArgs e) {
            UIHelper.EditCollection(typeof(Driver), this);
        }


        /// <summary>
        /// Продажа билетов за период
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadButton_Click(object sender, RoutedEventArgs e) {
            using (var report = new TicketsByPeriod()) {
                if (UIHelper.EditObjectWithoutDbContext(report, this)) {
                    report.ShowPreview();
                }
            }
        }


        /// <summary>
        /// Загрузка рейсов за период
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadButton_Click_1(object sender, RoutedEventArgs e) {
            using (var report = new RoutesByPeriod()) {
                if (UIHelper.EditObjectWithoutDbContext(report, this)) {
                    report.ShowPreview();
                }
            }
        }
    }
}
