using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRProjectConsole
{
    class Indexer
    {
        private List<string> filteredTokenes;
        private Dictionary<string, TermInfo> termsDict; //key = term, value = docId-termFr*
        private static Dictionary<string, string> docsDict = new Dictionary<string, string>();
        private static int totalWords = 0;
        private static int numOfDocs = 0;
        private static int numOfTerms = 0;
        private string docId;
        private List<string> sortedKeys;
        private const int MAX_DOCS = 300;
        private static int numOfFiles = 1;
        private ConcurrentQueue<string> filesToMerge;
        private static int mergedCounter = 1;
        private Queue<List<string>> docsTokens;
        private static Mutex m = new Mutex();
        private const char SEPERATOR = 'έ';

        //private static bool firstTime = true;


        public Indexer()
        {
            termsDict = new Dictionary<string, TermInfo>();
            if (!Directory.Exists("Post"))
            {
                Directory.CreateDirectory("Post");
            }
        }

        public Indexer(Dictionary<string, TermInfo> termDict)
        {
            this.termsDict = termDict;
        }

        public Indexer(Queue<List<string>> docsTokens)
        {
            this.docsTokens = docsTokens;
            termsDict = new Dictionary<string, TermInfo>();
            int totalDocs = docsTokens.Count;
            for (int i = 0; i < totalDocs; i++)
            {

            }
        }

        public void buildInvertedIndex(List<string> filteredTokenes, string docId)
        {
            this.docId = docId;
            this.filteredTokenes = filteredTokenes;
            totalWords += filteredTokenes.Count;
            //1.iterate on each token, for each token check if its already exist in the dictionary
            //if not add a new record to the dictionary and update the numm of terms
            //addToInvertedIndex();
            numOfDocs++;
            //check if we need to write to the file
            if (numOfDocs > MAX_DOCS * numOfFiles)
            {
                //writePostingToFile();
            }
        }

        public void writePostingToFile(Dictionary<string, TermInfo> termsDictFile)
        {
            this.termsDict = termsDictFile;
            //sort the dictionary
            sortDictionary();
            m.WaitOne();
            string fileName = "posting" + (numOfFiles).ToString();
            numOfFiles++;
            m.ReleaseMutex();
            writeToFile(fileName);
            //clearDict();
        }

        private void sortDictionary()
        {
            sortedKeys = termsDict.Keys.ToList<string>();
            sortedKeys.Sort();
        }

        private void clearDict()
        {
            termsDict.Clear();
        }

        public void merge()
        {
            //read k files and merge them to 1
            //1. get the total number of post files
            filesToMerge = new ConcurrentQueue<string>(Directory.GetFiles("Post").ToList());
            bool finished = false;
            while (!finished)
            {
                string fileName1 = "", fileName2 = "";
                //check if exist two files in the queue
                //take 2 files from the queue
                for (int i = 0; i < 2; i++)
                {
                    while (!filesToMerge.TryDequeue(out fileName1)) ;
                    while (!filesToMerge.TryDequeue(out fileName2)) ;
                    mergeFiles(fileName1, fileName2);
                }
            }
        }

        private void mergeFiles(string file1, string file2)
        {
            //open a new file to store the merge sort
            string mergedFileName = "Post/postM" + (mergedCounter++) + ".txt";
            Stream mergedStream = new FileStream(mergedFileName, FileMode.Create);
            Stream s1 = new FileStream(file1, FileMode.Open);
            Stream s2 = new FileStream(file2, FileMode.Open);
            BinaryWriter bw = new BinaryWriter(mergedStream);
            BinaryReader br1 = new BinaryReader(s1);
            BinaryReader br2 = new BinaryReader(s2);
            bool increaseFile1 = true, increaseFile2 = true;
            string lineFile1 = "", lineFile2 = "";
            while (br1.BaseStream.Position != br1.BaseStream.Length || br2.BaseStream.Position != br2.BaseStream.Length)
            {
                if (increaseFile1)
                {
                    lineFile1 = br1.ReadString();
                }
                if (increaseFile2)
                {
                    lineFile2 = br2.ReadString();
                }
                //take only the term (the part before the "'" sign)
                string term1 = lineFile1.Substring(0, lineFile1.IndexOf(SEPERATOR));
                string term2 = lineFile2.Substring(0, lineFile2.IndexOf(SEPERATOR));
                //compare the 2 terms
                if (String.Compare(term1, term2) < 0)
                {
                    //term1 is smaller than term2
                    bw.Write(lineFile1);
                    increaseFile1 = true;
                }
                else if (String.Compare(term1, term2) == 0)
                {
                    increaseFile1 = true;
                    increaseFile2 = true;
                    //the terms are equal
                    string post = "";
                    //create a new line 
                    //take the number of Occurrences in files
                    int length1 = lineFile1.IndexOf('*') - lineFile1.IndexOf(SEPERATOR) - 1;
                    int length2 = lineFile2.IndexOf('*') - lineFile2.IndexOf(SEPERATOR) - 1;
                    string numStr1 = lineFile1.Substring(lineFile1.IndexOf(SEPERATOR) + 1, length1);
                    string numStr2 = lineFile2.Substring(lineFile2.IndexOf(SEPERATOR) + 1, length2);
                    int numOfDoc1 = Int32.Parse(numStr1);
                    int numOfDoc2 = Int32.Parse(numStr2);
                    int numberOfOccurrences = numOfDoc1 + numOfDoc2;
                    post = ")" + lineFile2.Substring(lineFile2.IndexOf('*') + 1);
                    lineFile1 = lineFile1.Substring(0, lineFile1.Length - 1);
                    lineFile1 += post;
                    lineFile1 = lineFile1.Substring(0, lineFile1.IndexOf(SEPERATOR) + 1) + numberOfOccurrences.ToString() + lineFile1.Substring(lineFile1.IndexOf('*'));
                    bw.Write(lineFile1);
                }
                else
                {
                    //term2 is smaller than term1
                    bw.Write(lineFile2);
                    increaseFile2 = true;
                }
                if (br1.BaseStream.Position == br1.BaseStream.Length && br2.BaseStream.Position != br2.BaseStream.Length)
                {
                    //br1 finished -> put all the br2 terms in the merged file
                    while (br2.BaseStream.Position != br2.BaseStream.Length)
                    {
                        lineFile2 = br2.ReadString();
                        bw.Write(lineFile2);
                    }
                }
                if (br2.BaseStream.Position == br2.BaseStream.Length && br1.BaseStream.Position != br1.BaseStream.Length)
                {
                    //br2 finished -> put all the br1 terms in the merged file
                    while (br1.BaseStream.Position != br1.BaseStream.Length)
                    {
                        lineFile1 = br1.ReadString();
                        bw.Write(lineFile1);
                    }
                }
            }
            br1.Close();
            br2.Close();
            s1.Close();
            s2.Close();
            bw.Close();
            mergedStream.Close();
            //put the merged file in the queue
            filesToMerge.Enqueue(mergedFileName);
        }

        public Dictionary<string, TermInfo> buildIndexDictionary(List<string> filteredTokenes, string docId)
        {
            this.docId = docId;
            this.filteredTokenes = filteredTokenes;
            totalWords += filteredTokenes.Count;
            //1.iterate on each token, for each token check if its already exist in the dictionary
            //if not add a new record to the dictionary and update the numm of terms
            addToInvertedIndex(termsDict);
            numOfDocs++;
            return termsDict;
        }

        private void writeToFile(string fileName)
        {
            //open the stream
            string fileNameFull = "Post/" + fileName + ".txt";
            using (Stream s = new FileStream(fileNameFull, FileMode.Create))
            {
                using (BinaryWriter sw = new BinaryWriter(s))
                {
                    string lineToWrite = "";
                    foreach (string term in sortedKeys)
                    {
                        lineToWrite = term + SEPERATOR + termsDict[term].ToString() + "\n";
                        //write to disk
                        sw.Write(lineToWrite);
                    }
                }
            }

        }

        private void addToInvertedIndex(Dictionary<string, TermInfo> termsDict)
        {
            int i = 0;
            foreach (string term in filteredTokenes)
            {
                //check each token if it exist in the dictionary
                //if not add it
                if (!termsDict.ContainsKey(term))
                {
                    termsDict[term] = new TermInfo(docId, i);
                }
                else//if it exist we will increase the term frequency by one 
                {
                    termsDict[term].addTermInstance(docId, i);
                }
                i++;
            }
        }
    }
}
