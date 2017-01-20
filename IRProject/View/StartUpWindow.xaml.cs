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
        static bool firstTime = true;
        MyViewModel vm;
        Dictionary<string, long> autoCompletePointersDict;

        public StartUpWindow()
        {
            InitializeComponent();
            vm = new MyViewModel(new Model.MyModel());
            //autoCompletePointersDict = vm.getAutoComPointersDict();
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
            vm.searchQuery(queryTb.Text, "");
        }

        private void queryTb_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Space && firstTime)
            {
                string word = queryTb.Text.Trim();
                if (word != "" && autoCompletePointersDict.ContainsKey(word))
                {
                    firstTime = false;
                    List<string> autoCompleteWords = vm.autocomplete(autoCompletePointersDict[word]);
                    MessageBox.Show(autoCompleteWords[0]);
                }
            }
        }
    }
}
