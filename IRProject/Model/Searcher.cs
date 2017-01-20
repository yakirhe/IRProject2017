using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.Model
{
    class Searcher
    {
        Ranker ranker;
        string query;
        string language;
        Parse parseQuery;
        Dictionary<string, string> stopWordsDict;
        string stopWordsLocation;

        public Searcher()
        {
            stopWordsLocation = @"..\..\..\stopWords.txt";
            readStopWords();
        }

        private void readStopWords()
        {
            //change the location of the stop words file and init the field with new location
            if (!File.Exists(stopWordsLocation)) //the stop words file doesn't exist
            {
                return;
            }
            //load the file of the stopwords
            string stopWordsFull;
            using (FileStream fs = new FileStream(stopWordsLocation, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    //all the stop words will be in lower case
                    stopWordsFull = sr.ReadToEnd().ToLower();
                }
            }
            //Split the words to the stop word list
            string[] stopWordsArray = stopWordsFull.Split('\n', '\r');
            buildDictionary(stopWordsArray);
        }

        public List<string> searchQuery(string query, string language, Ranker ranker)
        {
            parseQuery = new Parse(stopWordsDict);
            this.query = parseQuery.parseQueryFunc(query);
            this.language = language;
            this.ranker = ranker;
            return getTop50();
        }

        /// <summary>
        /// build dictionary for the stop words
        /// </summary>
        /// <param name="stopWordsArray">array of strings(stop words)</param>
        private void buildDictionary(string[] stopWordsArray)
        {
            stopWordsDict = new Dictionary<string, string>();
            for (int i = 0; i < stopWordsArray.Length; i++)
            {
                stopWordsDict[stopWordsArray[i]] = "";
            }
        }


        /// <summary>
        /// Gets the top 50 most relevant document
        /// </summary>
        private List<string> getTop50()
        {
            return ranker.calculateRelevance(query,language);
        }
    }
}
