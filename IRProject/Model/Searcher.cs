using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRProject.Model
{
    class Searcher
    {
        Ranker ranker;
        string query;
        string language;

        public Searcher(string query, string language, Ranker ranker)
        {
            this.query = query.ToLower();
            this.language = language;
            this.ranker = ranker;
            getTop50();
        }

        /// <summary>
        /// Gets the top 50 most relevant document
        /// </summary>
        private void getTop50()
        {
            ranker.calculateRelevance(query);
        }
    }
}
