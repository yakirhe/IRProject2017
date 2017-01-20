using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Forms;

namespace IRProject.Model
{
    /// <summary>
    /// This class reads the documents from the corpus. 
    /// The class gets a path and seperates the files  
    /// </summary>
    class ReadFile : MyModel
    {
        #region fields
        HtmlAgilityPack.HtmlDocument fileDoc;
        private string path;
        private ConcurrentQueue<string> fileNames;//contain all the names of files in the corpus
        private const int WORKING_THREADS = 8, COMP_THREADS = 8;
        private Dictionary<string, string> stopWordsDic = new Dictionary<string, string>();//contain all the stop words from the file that we get from the user
        private Dictionary<string, Dictionary<string, TermInfo>> docsIndex;
        private Dictionary<string, long> finalTermDict;//final posting
        static Mutex m = new Mutex();
        private string corpusFolder;
        private string postingFolder;
        private string stopWordsFileName;
        private MyModel model;
        private bool stemming;
        private Dictionary<string, TermInfo> fileTermsDict;
        private HtmlNode[] docNodes;
        #endregion

        #region propfull
        /// <summary>
        /// get and set for corpus folder path
        /// </summary>
        public string CorpusFolder
        {
            get { return corpusFolder; }
            set { corpusFolder = value; }
        }
        /// <summary>
        /// get and set for posting folder path
        /// </summary>
        public string PostingFolder
        {
            get { return postingFolder; }
            set { postingFolder = value; }
        }

        #endregion

        #region constructors
        /// <summary>
        /// The default constructor
        /// The constructor reads the files and split them into documents and sends them to parser
        /// </summary>
        public ReadFile(bool stemming, string corpusF, string postingF, MyModel model)
        {
            //build with stemming or not
            this.stemming = stemming;
            this.model = model;
            corpusFolder = corpusF;
            postingFolder = postingF;
            //1.get the path
            path = corpusFolder;
            //change the location of the stop words and init the field
            //2.build the list of the stop words
            if (!buildStopWords())
            {
                return;
            }
            //2.1 clear the posting folder
            clearPostingFolder();
            //init the queue parse
            //3.create a queue of the file names
            fileNames = new ConcurrentQueue<string>();
            foreach (string file in Directory.GetFiles(path))
            {
                if (file != path + "\\stopWords.txt")
                {
                    fileNames.Enqueue(file);
                }
            }
            //4.iterate on the queue and create a thread for each one of the file names
            splitDocsThreadPool(fileNames);

        }

        private void clearPostingFolder()
        {
            if (!stemming)
            {
                foreach (string file in Directory.GetFiles(postingFolder))
                {
                    File.Delete(file);
                }
            }
            else
            {
                if (Directory.Exists(postingFolder + "\\Stemming"))
                {
                    foreach (string file in Directory.GetFiles(postingFolder + "\\Stemming"))
                    {
                        File.Delete(file);
                    }
                }
            }
            //check for stemming folder and delete

        }

        /// <summary>
        /// defult constructor
        /// </summary>
        public ReadFile()
        {
        }

        #endregion


        /// <summary>
        /// read the stop words from a file and build a list of stop words from it
        /// </summary>
        private bool buildStopWords()
        {
            //change the location of the stop words file and init the field with new location
            string stopWordsLocation = corpusFolder + "\\stopWords.txt";
            if (!File.Exists(stopWordsLocation)) //the stop words file doesn't exist
            {
                MessageBox.Show("stop words file doesn't exist");
                return false;
            }
            stopWordsFileName = stopWordsLocation;
            //load the file of the stopwords
            string stopWordsFull;
            using (FileStream fs = new FileStream(stopWordsFileName, FileMode.Open))
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
            return true;
        }

        /// <summary>
        /// build dictionary for the stop words
        /// </summary>
        /// <param name="stopWordsArray">array of strings(stop words)</param>
        private void buildDictionary(string[] stopWordsArray)
        {
            for (int i = 0; i < stopWordsArray.Length; i++)
            {
                stopWordsDic[stopWordsArray[i]] = "";
            }
        }

        /// <summary>
        /// Split the work to threads.
        /// Each thread works on a different file
        /// </summary>
        /// <param name="fileNames">queue with file names for seperate</param>
        private void splitDocsThreadPool(ConcurrentQueue<string> fileNames)
        {
            fileDoc = new HtmlAgilityPack.HtmlDocument();
            int totalFiles = fileNames.Count;
            docsIndex = new Dictionary<string, Dictionary<string, TermInfo>>();
            fileTermsDict = new Dictionary<string, TermInfo>();
            //CountdownEvent threadBarrier = new CountdownEvent(fileNames.Count);
            for (int i = 0; i < totalFiles; i++)
            {
                string fileName;
                fileNames.TryDequeue(out fileName);
                //create a thread for processing the file
                // ThreadPool.QueueUserWorkItem((splitDocs) =>
                //{
                splitFile(fileName); //split the file to docs
                                     //  threadBarrier.Signal();
                                     //});
                                     //write the last docs in the indexer
            }
            //threadBarrier.Wait();
            Indexer indexer = new Indexer(model, stemming);
            indexer.merge(postingFolder);
            //get the final doctionary and send notification
            //finalTermDict = indexer.finalTermDict;
            MessageBox.Show("The inverted index is done!");

        }

        /// <summary>
        /// work in a thread.
        /// Split the file to docs. This function is using 
        /// HtmlAgilityPack to help us work with Html tags
        /// </summary>
        /// <param name="fileName">file name</param>
        private void splitFile(string fileName)
        {
            //load the html from the file
            fileDoc.Load(fileName);
            fileTermsDict.Clear();
            //split the file to docs and store them in docNodes
            docNodes = fileDoc.DocumentNode.SelectNodes("//doc").ToArray();
            Parse p = new Parse(stopWordsDic);
            foreach (HtmlNode docNode in docNodes) //iterate on the docs
            {
                //get the doc tag name
                string docNum = docNode.SelectNodes(".//docno")[0].InnerText;
                Console.WriteLine(docNum);
                //get the text from the doc
                string docText = docNode.SelectNodes(".//text").First().InnerText;
                //send to the parser
                //Check if there is an available Parse in the queue

                //use Parser p to parse the doc
                if (!docsIndex.ContainsKey(fileName)) //first time
                {
                    p.parseDoc(docNum, docText, out fileTermsDict, stemming);
                    docsIndex[fileName] = fileTermsDict;
                }
                else
                {
                    docsIndex[fileName] = p.parseDoc(docNum, docText, docsIndex[fileName], stemming);
                }
                //merge the dictionary
                //mergeDicts(fileName, fileTermsDict);
                //enter to the queue the parser that we used
            }
            //Indexer ind = new Indexer();
            //ind.writePostingToFile(docsIndex[fileName], postingFolder, stemming);
            docsIndex[fileName] = null;
        }
    }
}
