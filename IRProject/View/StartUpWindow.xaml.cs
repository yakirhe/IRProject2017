using IRProject.ViewModel;
using Microsoft.Win32;
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
        Dictionary<string, long> autoCompletePointersDict; //contains the auto complete words pointers

        public StartUpWindow()
        {
            InitializeComponent();
            vm = new MyViewModel(new Model.MyModel());
            //load the pointers dictionary from a file
            autoCompletePointersDict = vm.getAutoComPointersDict();
            //load the languages of the docs
            readLang();
        }

        /// <summary>
        /// this function load the languages from a file
        /// </summary>
        private void readLang()
        {
            List<string> listLang = new List<string>();
            using (Stream s = new FileStream(@"Files/Languages.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        //add the language to the list
                        listLang.Add(br.ReadString());
                    }
                }
            }
            //initialize the combo box with the languages list
            langCB.ItemsSource = listLang;
        }

        //go to the indexing window
        private void mainWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow(vm);
            mw.Show();
        }

        /// <summary>
        /// activate the searcher and retreive the relevant queries
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            //check if the query is null
            if (queryTb.Text == "")
            {
                MessageBox.Show("You didn't enter a query to search");
                return;
            }
            string languages = "";
            //get the selected languages
            foreach (var lang in langCB.SelectedItems)
            {
                languages += lang.ToString() + " ";
            }
            resultsLV.ItemsSource = vm.searchQuery(queryTb.Text, languages.Trim());
        }

        /// <summary>
        /// when key is down check if its the spacebar and activate the autocomplete functioon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void queryTb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && oneWord(queryTb.Text.Trim()))
            {
                string word = queryTb.Text.Trim();
                if (word != "" && autoCompletePointersDict.ContainsKey(word))
                {
                    //load the auto complete words for the specific words
                    List<string> autoCompleteWords = vm.autocomplete(autoCompletePointersDict[word]);
                    for (int i = 0; i < autoCompleteWords.Count; i++)
                    {
                        autoCompleteWords[i] = word + " " + autoCompleteWords[i];
                    }
                    //call to a function that display the words in a combobox
                    showAllSuggestion(autoCompleteWords);
                }
            }
        }

        /// <summary>
        /// update the suggestion combo box with the suggested words
        /// </summary>
        /// <param name="autoCompleteWords"></param>
        private void showAllSuggestion(List<string> autoCompleteWords)
        {
            suggestionBox.ItemsSource = autoCompleteWords;
            suggestionBox.Visibility = Visibility.Visible;
            suggestionBox.IsDropDownOpen = true;
        }

        /// <summary>
        /// an helper function to check if there is only one word before the space
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private bool oneWord(string text)
        {
            if (text.Split(' ').Length == 1)
                return true;
            return false;
        }

        /// <summary>
        /// activate when the user select something from the suggestion combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void suggestionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if the combo box was updated to a new suggested options do nothing
            if (suggestionBox.SelectedIndex == -1)
            {
                return;
            }
            //change the text in the query to the selected item in the combo box
            string selectedText = suggestionBox.SelectedItem.ToString();
            queryTb.Text = selectedText;
        }

        /// <summary>
        /// when the load queries is pressed this function activate the function in the view model class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadQueryBtn_Click(object sender, RoutedEventArgs e)
        {
            //open the file dialog window
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                string queriesFile = dialog.FileName;
                MessageBox.Show("Start proccessing the queries");
                //activate the function in the view model
                vm.openQueries(queriesFile);
            }
        }
    }
}
