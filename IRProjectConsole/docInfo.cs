using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProjectConsole
{
    class docInfo
    {
        #region properties
        private string lang;

        public string Langauge
        {
            get { return lang; }
            set { lang = value; }
        }

        private int max;

        public int Max
        {
            get { return max; }
            set { max = value; }
        }

        private string maxTerm;

        public string MaxTerm
        {
            get { return maxTerm; }
            set { maxTerm = value; }
        }

        private int uniqueTerms;

        public int UniqueTerms
        {
            get { return uniqueTerms; }
            set { uniqueTerms = value; }
        }
        #endregion

        public docInfo(string maxTerm, int max, string lang, int uniqueTerms)
        {
            this.maxTerm = maxTerm;
            this.max = max;
            this.lang = lang;
            this.uniqueTerms = uniqueTerms;
        }
    }
}
