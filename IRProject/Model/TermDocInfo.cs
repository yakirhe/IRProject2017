using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.Model
{
    /// <summary>
    /// This class contain info about each doc
    /// </summary>
    class TermDocInfo
    {
        #region fields
        private int pos;

        private int termFreq;

        public int TermFreq
        {
            get { return termFreq; }
            set { termFreq = value; }
        }

        private List<int> termPos;
        private string docNum;

        #endregion

        /// <summary>
        /// empty constructor
        /// </summary>
        public TermDocInfo()
        {

        }

        /// <summary>
        /// this constructor init the fields
        /// </summary>
        /// <param name="termFreq"></param>
        /// <param name="pos"></param>
        public TermDocInfo(int termFreq, int pos)
        {
            this.termFreq = termFreq;
            this.pos = pos;
            termPos = new List<int>();
            termPos.Add(pos);
        }

        public TermDocInfo(string docNum, List<int> positionListInt)
        {
            this.docNum = docNum;
            this.termPos = positionListInt;
            this.termFreq = positionListInt.Count;
        }


        /// <summary>
        /// this function add the postion to the list
        /// </summary>
        /// <param name="pos">postion</param>
        public void updateTermInfo(int pos)
        {
            termPos.Add(pos);
            this.termFreq++;
        }

        /// <summary>
        /// this function override the to string
        /// </summary>
        /// <returns>return the string</returns>
        public override string ToString()
        {
            string termPosString = buildPosString();
            return termFreq.ToString() + "-" + termPosString;
        }

        /// <summary>
        /// this function build the string for the to string function
        /// </summary>
        /// <returns>the string</returns>
        private string buildPosString()
        {
            string str = "";
            int i = 1;
            foreach (int term in termPos.ToList())
            {
                if (i == termPos.Count)
                {
                    str += term;
                }
                else
                {
                    str += term + ",";
                }
                i++;
            }
            return str;
        }
    }
}
