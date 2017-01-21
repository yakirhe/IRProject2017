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
        #region fields
        Ranker ranker; //the ranker object that we use for getting the top 50 
        string query; //the query that the user entered
        int queryNumber; //the query number
        string language; //the language of the requierd results
        Parse parseQuery; //the parser that we use to parse the query
        Dictionary<string, string> stopWordsDict; //the stop words dictionary
        string stopWordsLocation; //the path to the stopwords
        #endregion

        /// <summary>
        /// the default constructor
        /// </summary>
        public Searcher()
        {
            stopWordsLocation = @"..\..\..\stopWords.txt"; //update the path
            readStopWords(); //load stopwords
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
            buildDictionary(stopWordsArray); //build the stopwords dictionary
        }

        /// <summary>
        /// this is the main function in this part of the project
        /// this function gets a query and returns a list of relevant queries
        /// </summary>
        /// <param name="query"></param>
        /// <param name="language"></param>
        /// <param name="ranker"></param>
        /// <param name="queryNumber"></param>
        /// <returns></returns>
        public List<string> searchQuery(string query, string language, Ranker ranker, int queryNumber = 0)
        {
            parseQuery = new Parse(stopWordsDict);
            this.query = parseQuery.parseQueryFunc(query);
            this.language = language;
            this.queryNumber = queryNumber;
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
            return ranker.calculateRelevance(query, language, queryNumber);
        }
    }
}
