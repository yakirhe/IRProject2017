using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nml;
using LAIR.ResourceAPIs.WordNet;
using LAIR.Collections.Generic;

namespace IRProject.Model
{
    class Ranker
    {
        private const double k1 = 1.2;
        private const double b = 0;
        private const double alpha = 0.999;
        private Dictionary<string, docInfo> docsInfoDict;//key=docId, value = docInfo
        private Dictionary<string, long> termPointerDict;//key=term, value = pointer
        private Dictionary<string, double> docsRating;//key=docId,value=rating
        private Dictionary<string, TermInfo> queryTermInfoDict; //key=query word, value = TermInfo
        private Dictionary<string, int> termsDoc = new Dictionary<string, int>();//for one doc- key term,value-termFreq in doc
        private Dictionary<string, double> docsWeightResult;
        List<string> relevantDocs = new List<string>();//contain the relevant docs
        private double avgDocLength;
        private const double R = 0;
        private const double r = 0;
        private const double k2 = 100;
        private const char SEPERATOR = 'έ';
        private int df, tf;
        private WordNetEngine wn;
        string pathToSynonameDict;

        public Ranker()
        {
            pathToSynonameDict = @"..\..\..\wordsDb";
            wn = new WordNetEngine(pathToSynonameDict, false);
            //read the docs info dictionary drom the disk
            initializeDict();
            readTermPointerDict();
            readDocInfo();
        }

