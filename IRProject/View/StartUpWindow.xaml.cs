using IRProject.ViewModel;
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

namespace IRProject.View
{
    /// <summary>
    /// Interaction logic for StartUpWindow.xaml
    /// </summary>
    public partial class StartUpWindow : Window
    {
        MyViewModel vm;

        public StartUpWindow()
        {
            InitializeComponent();
            vm = new MyViewModel(new Model.MyModel());
        }

        private void mainWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow(vm);
            mw.Show();
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (queryTb.Text == "")
            {
                MessageBox.Show("You didn't enter a query to search");
                return;
            }
            vm.searchQuery(queryTb.Text,"");
        }
    }
}
