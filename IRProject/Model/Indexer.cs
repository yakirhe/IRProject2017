using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRProject.Model
{
    /// <summary>
    /// This class create a postings files and merge them to final posting
    /// </summary>
    class Indexer
    {
        #region class members
        private List<string> filteredTokenes;
        public static Dictionary<string, TermInfo> termsDict = new Dictionary<string, TermInfo>(); //key = term, value = docId-termFr*
        private Dictionary<string, long> m_finalTermDict;//key = term, value = pointer to the posting
        private Dictionary<string, int> termFreqDict; //this is the dictionary that we use for the final display in the GUI
        private Dictionary<string, int> singleDocTermsDict = new Dictionary<string, int>();
        //private static ConcurrentDictionary<string, docInfo> docsDict = new ConcurrentDictionary<string, docInfo>();
        private int totalWords = 0;
        private static int numOfDocs = 0;
        private static int numOfTerms = 0;
        private string docId;
        private List<string> sortedKeys;
        private static int numOfFiles = 1;
        private ConcurrentQueue<string> filesToMerge;
        private static int mergedCounter = 1;
        private static Mutex m = new Mutex();
        private const char SEPERATOR = 'έ';
        private static int mergedFinished = 0;
        //private static ObservableCollection<string> languages = new ObservableCollection<string>();
        string docLang;
        private MyModel model;
        private bool stemming;
        //private static ConcurrentDictionary<string, int> langDict = new ConcurrentDictionary<string, int>();
        //private static Dictionary<string, string> autoCompleteDict = new Dictionary<string, string>();
        private bool firstTime = true;//write the auto complete file to the disk
        private Dictionary<string, long> autoCompletePointers;
        #endregion

        #region propfull
        //public Dictionary<string, long> finalTermDict
        //{
        //    get { return m_finalTermDict; }
        //    set { m_finalTermDict = value; }
        //}

        //public ConcurrentDictionary<string, int> LangDict
        //{
        //    get { return langDict; }
        //    set { langDict = value; }
        //}

        //public ObservableCollection<string> Languages
        //{
        //    get { return languages; }
        //    set { languages = value; }
        //}

        //public Dictionary<string, int> TermFreqDict
        //{
        //    get { return termFreqDict; }
        //    set { termFreqDict = value; }
        //}

        #endregion

        /// <summary>
        /// this constructor get model and bool stemming and init the field
        /// </summary>
        /// <param name="model">model</param>
        /// <param name="stemming">stemming or not</param>
        public Indexer(MyModel model, bool stemming)
        {
            this.stemming = stemming;
            //termsDict = new Dictionary<string, TermInfo>();
            this.model = model;
        }

        /// <summary>
        /// this consructor
        /// </summary>
        /// <param name="termDict">info about the terms</param>
        public Indexer(Dictionary<string, TermInfo> termDict)
        {
            //this.termsDict = termDict;
        }

        /// <summary>
        /// this constructor init the term dictionary
        /// </summary>
        public Indexer()
        {
            //termsDict = new Dictionary<string, TermInfo>();
        }


        /// <summary>
        /// this function get the info about the terms, path to write the posting in the disk 
        /// and if we work with stemming or not and organize this details and after this
        /// call to function that write the postings to the diak
        /// </summary>
        /// <param name="termsDictFile">info about the terms</param>
        /// <param name="postingFolder">path to write the posting</param>
        /// <param name="stemming">with stemming or not</param>
        public void writePostingToFile(Dictionary<string, TermInfo> termsDictFile, string postingFolder, bool stemming)
        {
            string fileName = "";
            //this.termsDict = termsDictFile;
            //sort the dictionary
            sortDictionary();
            m.WaitOne();
            if (!stemming)
            {
                fileName = "posting" + (numOfFiles).ToString();
                numOfFiles++;
                m.ReleaseMutex();
                writeToFile(fileName, postingFolder);
            }
            else
            {
                Directory.CreateDirectory(postingFolder + "\\Stemming");
                fileName = "posting" + (numOfFiles).ToString();
                numOfFiles++;
                m.ReleaseMutex();
                writeToFile(fileName, postingFolder + "\\Stemming");
            }
        }


        /// <summary>
        /// this function sort the dictionary
        /// </summary>
        private void sortDictionary()
        {
            //sortedKeys = termsDict.Keys.ToList<string>();
            sortedKeys.Sort();
        }

        /// <summary>
        /// this function check if we able to access to this file
        /// </summary>
        /// <param name="file">name of the file</param>
        /// <returns>bool- allow to access or not</returns>
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


        /// <summary>
        /// this function iterate on the posting files and merge them to final posting
        /// </summary>
        /// <param name="postingFolder">path to read the posting files</param>
        public void merge(string postingFolder)
        {
            //if (firstTime)
            //{
            //    firstTime = false;
            //    writeAutoComToDisk();
            //    writeAutoComPointerToDisk();
            //}
            //read k files and merge them to 1
            ////1. get the total number of post files
            if (!stemming)
            {
                filesToMerge = new ConcurrentQueue<string>(Directory.GetFiles(postingFolder).ToList());
            }
            else
            {
                filesToMerge = new ConcurrentQueue<string>(Directory.GetFiles(postingFolder + "\\Stemming").ToList());
            }
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
                    mergeFiles(fileName1, fileName2, counter, false, postingFolder);
                    mergedFinished++;
                    barrier.Signal();
                });
            }
            barrier.Wait();
            //perform the last merge and build the final terms dictionary
            termFreqDict = new Dictionary<string, int>();
            string fileNameStr1 = "", fileNameStr2 = "";
            filesToMerge.TryDequeue(out fileNameStr1);
            filesToMerge.TryDequeue(out fileNameStr2);
            mergeFiles(fileNameStr1, fileNameStr2, 0, true, postingFolder);
            model.TermFreqDict = termFreqDict;
            //model.LangDict = langDict;
            //buildLanguagesCollection();
            //model.Languages = languages;
            //write the dictionary to file
            writeDictionariesToFile(postingFolder);
            numOfFiles = 1;
            //write the doc dictionary to file
            //writeDocsDictToFile(postingFolder);
        }

        private void writeAutoComPointerToDisk()
        {
            using (Stream s = new FileStream("autoCompletePointers.txt", FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(s))
                {
                    foreach (string term in autoCompletePointers.Keys)
                    {
                        bw.Write(term);
                        bw.Write(autoCompletePointers[term]);
                    }
                }
            }
        }

      //  private void writeAutoComToDisk()
      //  {
      //      autoCompletePointers = new Dictionary<string, long>();
      //      int num = 0;
      //      using (Stream s = new FileStream("autoComplete.txt", FileMode.Create))
      //      {
      //          using (BinaryWriter bw = new BinaryWriter(s))
      //          {
      //              foreach (string term in autoCompleteDict.Keys)
      //              {
      //                  List<string> list = autoCompleteDict[term].Split(' ').ToList();
      //                  list = fiveMostCommon(list);
      //                  autoCompletePointers[term] = bw.BaseStream.Position;
      //                  if (list == null)
      //                  {
      //                      num = 0;
      //                  }
      //                  if (list.Count < 5)
      //                  {
      //                      num = list.Count;
      //                  }
      //                  else
      //                  {
      //                      num = 5;
      //                  }
      //                  bw.Write(term);
      //                  bw.Write(num);
      //                  for (int i = 0; i < num; i++)
      //                  {
      //                      bw.Write(list[i]);
      //                  }
      //              }
      //          }
      //      }
      //  }

      //  private List<string> fiveMostCommon(List<string> list)
      //  {
      //      var most = list.GroupBy(i => i).OrderByDescending(grp => grp.Count())
      //.Select(grp => grp.Key);
      //      return most.ToList();


      //  }

        //private void writeDocsDictToFile(string postingFolder)
        //{
        //    using (Stream s = new FileStream(postingFolder + "\\DocsDict.txt", FileMode.Create))
        //    {
        //        using (BinaryWriter br = new BinaryWriter(s))
        //        {
        //            foreach (string docId in docsDict.Keys)
        //            {
        //                br.Write(docId);
        //                br.Write(docsDict[docId].ToString());
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// this function add to the language dictionary 
        /// </summary>
        //private void buildLanguagesCollection()
        //{
        //    foreach (string lang in LangDict.Keys)
        //    {
        //        languages.Add(lang);
        //    }
        //}

        /// <summary>
        /// this function write the dictionary to the disk
        /// </summary>
        /// <param name="postingFolder">path to write the dictionary</param>
        private void writeDictionariesToFile(string postingFolder)
        {
            if (stemming)
            {
                postingFolder += "\\Stemming";
            }
            writeTermFreqDictToFile(postingFolder);
            writeFinalTermsDictToFile(postingFolder);
        }

        /// <summary>
        /// this function write the dictionary to the disk
        /// </summary>
        /// <param name="postingFolder">path to write the dictionary</param>
        private void writeFinalTermsDictToFile(string postingFolder)
        {
            using (Stream s = new FileStream(postingFolder + "//finalDict.txt", FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(s))
                {
                    foreach (string term in m_finalTermDict.Keys)
                    {

                        bw.Write(term);
                        bw.Write(m_finalTermDict[term].ToString());
                    }
                }
            }
        }

        /// <summary>
        /// this function write the dictionary to the disk
        /// </summary>
        /// <param name="postingFolder">path to write the dictionary</param>
        private void writeTermFreqDictToFile(string postingFolder)
        {
            using (Stream s = new FileStream(postingFolder + "//termFreqDict.txt", FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(s))
                {
                    foreach (string term in termFreqDict.Keys)
                    {
                        string lineToWrite = term + SEPERATOR + termFreqDict[term].ToString();
                        bw.Write(lineToWrite);
                    }
                }
            }
        }

        /// <summary>
        /// this function iterate on the posting files and merge them to final posting
        /// </summary>
        /// <param name="file1">file 1 to merge</param>
        /// <param name="file2">file 2 to merge</param>
        /// <param name="counter">counter</param>
        /// <param name="finalMerge">bool final merge</param>
        /// <param name="postingFolder">path to read and write</param>
        private void mergeFiles(string file1, string file2, int counter, bool finalMerge, string postingFolder)
        {
            if (stemming)
            {
                postingFolder += "\\Stemming";
            }
            string mergedFileName = "";
            //open a new file to store the merge sort
            if (finalMerge)
            {
                //m_finalTermDict = new Dictionary<string, long>();
                mergedFileName = postingFolder + "/finalPosting.txt";
            }
            else
            {
                mergedFileName = postingFolder + "/postM" + counter + ".txt";
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
                        //m_finalTermDict[term1] = bw.BaseStream.Position;
                        //termFreqDict[term1] = getTf(lineFile1);
                    }
                    bw.Write(lineFile1);
                    increaseFile1 = true;
                    increaseFile2 = false;
                }
                else if (String.Compare(term1, term2) == 0)
                {
                    if (finalMerge)
                    {
                        //m_finalTermDict[term1] = bw.BaseStream.Position;
                        //termFreqDict[term1] = getTf(lineFile1, lineFile2);
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
                        m_finalTermDict[term2] = bw.BaseStream.Position;
                        termFreqDict[term2] = getTf(lineFile2);
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
                            m_finalTermDict[term2] = bw.BaseStream.Position;
                            termFreqDict[term2] = getTf(lineFile2);
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
                            m_finalTermDict[term1] = bw.BaseStream.Position;
                            termFreqDict[term1] = getTf(lineFile1);
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
        /// this function take the tf from spesipic line
        /// </summary>
        /// <param name="line">line that represent term info</param>
        /// <returns>the tf</returns>
        private int getTf(string line)
        {
            int length = line.IndexOf('*') - line.IndexOf(SEPERATOR) - 1;
            int docsInstances = Int32.Parse(line.Substring(line.IndexOf(SEPERATOR) + 1, length));
            string docsStr = line.Substring(line.IndexOf('|') + 1);
            int termTf = 0;
            for (int i = 0; i < docsInstances; i++)
            {
                termTf += Int32.Parse(docsStr.Substring(0, docsStr.IndexOf('-')));
                if (docsStr.IndexOf('|') > 0)
                    docsStr = docsStr.Substring(docsStr.IndexOf('|') + 1);
            }
            return termTf;
        }

        /// <summary>
        /// this function use the function getTf(string) and summarize the df's of equal terms
        /// </summary>
        /// <param name="line1">line that represent term info</param>
        /// <param name="line2">line that represent term info</param>
        /// <returns>the tf</returns>
        private int getTf(string line1, string line2)
        {
            return getTf(line1) + getTf(line2);
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
        public Dictionary<string, TermInfo> buildIndexDictionary(List<string> filteredTokenes, string docId, string docLang, bool stemming)
        {
            //singleDocTermsDict = new Dictionary<string, int>();
            this.stemming = stemming;
            this.docId = docId;
            this.docLang = docLang;
            this.filteredTokenes = filteredTokenes;
            totalWords += filteredTokenes.Count;
            //1.iterate on each token, for each token check if its already exist in the dictionary
            //if not add a new record to the dictionary and update the numm of terms
            addToInvertedIndex(termsDict);
            numOfDocs++;
            //checkLangDict(docLang);
            //addToDocsDict();
            //writeDocsTermsToFile(docId);
            return termsDict;
        }

        private void writeDocsTermsToFile(string docId)
        {
            //Directory.CreateDirectory("docsTerms");
            //using (Stream s = new FileStream(@"docsTerms/" + docId + ".txt", FileMode.Create))
            //{
            //    using (BinaryWriter bw = new BinaryWriter(s))
            //    {
            //        foreach (string term in singleDocTermsDict.Keys)
            //        {
            //            bw.Write(term);
            //            bw.Write(singleDocTermsDict[term]);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Checks if the language is in the language dictionary
        /// if not add it to the dictionary
        /// </summary>
        //private void checkLangDict(string lang)
        //{
        //    if (lang != "")
        //    {
        //        //check if we don't have it in the dictionary
        //        if (!langDict.ContainsKey(lang))
        //        {
        //            langDict[lang] = 1;
        //        }
        //        else //already in the dictionary
        //        {
        //            langDict[lang]++;
        //        }
        //    }
        //}

        /// <summary>
        /// Add the most frequent term and language of each doc
        /// to the docs dictionary
        /// </summary>
        //private void addToDocsDict()
        //{
            //int max = 0;
            //string maxTerm = "";
            //foreach (string term in singleDocTermsDict.Keys)
            //{
            //    if (singleDocTermsDict[term] > max)
            //    {
            //        maxTerm = term;
            //        max = singleDocTermsDict[term];
            //    }
            //}
            //docInfo dInfo = new docInfo(maxTerm, max, docLang, singleDocTermsDict.Count, filteredTokenes.Count);
            //docsDict[docId] = dInfo;
        //}

        private void writeToFile(string fileName, string postingFolder)
        {
            //open the stream
            string fileNameFull = postingFolder + "/" + fileName + ".txt";
            using (Stream s = new FileStream(fileNameFull, FileMode.Create))
            {
                using (BinaryWriter sw = new BinaryWriter(s))
                {
                    string lineToWrite = "";
                    foreach (string term in sortedKeys)
                    {
                        //lineToWrite = term + SEPERATOR + termsDict[term].ToString() + "\n";
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
                //build the auto complete dictionary
                //if (i + 1 < filteredTokenes.Count)
                //{
                //    if (!autoCompleteDict.ContainsKey(term))
                //    {
                //        autoCompleteDict[term] = filteredTokenes[i + 1] + " ";
                //    }
                //    else
                //    {
                //        autoCompleteDict[term] += filteredTokenes[i + 1] + " ";
                //    }
                //}
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
