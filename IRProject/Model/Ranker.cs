using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nml;

namespace IRProject.Model
{
    class Ranker
    {
        private const double k1 = 1.2;
        private const double b = 0.75;
        private Dictionary<string, docInfo> docsInfoDict;//key=docId, value = docInfo
        private Dictionary<string, long> termPointerDict;//key=term, value = pointer
        private Dictionary<string, double> docsRating;//key=docId,value=rating
        private Dictionary<string, TermInfo> queryTermInfoDict; //key=query word, value = TermInfo
        List<string> relevantDocs = new List<string>();//contain the relevant docs
        private double avgDocLength;
        private const double R = 0;
        private const double r = 0;
        private const double k2 = 100;
        private const char SEPERATOR = 'έ';
        private int df, tf;
        private int n;

        public Ranker()
        {
            docsInfoDict = new Dictionary<string, docInfo>();
            //read the docs info dictionary drom the disk
            readDocInfo();
            readTermPointerDict();
        }

        private void buildQueryTermInfoDict(string query)
        {
            queryTermInfoDict = new Dictionary<string, TermInfo>();
            using (Stream s = new FileStream("Files/finalPosting.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    foreach (string word in query.Split(' '))
                    {
                        if (!termPointerDict.ContainsKey(word))
                        {
                            continue;
                        }
                        long positionToRead = termPointerDict[word];
                        br.BaseStream.Seek(positionToRead, SeekOrigin.Begin);
                        //get the term info
                        string termInfoStr = br.ReadString();
                        termInfoStr = termInfoStr.Substring(termInfoStr.IndexOf(SEPERATOR));
                        TermInfo termInfo = new TermInfo(termInfoStr);
                        queryTermInfoDict[word] = termInfo;
                    }
                }
            }
        }

        private void readTermPointerDict()
        {
            termPointerDict = new Dictionary<string, long>();
            using (Stream s = new FileStream("Files/finalDict.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        string term = br.ReadString();
                        string pointer = br.ReadString();
                        termPointerDict[term] = long.Parse(pointer);
                    }
                }
            }
        }

        /// <summary>
        /// read the docs info dictionary from the disk
        /// </summary>
        private void readDocInfo()
        {
            int docLengthSum = 0;
            using (Stream s = new FileStream("Files//DocsDict.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        string docId = br.ReadString();
                        string docInfo = br.ReadString();
                        docsInfoDict[docId] = new docInfo(docInfo);
                        docLengthSum += docsInfoDict[docId].DocLength;
                    }
                }
            }
            avgDocLength = docLengthSum / docsInfoDict.Count;
        }

        /// <summary>
        /// gets called from the Searcher class and calculate for each document it's BM25 and LSI
        /// </summary>
        /// <param name="query"></param>
        public void calculateRelevance(string query)
        {
            docsRating = new Dictionary<string, double>();
            buildQueryTermInfoDict(query);
            createUnionListOfRelDocs();
            foreach (string docId in relevantDocs)
            {
                docsRating[docId] = calculateBM25(query, docId);
            }
            //string expandedQuery = expandQuery(query);
        }

        /// <summary>
        /// This function expanding the query for semantic evaluation
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        // private string expandQuery(string query)
        //{

        //}

        /// <summary>
        /// create union list of all relevents docs for the query 
        /// </summary>
        private void createUnionListOfRelDocs()
        {
            List<string> unionDocs = new List<string>();
            foreach (string word in queryTermInfoDict.Keys)
            {
                relevantDocs = (relevantDocs.Union(queryTermInfoDict[word].DocTermsDict.Keys.ToList())).ToList();
            }
        }

        public double calculateBM25(string query, string docNum)
        {
            //get the doc length
            int docLength = docsInfoDict.Count;
            double k = calculateK(docLength);
            double sum = 0;
            foreach (string word in query.Split(' '))
            {
                int wordFreqInQuery = wordFreqInQueryFunc(word, query);
                getTermFreqDoc(docNum, word);
                double numerator = ((r + 0.5) / (R - r + 0.5)) * (k1 + 1) * tf * (k2 + 1) * wordFreqInQuery;
                double denominator = (df - r + 0.5) / (docsInfoDict.Count - n - R + r + 0.5) * (k + tf) * (k2 + wordFreqInQuery);
                if (numerator == 0)
                {
                    continue;
                }
                else
                {
                    sum += Math.Log(numerator / denominator);
                }
            }
            return sum;
        }

        private int wordFreqInQueryFunc(string word, string query)
        {
            int termFreqQuery = 0;
            foreach (string wordInQuery in query.Split(' '))
            {
                if (wordInQuery == word)
                {
                    termFreqQuery++;
                }
            }
            return termFreqQuery;
        }

        private void getTermFreqDoc(string docNum, string word)
        {
            //check if the term exist in the corpus
            if (!termPointerDict.ContainsKey(word))
            {
                df = 0;
                tf = 0;
                return;
            }
            df = queryTermInfoDict[word].df;
            if (!queryTermInfoDict[word].DocTermsDict.ContainsKey(docNum))
            {
                tf = 0;
                return;
            }
            tf = queryTermInfoDict[word].DocTermsDict[docNum].TermFreq;
        }

        /// <summary>
        /// calculate constant K
        /// </summary>
        private double calculateK(int docLength)
        {
            double leftSide = 1 - b;
            double rightSide = b * docLength / avgDocLength;
            return k1 * (leftSide + rightSide);
        }
    }
}
