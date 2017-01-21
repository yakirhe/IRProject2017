using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.Model
{
    /// <summary>
    /// This class save docs info
    /// </summary>
    class docInfo
    {
        #region properties
        private int docLength;

        public int DocLength
        {
            get { return docLength; }
            set { docLength = value; }
        }

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

        /// <summary>
        /// this constructor init the properties
        /// </summary>
        /// <param name="maxTerm">max term</param>
        /// <param name="max"></param>
        /// <param name="lang">language</param>
        /// <param name="uniqueTerms">number of uniqe terms</param>
        public docInfo(string maxTerm, int max, string lang, int uniqueTerms, int docLength)
        {
            this.docLength = docLength;
            this.maxTerm = maxTerm;
            this.max = max;
            this.lang = lang;
            this.uniqueTerms = uniqueTerms;
        }

        /// <summary>
        /// This constructor init the propertirs from a string
        /// </summary>
        /// <param name="docInfo"></param>
        public docInfo(string docInfo)
        {
            //build docInfo object
            string[] docInfoArr = docInfo.Split(' ');
            this.maxTerm = docInfoArr[0];
            this.max = Int32.Parse(docInfoArr[1]);
            this.lang = docInfoArr[2];
            this.uniqueTerms = Int32.Parse(docInfoArr[3]);
            this.docLength = Int32.Parse(docInfoArr[4]);
        }

        /// <summary>
        /// override to string that represent the docInfo object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return maxTerm + " " + max + " " + lang + " " + uniqueTerms + " " + docLength;
        }
    }
}
