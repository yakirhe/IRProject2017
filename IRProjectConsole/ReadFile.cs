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

namespace IRProjectConsole
{
    /// <summary>
    /// This class reads the documents from the corpus. 
    /// The class gets a path and seperates the files
    /// </summary>
    class ReadFile
    {
        private string path;
        private ConcurrentQueue<string> fileNames;
        private const int WORKING_THREADS = 8, COMP_THREADS = 8;
        //private List<string> stopWords = new List<string>(); //list of the stop words
        private Dictionary<string, string> stopWordsDic = new Dictionary<string, string>();
        private ConcurrentQueue<Parse> parseQueue;
        private ConcurrentDictionary<string, Dictionary<string, TermInfo>> docsIndex;
        static Mutex m = new Mutex();
        /// <summary>
        /// The default constructor
        /// The constructor reads the files and split them into documents and sends them to parser
        /// </summary>
        public ReadFile()
        {
            //1.get the path
            //getPath();
            path = @"C:/newCorpus";
            //2.build the list of the stop words
            buildStopWords();
            //
            initQueueParse();
            //3.create a queue of the file names
            fileNames = new ConcurrentQueue<string>();
            foreach (string file in Directory.GetFiles(path))
            {
                fileNames.Enqueue(file);
            }
            //4.iterate on the queue and create a thread for each one of the file names
            splitDocsThreadPool(fileNames);
            #region test
            //test
            //string text;
            //Parse p = new Parse(stopWordsDic);
            //using (FileStream fs = new FileStream("test5.txt", FileMode.Open))
            //{
            //    using (StreamReader sr = new StreamReader(fs))
            //    {
            //        text = sr.ReadToEnd();
            //    }
            //}
            //p.parseDoc("1", text, null);
            #endregion
        }

        private void initQueueParse()
        {
            parseQueue = new ConcurrentQueue<Parse>();
            for (int i = 0; i < WORKING_THREADS; i++)
            {
                Parse p = new Parse(stopWordsDic);
                parseQueue.Enqueue(p);
            }
        }

        /// <summary>
        /// read the stop words from a file and build a list of stop words from it
        /// </summary>
        private void buildStopWords()
        {
            //1.load the file of the stopwords
            string stopWordsFull;
            using (FileStream fs = new FileStream("stopWords.txt", FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    //all the stop words will be in lower case
                    stopWordsFull = sr.ReadToEnd().ToLower();
                }
            }
            //2.Split the words to the stop word list
            string[] stopWordsArray = stopWordsFull.Split('\n', '\r');
            buildDictionary(stopWordsArray);
        }

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
        /// <param name="fileNames"></param>
        private void splitDocsThreadPool(ConcurrentQueue<string> fileNames)
        {
            ThreadPool.SetMaxThreads(WORKING_THREADS, COMP_THREADS);
            int totalFiles = fileNames.Count;
            docsIndex = new ConcurrentDictionary<string, Dictionary<string, TermInfo>>();
            CountdownEvent threadBarrier = new CountdownEvent(fileNames.Count);
            for (int i = 0; i < totalFiles; i++)
            {
                string fileName;
                fileNames.TryDequeue(out fileName);
                //create a thread for processing the file
                ThreadPool.QueueUserWorkItem((splitDocs) =>
                {
                    splitFile(fileName); //split the file to docs
                    threadBarrier.Signal();
                });
                //write the last docs in the indexer
            }
            threadBarrier.Wait();
            Indexer indexer = new Indexer();
            indexer.merge();
        }

        /// <summary>
        /// work in a thread.
        /// Split the file to docs. This function is using 
        /// HtmlAgilityPack to help us work with Html tags
        /// </summary>
        /// <param name="fileName"></param>
        private void splitFile(string fileName)
        {
            HtmlDocument fileDoc = new HtmlDocument();
            Dictionary<string, TermInfo> fileTermsDict = new Dictionary<string, TermInfo>();
            //load the html from the file
            fileDoc.Load(fileName);
            //split the file to docs and store them in docNodes
            HtmlNode[] docNodes = fileDoc.DocumentNode.SelectNodes("//doc").ToArray();
            foreach (HtmlNode docNode in docNodes) //iterate on the docs
            {
                //get the doc tag name
                string docNum = docNode.SelectNodes(".//docno")[0].InnerText;
                Console.WriteLine(docNum);
                //get the text from the doc
                string docText = docNode.SelectNodes(".//text").First().InnerText;
                //send to the parser
                Parse p;
                //Check if there is an available Parse in the queue
                while (!parseQueue.TryDequeue(out p))
                {
                };
                //use Parser p to parse the doc
                if (!docsIndex.ContainsKey(fileName)) //first time
                {
                    p.parseDoc(docNum, docText, out fileTermsDict);
                    docsIndex[fileName] = fileTermsDict;
                }
                else
                {
                    docsIndex[fileName] = p.parseDoc(docNum, docText, docsIndex[fileName]);
                }
                //merge the dictionary
                //mergeDicts(fileName, fileTermsDict);
                //enter to the queue the parser that we used
                parseQueue.Enqueue(p);
            }
            Indexer ind = new Indexer();
            ind.writePostingToFile(docsIndex[fileName]);
            docsIndex[fileName] = null;
        }

        /// <summary>
        /// Get the absulute path of the corpus
        /// </summary>
        private void getPath()
        {
            do
            {
                Console.WriteLine("Enter the files path");
                path = Console.ReadLine();
            } while (!Directory.Exists(path));
        }
    }
}
