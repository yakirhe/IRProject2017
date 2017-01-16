using IRProject.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ShowDict : Window
    {

        #region fields
        MyViewModel vm;
        static int counter = 0;
        Dictionary<string, int> dictionary;
        Dictionary<string, int> termFreqDict;

        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="vm">view model</param>
        public ShowDict(MyViewModel vm)
        {
            InitializeComponent();
            this.vm = vm;
            this.DataContext = vm;
            termFreqDict = vm.VM_TermFreqDict;
            int i = 0;
            using (Stream s = new FileStream("top10.txt", FileMode.Create))
            {
                using (StreamWriter sr = new StreamWriter(s))
                {
                    foreach (string term in termFreqDict.Keys)
                    {
                        if (i < 10)
                        {
                            sr.WriteLine(term);
                            i++;
                        }
                    }
                }
            }
            i = 0;
            List<string> l = termFreqDict.Keys.ToList();
            l.Reverse();
            using (Stream s = new FileStream("top10reverse.txt", FileMode.Create))
            {
                using (StreamWriter sr = new StreamWriter(s))
                {
                    foreach (string term in l)
                    {
                        if (i < 10)
                        {
                            sr.WriteLine(term);
                            i++;
                        }
                    }
                }
            }
            using (Stream s = new FileStream("zipLawV.txt", FileMode.Create))
            {
                using (StreamWriter sr = new StreamWriter(s))
                {

                    foreach (int term in termFreqDict.Values)
                    {
                        sr.WriteLine(term);
                    }
                }
            }
            MessageBox.Show("done");
            MessageBox.Show("There is: " + vm.VM_TermFreqDict.Count.ToString() + " terms\n" + "There is: " + counter.ToString() + " numbers");

        }

        /// <summary>
        /// click on the column
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void results_Click(object sender, RoutedEventArgs e)
        {
            string columnName = ((GridViewColumnHeader)e.OriginalSource).Column.Header.ToString();
            Dictionary<string, int> myCollection = ((Dictionary<string, int>)results.ItemsSource);
            if (columnName == "Frequency in collection")
            {
                if (counter++ % 2 == 0)
                {
                    //ascending
                    myCollection = myCollection.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    //descending
                    myCollection = myCollection.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                }
                vm.VM_TermFreqDict = myCollection;
                results.ItemsSource = vm.VM_TermFreqDict;
                this.UpdateLayout();
                results.UpdateLayout();
            }
        }
    }
}
