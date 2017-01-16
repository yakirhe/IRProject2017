using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IRProject.Model
{
    /// <summary>
    /// this class use to call to function in view model after logical operations
    /// </summary>
    public class MyModel : INotifyPropertyChanged
    {

        #region fields
        //fields
        private Dictionary<string, int> termFreqDict;
        private ConcurrentDictionary<string, int> langDict;
        static ReadFile rf;
        static Indexer indexer;
        private const char SEPERATOR = 'έ';
        private ObservableCollection<string> languages;
        Searcher searcher;
        Ranker ranker;

        #endregion

        /// <summary>
        /// empty constructor
        /// </summary>
        public MyModel()
        {
            ranker = new Ranker();
        }

        /// <summary>
        /// get and set for the language dictionary
        /// </summary>
        public ConcurrentDictionary<string, int> LangDict
        {
            get { return langDict; }
            set
            {
                langDict = value;
                notifyPropertyChanged("LangDict");
            }
        }

        public void searchQuery(string query, string language)
        {
            searcher = new Searcher(query, language, ranker);
        }

        /// <summary>
        /// get and set for the term frequancy dictionary dictionary
        /// </summary>
        public Dictionary<string, int> TermFreqDict
        {
            get { return termFreqDict; }
            set
            {
                termFreqDict = value;
                notifyPropertyChanged("TermFreqDict");
            }
        }

        /// <summary>
        /// get and set for the language dictionary
        /// </summary>
        public ObservableCollection<string> Languages
        {
            get { return languages; }
            set
            {
                languages = value;
                notifyPropertyChanged("Languages");
            }
        }

        /// <summary>
        /// notify property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void notifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }


        public void startIndexing(bool stemming, string corpus, string posting)
        {
            rf = new ReadFile(stemming, corpus, posting, this);
        }

        /// <summary>
        /// this function reset the disk
        /// </summary>
        /// <param name="postingPaths">path to the folder that contain the files to delete</param>
        public void resetIndex(string postingPaths)
        {
            //delete all the posting files
            string[] postingFiles = Directory.GetFiles(postingPaths);
            //reset with stemming
            if (Directory.Exists(postingPaths + "\\Stemming"))
            {
                foreach (string file in Directory.GetFiles(postingPaths + "\\Stemming"))
                {
                    File.Delete(file);
                }
                Directory.Delete(postingPaths + "\\Stemming");
            }
            //without stemming
            foreach (string fileName in postingFiles)
            {
                File.Delete(fileName);
            }
        }

        /// <summary>
        /// this function loaded the dictionary from the disk
        /// </summary>
        /// <param name="postingPath">path to posting in the disk</param>
        /// <param name="stemming">stemming or not</param>
        /// <returns>bool- if its success or not</returns>
        internal bool loadFreqTermDict(string postingPath, bool stemming)
        {
            if (!stemming)
            {
                return loadDictionary(postingPath);
            }
            else
            {
                return loadDictionary(postingPath + "\\Stemming");
            }
        }

        /// <summary>
        /// this function loaded the dictionary from the disk
        /// </summary>
        /// <param name="postingPath">path to postings in the disk</param>
        /// <returns>bool-if its success or not</returns>
        private bool loadDictionary(string postingPath)
        {
            if (!File.Exists(postingPath + "\\termFreqDict.txt"))
            {
                MessageBox.Show("Dictionary file doesn't exist in " + postingPath);
                return false;
            }
            termFreqDict = new Dictionary<string, int>();
            string line = "";
            string term = "";
            int tf = 0;
            using (Stream s = new FileStream(postingPath + "\\termFreqDict.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        line = br.ReadString();
                        term = line.Substring(0, line.IndexOf(SEPERATOR));
                        tf = Int32.Parse(line.Substring(line.IndexOf(SEPERATOR) + 1));
                        termFreqDict[term] = tf;
                    }
                }
            }
            return true;
        }
    }
}
