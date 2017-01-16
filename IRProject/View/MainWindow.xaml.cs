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
using System.Windows.Forms;
using IRProject.ViewModel;
using IRProject.Model;
using IRProject.View;
using System.Windows.Threading;
using System.Threading;

namespace IRProject
{
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields
        MyViewModel vm;
        private string m_postingPath;
        DispatcherTimer t;
        DateTime start;
        private string time;
        #endregion


        #region propfull
        public string Time
        {
            get { return time; }
            set { time = value; }
        }


        public string PostingPath
        {
            get { return m_postingPath; }
            set { m_postingPath = value; }
        }

        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        public MainWindow(MyViewModel vm)
        {
            InitializeComponent();
            this.vm = vm;
            this.DataContext = this;
            time = "1";
            //this.DataContext = vm;
        }

        /// <summary>
        /// open the dialog to choose the folde that contain the corpus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog chooseCorpus = new FolderBrowserDialog();
            if (chooseCorpus.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                corpusText.Text = chooseCorpus.SelectedPath;
            }
        }

        /// <summary>
        /// open the dialog to choose the folde that save the postings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog choosePosting = new FolderBrowserDialog();
            if (choosePosting.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                postingText.Text = choosePosting.SelectedPath;
            }
        }

        /// <summary>
        /// Starts the engine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (postingText.Text == "" || corpusText.Text == "")
            {
                System.Windows.MessageBox.Show("You forgot to enter all the necessary details");
            }
            else
            {
                t = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 50), DispatcherPriority.Background, t_Tick, Dispatcher.CurrentDispatcher);
                t.IsEnabled = true;
                start = DateTime.Now;
                System.Windows.MessageBox.Show("Start indexing");
                m_postingPath = postingText.Text;
                bool stemmingChecked = stemming.IsChecked.HasValue;
                if (stemmingChecked) //this cast is safe  
                {
                    string corpusPath = corpusText.Text;
                    bool stem = stemming.IsChecked.Value;
                    Thread t1 = new Thread(() =>
                    {
                        vm.startIndexing(stem, corpusPath, m_postingPath);
                    });
                    t1.Start();
                    t1.Join();
                    resetBtn.IsEnabled = true;
                    showBtn.IsEnabled = true;
                    langGrid.Visibility = Visibility;
                    language.ItemsSource = vm.VM_LangDict;
                    language.SelectedIndex = 0;
                }
                Thread.Sleep(100);
                t.Stop();
            }

        }

        private void t_Tick(object sender, EventArgs e)
        {
            this.Title = Convert.ToString(DateTime.Now - start);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ReadFile rf = new ReadFile();
            if (Directory.Exists(m_postingPath))
            {
                vm.resetIndex(m_postingPath);
                System.Windows.MessageBox.Show("Deleted all files");
            }
            else
            {
                System.Windows.MessageBox.Show("The posing folder doesn't exist");
            }
        }

        private void showBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowDict showDict = new ShowDict(vm);
            showDict.Show();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (vm.loadTermFreqDict(postingText.Text, stemming.IsChecked.Value))
            {
                showBtn.IsEnabled = true;
            }
        }
    }
}
