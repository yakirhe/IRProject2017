using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.Model
{
    /// <summary>
    /// This class contain information about each doc
    /// </summary>
    class TermInfo
    {
        #region fields
        private Dictionary<string, TermDocInfo> docTermsDict; //key = the doc number, the term info in the doc (tf,position...)
        private int m_df;
        private int m_pos;
        private string m_docId;
        #endregion

        #region propfull
        public int df
        {
            get { return m_df; }
            set { m_df = value; }
        }
        public int pos
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        public Dictionary<string, TermDocInfo> DocTermsDict
        {
            get { return docTermsDict; }
            set
            {
                docTermsDict = value;
            }
        }

        public string docId
        {
            get { return m_docId; }
            set { m_docId = value; }
        }
        #endregion


        /// <summary>
        /// this constructor init the fields and add new doc to the dictionary
        /// </summary>
        /// <param name="docId">the doc id</param>
        /// <param name="pos">postion</param>
        public TermInfo(string docId, int pos)
        {
            m_pos = pos;
            m_df = 1;
            m_docId = docId;
            addDocToDict();
        }

        public TermInfo(string termInfo)
        {
            docTermsDict = new Dictionary<string, TermDocInfo>();
            //a full line in the posting list that we need to translate
            m_df = Int32.Parse(termInfo.Substring(1, termInfo.IndexOf("*") - 1));
            string remaining = termInfo.Substring(termInfo.IndexOf('*') + 1);
            string[] docInfo = remaining.Split(')');
            List<string> positionList = new List<string>();
            List<int> positionListInt = new List<int>();
            //now we have info about each doc
            foreach (string docData in docInfo)
            {
                string docDataSecond;
                string docNum = docData.Substring(0, docData.IndexOf('|')).Trim();
                docDataSecond = docData.Substring(docData.IndexOf('|') + 1);
                string tf = docDataSecond.Substring(0, docDataSecond.IndexOf('-') - 0);
                string postion = docDataSecond.Substring(docDataSecond.IndexOf('-') + 1);
                //There is more then 1 ocurence in this doc
                if (Int32.Parse(tf) != 1)
                {
                    positionList = postion.Split(',').ToList();
                    //convert the string list to int list
                    foreach (string pos in positionList)
                    {
                        positionListInt.Add(Int32.Parse(pos));
                    }
                }
                else
                {
                    positionListInt.Add(Int32.Parse(postion));
                }
                TermDocInfo tdi = new TermDocInfo(docNum, positionListInt);
                docTermsDict[docNum] = tdi;
            }

        }


        /// <summary>
        /// add new doc to the dictionary
        /// </summary>
        private void addDocToDict()
        {
            docTermsDict = new Dictionary<string, TermDocInfo>();
            docTermsDict[docId] = new TermDocInfo(1, pos);
        }

        /// <summary>
        /// add term instance to specific doc
        /// </summary>
        /// <param name="docId">doc id</param>
        /// <param name="pos">postion</param>
        public void addTermInstance(string docId, int pos)
        {
            if (!docTermsDict.ContainsKey(docId)) //if the document is not exist in the posting list add 1 to df
            {
                this.df++;
                docTermsDict[docId] = new TermDocInfo(1, pos);
            }
            else //the document is already in the posting list
            {
                docTermsDict[docId].updateTermInfo(pos);
            }
        }

        /// <summary>
        /// override the to string
        /// </summary>
        /// <returns>return the string</returns>
        public override string ToString()
        {
            string dictString = buildDictString();
            return m_df.ToString() + "*" + dictString;
        }

        /// <summary>
        /// this function help to the to string function
        /// </summary>
        /// <returns>the string </returns>
        private string buildDictString()
        {
            string str = "";
            int i = 1;
            foreach (string docNubmber in docTermsDict.Keys.ToList())
            {
                if (i == docTermsDict.Count)
                {
                    str += docNubmber + "|" + docTermsDict[docNubmber].ToString();
                }
                else
                {
                    str += docNubmber + "|" + docTermsDict[docNubmber].ToString() + ")";
                }
                i++;
            }
            return str;
        }
    }
}
