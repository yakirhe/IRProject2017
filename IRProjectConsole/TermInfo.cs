using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProjectConsole
{
    class TermInfo
    {
        private Dictionary<string, TermDocInfo> docTermsDict; //key = the doc number, the term info in the doc (tf,position...)

        public TermInfo(string docId, int pos)
        {
            m_pos = pos;
            m_df = 1;
            m_docId = docId;
            addDocToDict();
        }

        private int m_df;

        public int df
        {
            get { return m_df; }
            set { m_df = value; }
        }

        private int m_pos;

        public int pos
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        private string m_docId;

        public string docId
        {
            get { return m_docId; }
            set { m_docId = value; }
        }

        private void addDocToDict()
        {
            docTermsDict = new Dictionary<string, TermDocInfo>();
            docTermsDict[docId] = new TermDocInfo(1, pos);
        }

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

        public override string ToString()
        {
            string dictString = buildDictString();
            return m_df.ToString() + "*" + dictString;
        }

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