        /// <summary>
        /// All this function does is to initialize the dictionaries
        /// </summary>
        private void initializeDict()
        {
            termPointerDict = new Dictionary<string, long>();
            docsRating = new Dictionary<string, double>();
            queryTermInfoDict = new Dictionary<string, TermInfo>();
            docsWeightResult = new Dictionary<string, double>();
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
                        queryTermInfoDict[word] = getPostingLine(br, word);
                    }
                }
            }
        }

        private TermInfo getPostingLine(BinaryReader br, string word)
        {
            long positionToRead = termPointerDict[word];
            br.BaseStream.Seek(positionToRead, SeekOrigin.Begin);
            //get the term info
            string termInfoStr = br.ReadString();
            termInfoStr = termInfoStr.Substring(termInfoStr.IndexOf(SEPERATOR));
            TermInfo termInfo = new TermInfo(termInfoStr);
            return termInfo;
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
            docsInfoDict = new Dictionary<string, docInfo>();
            int docLengthSum = 0;
            using (Stream s = new FileStream("Files//DocsDict.txt", FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        string docId = br.ReadString().Trim();
                        string docInfo = br.ReadString();
                        docInfo di = new Model.docInfo(docInfo);
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
        public List<string> calculateRelevance(string query, string languages, int queryNumber = 0)
        {
            List<string> languagesList = languages.Split(' ').ToList();
            docsRating = new Dictionary<string, double>();
            #region calculateBM25 for the query
            //take all the relevant docs (docs that at least 1 query word appears in it)
            buildQueryTermInfoDict(query);
            createUnionListOfRelDocs();
            foreach (string docId in relevantDocs)
            {
                docsRating[docId] = alpha * calculateBM25(query, docId);
            }
            #endregion
            #region calculateWeight (tf*idf) for the expanded query
            string expandedQuery = expandQuery(query);
            //take all the relevant docs of the expanded query (docs that at least 1 query word appears in it)
            buildQueryTermInfoDict(expandedQuery);
            foreach (string docId in relevantDocs)
            {
                docsRating[docId] = docsRating[docId] + (1 - alpha) * calculateWeightRating(expandedQuery, docId);
            }
            #endregion
            //sort the docs rating dictionary
            var sortedDict = from entry in docsRating orderby entry.Value descending select entry;
            docsRating = sortedDict.ToDictionary(pair => pair.Key, pair => pair.Value);
            List<string> top50 = getTop50(languagesList);
            writeResults(top50, queryNumber);
            return top50;
        }

        private void writeResults(List<string> top50, int queryNumber = 0)
        {
            int counter = 1;
            using (Stream s = new FileStream("results.txt", FileMode.OpenOrCreate))
            {
                using (StreamWriter sw = new StreamWriter(s))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    foreach (string docId in top50)
                    {
                        string line = queryNumber + " 0 " + docId + " " + (counter++) + " " + docsRating[docId] + " mt";
                        sw.WriteLine(line);
                    }
                }
            }
        }


        private List<string> getTop50(List<string> languages)
        {
            bool checkLang = true;
            if (languages.Count == 0)
            {
                checkLang = false;
            }
            List<string> docsToReturn = new List<string>();
            int counter = 0;
            foreach (string docId in docsRating.Keys)
            {
                if (counter > 50)
                {
                    break; ;
                }
                if (checkLang && languages.Contains(docsInfoDict[docId].Langauge))
                {
                    docsToReturn.Add(docId);
                    counter++;
                }
                else if (!checkLang)
                {
                    docsToReturn.Add(docId);
                    counter++;
                }
            }
            if (docsToReturn.Count == 0)
            {
                docsToReturn.Add("No results found");
            }
            return docsToReturn;
        }

        private double calculateWeightRating(string query, string docId)
        {
            double numerator = 0;
            foreach (string word in query.Split(' '))
            {
                double numeratorLeft = getWeightedDocValue(docId, word, false);
                double queryWeight = wordFreqInQueryFunc(word, query);
                numerator += numeratorLeft * queryWeight;
            }
            if (numerator == 0)
                return 0;
            return numerator;
        }

        /// <summary>
        /// calculate tf*idf
        /// W = tf*idf
        /// idf= log2(N/df)
        /// tf= f/maxf
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private double getWeightedDocValue(string docId, string word, bool goToPosting)
        {
            getTermFreqDoc(docId, word);
            if (df == 0)
            {
                return 0;
            }
            double idf = Math.Log((double)docsInfoDict.Count / (double)df);
            double tfNormal = (double)tf / (double)docsInfoDict[docId].Max;
            return idf * tfNormal;
        }

        /// <summary>
        /// This function expanding the query for semantic evaluation
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private string expandQuery(string query)
        {
            string newQuery = "";
            //seperate the query to words
            string[] queryWords = query.Split(' ');
            foreach (string wordInQuery in queryWords)
            {
                Set<SynSet> synSets = wn.GetSynSets(wordInQuery, WordNetEngine.POS.Noun);
                Set<SynSet> synSetsAdj = wn.GetSynSets(wordInQuery, WordNetEngine.POS.Adjective);
                Set<SynSet> synSetsVerb = wn.GetSynSets(wordInQuery, WordNetEngine.POS.Verb);
                Set<SynSet> synSetsAdverb = wn.GetSynSets(wordInQuery, WordNetEngine.POS.Adverb);
                synSets = new Set<SynSet>(synSets.Union(synSetsAdj).Union(synSetsVerb).Union(synSetsAdverb).ToList());
                foreach (SynSet synSet in synSets)
                {
                    foreach (string synoname in synSet.Words)
                    {
                        if (synoname != wordInQuery && synoname.IndexOf('_') == -1)
                        {
                            newQuery += synoname + " ";
                        }
                    }
                }
            }
            return newQuery;
        }

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
            int docLength = docsInfoDict[docNum].DocLength;
            double k = calculateK(docLength);
            double sum = 0;
            foreach (string word in query.Split(' '))
            {
                int wordFreqInQuery = wordFreqInQueryFunc(word, query);
                getTermFreqDoc(docNum, word);
                double numerator = ((r + 0.5) / (R - r + 0.5)) * (k1 + 1) * tf * (k2 + 1) * wordFreqInQuery;
                double denominator = (df - r + 0.5) / (docsInfoDict.Count - df - R + r + 0.5) * (k + tf) * (k2 + wordFreqInQuery);
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


        private void loadTermsFreqDocToDict(string docNum)
        {
            string test = "docsTerms/ " + docNum + " .txt";
            using (Stream s = new FileStream(test, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        string term = br.ReadString();
                        termsDoc[term] = br.ReadInt32();
                    }
                }
            }
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
