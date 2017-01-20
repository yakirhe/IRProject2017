using IRProject.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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
        Dictionary<string, long> autoCompletePointersDict;

        public StartUpWindow()
        {
            InitializeComponent();
            vm = new MyViewModel(new Model.MyModel());
            autoCompletePointersDict = vm.getAutoComPointersDict();
            readLang();
        }

        private void readLang()
        {
            List<string> listLang = new List<string>();
            using (Stream s = new FileStream(@"Files/Languages.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        listLang.Add(br.ReadString());
                    }
                }
            }
            langCB.ItemsSource = listLang;
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
            string languages = "";
            foreach (var lang in langCB.SelectedItems)
            {
                languages += lang.ToString()+" ";
            }
            resultsLV.ItemsSource = vm.searchQuery(queryTb.Text, languages.Trim());
        }

        private void queryTb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && oneWord(queryTb.Text.Trim()))
            {
                string word = queryTb.Text.Trim();
                if (word != "" && autoCompletePointersDict.ContainsKey(word))
                {
                    List<string> autoCompleteWords = vm.autocomplete(autoCompletePointersDict[word]);
                    for (int i = 0; i < autoCompleteWords.Count; i++)
                    {
                        autoCompleteWords[i] = word + " " + autoCompleteWords[i];
                    }
                    showAllSuggestion(autoCompleteWords);
                }
            }
        }

        private void showAllSuggestion(List<string> autoCompleteWords)
        {
            suggestionBox.ItemsSource = autoCompleteWords;
            suggestionBox.Visibility = Visibility.Visible;
            suggestionBox.IsDropDownOpen = true;
        }

        private bool oneWord(string text)
        {
            if (text.Split(' ').Length == 1)
                return true;
            return false;
        }

        private void suggestionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suggestionBox.SelectedIndex == -1)
            {
                return;
            }
            string selectedText = suggestionBox.SelectedItem.ToString();
            queryTb.Text = selectedText;
        }
    }
}
