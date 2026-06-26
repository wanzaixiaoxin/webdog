using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WebDog.Models;
using WebDog.ViewModels;

namespace WebDog
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HistoryItem item)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectHistoryCommand.Execute(item);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveEnvVars();
            }
            base.OnClosing(e);
        }
    }
}
