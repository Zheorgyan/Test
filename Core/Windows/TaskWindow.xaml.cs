using System;
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
using Telerik.Windows.Controls;

namespace WpfApp.Core {

    /// <summary>
    /// Диалоговое окно выбора опций (вариантов) из списка
    /// </summary>
    public partial class TaskWindow : Window {
        public int SelectedOption { get; private set; } = -1;

        public TaskWindow(string title, string[] options) {
            InitializeComponent();
            SetTitle(title);
            CreateOptions(options);
        }

        protected virtual void SetTitle(string title) {
            Title = title;
        }

        protected virtual void CreateOptions(string[] options) {
            for (int i = 0; i < options.Length; i++) {
                Control button = CreateOptionButton(options[i], i);
                stackPanel.Children.Add(button);
            }
        }

        protected virtual Control CreateOptionButton(string text, int index) {
            RadButton btn = new RadButton();
            btn.Padding = new Thickness(8, 16, 8, 16);
            btn.Margin = new Thickness(2);
            btn.Content = text;
            btn.Tag = index;
            btn.Click += OptionButtonClick;
            return btn;
        }

        private void OptionButtonClick(object sender, RoutedEventArgs e) {
            SelectedOption = (int)(((Control)sender).Tag);
            DialogResult = true;
            Close();
        }

        public static int Show(Window owner, string title, string[] options) {
            TaskWindow dlg = new TaskWindow(title, options);
            dlg.Owner = owner;
            if (dlg.ShowDialog() == true) {
                return dlg.SelectedOption;
            } else {
                return -1;
            }
        }
    }
}
