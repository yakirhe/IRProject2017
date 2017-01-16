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
        #region class members
        private List<string> filteredTokenes;
        private Dictionary<string, TermInfo> termsDict; //key = term, value = docId-termFr*
        private Dictionary<string, long> finalTermsDict; //key = term, value = pointer to the posting
        private Dictionary<string, int> singleDocTermsDict;
        private static Dictionary<string, docInfo> docsDict = new Dictionary<string, docInfo>();
        private int totalWords = 0;
        private static int numOfDocs = 0;
        private static int numOfTerms = 0;
        private string docId;
        private List<string> sortedKeys;
        private const int MAX_DOCS = 300;
        private static int numOfFiles = 1;
        private ConcurrentQueue<string> filesToMerge;
        private static int mergedCounter = 1;
        private static Mutex m = new Mutex();
        private const char SEPERATOR = 'έ';
        private static int mergedFinished = 0;
        string docLang;
        #endregion

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

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        public void merge()
        {
            //read k files and merge them to 1
            //1. get the total number of post files
            filesToMerge = new ConcurrentQueue<string>(Directory.GetFiles("Post").ToList());
            int filesCount = filesToMerge.Count;
            bool finished = false;
            CountdownEvent barrier = new CountdownEvent(filesCount - 2);
            for (int i = 0; i < filesCount - 2; i++)
            {
                string fileName1 = "", fileName2 = "";
                //check if exist two files in the queue
                //take 2 files from the queue
                ThreadPool.SetMaxThreads(8, 8);
                while (!filesToMerge.TryDequeue(out fileName1)) ;
                while (!filesToMerge.TryDequeue(out fileName2)) ;
                int counter = mergedCounter++;
                ThreadPool.QueueUserWorkItem((merge) =>
                {
                    mergeFiles(fileName1, fileName2, counter, false);
                    mergedFinished++;
                    barrier.Signal();
                });
            }
            barrier.Wait();
            //perform the last merge and build the final terms dictionary
            string fileNameStr1 = "", fileNameStr2 = "";
            filesToMerge.TryDequeue(out fileNameStr1);
            filesToMerge.TryDequeue(out fileNameStr2);
            mergeFiles(fileNameStr1, fileNameStr2, 0, true);
        }

        private void mergeFiles(string file1, string file2, int counter, bool finalMerge)
        {
            string mergedFileName = "";
            //open a new file to store the merge sort
            if (finalMerge)
            {
                finalTermsDict = new Dictionary<string, long>();
                mergedFileName = "Post/finalPosting.txt";
            }
            else
            {
                mergedFileName = "Post/postM" + counter + ".txt";
            }
            Stream mergedStream = new FileStream(mergedFileName, FileMode.Create);
            FileInfo fileInfo1 = new FileInfo(file1);
            FileInfo fileInfo2 = new FileInfo(file2);
            //check if no other thread writing to the file
            while (IsFileLocked(fileInfo1)) ;
            Stream s1 = new FileStream(file1, FileMode.Open);
            while (IsFileLocked(fileInfo2)) ;
            Stream s2 = new FileStream(file2, FileMode.Open);
            BinaryWriter bw = new BinaryWriter(mergedStream);
            BinaryReader br1 = new BinaryReader(s1);
            BinaryReader br2 = new BinaryReader(s2);
            string lineFile1 = "", lineFile2 = "";
            bool increaseFile1 = true, increaseFile2 = true;
            while (br1.BaseStream.Position != br1.BaseStream.Length || br2.BaseStream.Position != br2.BaseStream.Length)
            {
                if (increaseFile1)
                    lineFile1 = br1.ReadString();
                if (increaseFile2)
                    lineFile2 = br2.ReadString();
                //take only the term (the part before the "'" sign)
                string term1 = lineFile1.Substring(0, lineFile1.IndexOf(SEPERATOR));
                string term2 = lineFile2.Substring(0, lineFile2.IndexOf(SEPERATOR));
                //compare the 2 terms
                if (String.Compare(term1, term2) < 0)
                {
                    //term1 is smaller than term2
                    if (finalMerge)
                    {
                        finalTermsDict[term1] = bw.BaseStream.Position;
                    }
                    bw.Write(lineFile1);
                    increaseFile1 = true;
                    increaseFile2 = false;
                }
                else if (String.Compare(term1, term2) == 0)
                {
                    if (finalMerge)
                    {
                        finalTermsDict[term1] = bw.BaseStream.Position;
                    }
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
                    if (finalMerge)
                    {
                        finalTermsDict[term2] = bw.BaseStream.Position;
                    }
                    bw.Write(lineFile2);
                    increaseFile2 = true;
                    increaseFile1 = false;
                }
                if (br1.BaseStream.Position == br1.BaseStream.Length && br2.BaseStream.Position != br2.BaseStream.Length)
                {
                    //br1 finished -> put all the br2 terms in the merged file
                    while (br2.BaseStream.Position != br2.BaseStream.Length)
                    {
                        lineFile2 = br2.ReadString();
                        term2 = lineFile2.Substring(0, lineFile2.IndexOf(SEPERATOR));
                        if (finalMerge)
                        {
                            finalTermsDict[term2] = bw.BaseStream.Position;
                        }
                        bw.Write(lineFile2);
                    }
                }
                if (br2.BaseStream.Position == br2.BaseStream.Length && br1.BaseStream.Position != br1.BaseStream.Length)
                {
                    //br2 finished -> put all the br1 terms in the merged file
                    while (br1.BaseStream.Position != br1.BaseStream.Length)
                    {
                        lineFile1 = br1.ReadString();
                        term1 = lineFile1.Substring(0, lineFile1.IndexOf(SEPERATOR));
                        if (finalMerge)
                        {
                            finalTermsDict[term2] = bw.BaseStream.Position;
                        }
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
            //delete the 2 merged files
            deleteMergedFiles(file1, file2);
        }

        /// <summary>
        /// Delete the files that already been used
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        private void deleteMergedFiles(string file1, string file2)
        {
            File.Delete(file1);
            File.Delete(file2);
        }


        /// <summary>
        /// This function is called from the Parse for each doc we finish to proccess
        /// </summary>
        /// <param name="filteredTokenes"></param>
        /// <param name="docId"></param>
        /// <returns></returns>
        public Dictionary<string, TermInfo> buildIndexDictionary(List<string> filteredTokenes, string docId, string docLang)
        {
            singleDocTermsDict = new Dictionary<string, int>();
            this.docId = docId;
            this.docLang = docLang;
            this.filteredTokenes = filteredTokenes;
            totalWords += filteredTokenes.Count;
            //1.iterate on each token, for each token check if its already exist in the dictionary
            //if not add a new record to the dictionary and update the numm of terms
            addToInvertedIndex(termsDict);
            numOfDocs++;
            addToDocsDict();
            return termsDict;
        }

        /// <summary>
        /// Add the most frequent term and language of each doc
        /// to the docs dictionary
        /// </summary>
        private void addToDocsDict()
        {
            int max = 0;
            string maxTerm = "";
            foreach (string term in singleDocTermsDict.Keys)
            {
                if (singleDocTermsDict[term] > max)
                {
                    maxTerm = term;
                    max = singleDocTermsDict[term];
                }
            }
            docsDict[docId] = new docInfo(maxTerm, max, this.docLang, singleDocTermsDict.Count);
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

        /// <summary>
        /// check each term of the doc if he already exist in the dictionary and update his posting list
        /// if he is not in the dictionary add him to the dictionary
        /// </summary>
        /// <param name="termsDict"></param>
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
                if (!singleDocTermsDict.ContainsKey(term))
                {
                    singleDocTermsDict[term] = 1;
                }
                else
                {
                    singleDocTermsDict[term]++;
                }
                i++;
            }
        }
    }
}
