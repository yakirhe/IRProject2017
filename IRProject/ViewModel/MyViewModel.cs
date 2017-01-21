using IRProject.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.ViewModel
{
    /// <summary>
    /// this class connection the view with the model
    /// </summary>
    public class MyViewModel : INotifyPropertyChanged
    {

        #region fields
        MyModel model;
        private Dictionary<string, int> termFreqDict;
        private ConcurrentDictionary<string, int> langDict;
        #endregion

        /// <summary>
        /// this constructor init the property changed event
        /// </summary>
        /// <param name="model">model</param>
        public MyViewModel(MyModel model)
        {
            this.model = model;
            model.PropertyChanged += delegate (Object sender, PropertyChangedEventArgs e)
            {
                notifyPropertyChanged("VM_" + e.PropertyName);
            };
        }

        public Dictionary<string, long> getAutoComPointersDict()
        {
            return model.getAutoComPointersDict();
        }

        /// <summary>
        /// send to the model the quert that we want ro search
        /// </summary>
        /// <param name="text"></param>
        /// <param name="v"></param>
        public List<string> searchQuery(string query, string language)
        {
            return model.searchQuery(query, language);
        }

        /// <summary>
        /// get and set term freq dictionary
        /// </summary>
        public Dictionary<string, int> VM_TermFreqDict
        {
            get { return model.TermFreqDict; }
            set
            {
                termFreqDict = value;
                notifyPropertyChanged("VM_TermFreqDict");
            }
        }

        public List<string> autocomplete(long termPointer)
        {
            return model.autoComplete(termPointer);
        }

        /// <summary>
        /// get and set language dictionary
        /// </summary>
        public ConcurrentDictionary<string, int> VM_LangDict
        {
            get { return model.LangDict; }
            set
            {
                langDict = value;
            }
        }

        private ObservableCollection<string> languages;

        public ObservableCollection<string> VM_Languages
        {
            get { return model.Languages; }
            set { languages = value; }
        }

        /// <summary>
        /// property changed handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void notifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        /// <summary>
        /// start inexing
        /// </summary>
        /// <param name="stemming">stemming</param>
        /// <param name="corpus">path to corpus</param>
        /// <param name="posting">path to posting folder</param>
        internal void startIndexing(bool stemming, string corpus, string posting)
        {
            model.startIndexing(stemming, corpus, posting);
        }

        /// <summary>
        /// call to function in the model layer to reset
        /// </summary>
        /// <param name="postingPath">path to posting folder</param>
        internal void resetIndex(string postingPath)
        {
            model.resetIndex(postingPath);
        }

        internal void openQueries(string queriesFile)
        {
            model.openQueries(queriesFile);
        }

        /// <summary>
        /// call to function in the mmodel that load the dictionary to the RAM
        /// </summary>
        /// <param name="postingPath">path to posting folder</param>
        /// <param name="stemming">stemming</param>
        /// <returns>bool</returns>
        internal bool loadTermFreqDict(string postingPath, bool stemming)
        {
            return model.loadFreqTermDict(postingPath, stemming);
        }
    }
}
