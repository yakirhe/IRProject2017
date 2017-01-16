using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProjectConsole
{
    class TermDocInfo
    {
        private int pos;
        private int termFreq;
        private List<int> termPos;

        public TermDocInfo()
        {

        }

        public TermDocInfo(int termFreq, int pos)
        {
            this.termFreq = termFreq;
            this.pos = pos;
            termPos = new List<int>();
            termPos.Add(pos);
        }

        public void updateTermInfo(int pos)
        {
            termPos.Add(pos);
            this.termFreq++;
        }

        public override string ToString()
        {
            string termPosString = buildPosString();
            return termFreq.ToString() + "-" + termPosString;
        }

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
