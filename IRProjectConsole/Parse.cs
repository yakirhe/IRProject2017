using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IRProjectConsole
{
    class Parse
    {
        string docText;
        string docNum;
        //List<string> stopWords;
        Dictionary<string, string> stopWordsDic;
        static Dictionary<string, string> monthsDic;
        List<string> filteredTokenes;
        string[] unfilteredTerms;
        char[] filteredSigns = { ',', ':', '(', ')', '[', ']', '{', '}', '\'', '\"', '*', '#' };
        Stemmer stemmer;
        Indexer indexer;
        string docLang;
        static bool firstTime = true;
        private const int MAX_DOCS = 300;
        private static Queue<List<string>> docsTokens = new Queue<List<string>>();

        public Parse(Dictionary<string, string> stopWordsDic)
        {
            this.stopWordsDic = stopWordsDic;
            stemmer = new Stemmer();
            indexer = new Indexer();
            if (firstTime)
            {
                buildMonthsDic();
                firstTime = false;
            }
        }

        private void buildMonthsDic()
        {
            monthsDic = new Dictionary<string, string>();
            monthsDic["january"] = "01";
            monthsDic["jan"] = "01";
            monthsDic["february"] = "02";
            monthsDic["feb"] = "02";
            monthsDic["march"] = "03";
            monthsDic["mar"] = "03";
            monthsDic["april"] = "04";
            monthsDic["apr"] = "04";
            monthsDic["may"] = "05";
            monthsDic["june"] = "06";
            monthsDic["jun"] = "06";
            monthsDic["july"] = "07";
            monthsDic["jul"] = "07";
            monthsDic["august"] = "08";
            monthsDic["aug"] = "08";
            monthsDic["september"] = "09";
            monthsDic["sep"] = "09";
            monthsDic["october"] = "10";
            monthsDic["oct"] = "10";
            monthsDic["november"] = "11";
            monthsDic["nov"] = "11";
            monthsDic["december"] = "12";
            monthsDic["dec"] = "12";
        }

        private void tokenize(out Dictionary<string, TermInfo> fileTermsDict)
        {
            docText = docText.Replace("/", " ");
            unfilteredTerms = docText.Split('\n', ' ');
            filterTokens(out fileTermsDict);
        }

        private Dictionary<string, TermInfo> tokenize(Dictionary<string, TermInfo> fileTermsDict)
        {
            docText = docText.Replace("/", " ");
            unfilteredTerms = docText.Split('\n', ' ');
            return filterTokens(fileTermsDict);
        }

        private void filterTokens(out Dictionary<string, TermInfo> fileTermsDict)
        {
            filterTokensProcedure();
            //send to the Indexer
            Indexer ind = new Indexer();
            fileTermsDict = ind.buildIndexDictionary(filteredTokenes, docNum, docLang);
        }

        private void filterTokensProcedure()
        {
            if (unfilteredTerms[1].Trim().ToLower() == "language:")
            {
                this.docLang = unfilteredTerms[3].ToLower();
            }
            for (int i = 0; i < unfilteredTerms.Length; i++)
            {
                //change all the terms to lowercase and trim the blank spaces
                unfilteredTerms[i] = unfilteredTerms[i].Replace("\'", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace("\"", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace("*", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace(":", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace(",", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace("(", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Replace(")", string.Empty);
                unfilteredTerms[i] = unfilteredTerms[i].Trim(filteredSigns).ToLower();
                //check for the rules for each one of the tokens
                numParsing(unfilteredTerms[i], i);
                //check for the dollar sign at the beginning of a token
                checkForDollarSign(unfilteredTerms[i], i);
                //check if the token is a date type token
                checkForDate(unfilteredTerms[i], i);
                //filter stopWords
                removeStopWords(unfilteredTerms[i], i);
                //check if the token is empty and pass it
                if (unfilteredTerms[i] == "")
                {
                    continue;
                }
                //perform stemming
                unfilteredTerms[i] = stemmer.stemTerm(unfilteredTerms[i]);
                //add to the token list
                filteredTokenes.Add(unfilteredTerms[i]);
            }
        }

        private Dictionary<string, TermInfo> filterTokens(Dictionary<string, TermInfo> fileTermsDict)
        {
            filterTokensProcedure();
            //send to the Indexer
            Indexer ind = new Indexer(fileTermsDict);
            fileTermsDict = ind.buildIndexDictionary(filteredTokenes, docNum, docLang);
            return fileTermsDict;
        }

        private void checkForDate(string token, int i)
        {
            int res = 0;
            //look for term in the pattern of dd month yy/yyyy
            if (Int32.TryParse(token, out res))
            {
                //check the next token if exists
                if (i + 1 < unfilteredTerms.Length)
                {
                    string nextToken = unfilteredTerms[i + 1].Trim(filteredSigns).ToLower();
                    if (monthsDic.ContainsKey(nextToken)) //the term is a month
                    {
                        //check if there is an year and if the day is valid
                        int year = checkIfDayIsValid(nextToken, i, res, 0, token, false);
                        if (year == -1)
                        {
                            //return;
                        }
                        //check if the next term is a year number
                        if (i + 2 < unfilteredTerms.Length)
                        {
                            //trim the term
                            unfilteredTerms[i + 2] = unfilteredTerms[i + 2].Trim(filteredSigns);
                            if (Int32.TryParse(unfilteredTerms[i + 2], out res))
                            {
                                //the term i+2 is a number. now we need to check
                                yearModification(i, res, nextToken, unfilteredTerms[i], true);
                            }
                        }
                        //this is the pattern of dd month -> mm/dd
                        else
                        {
                            unfilteredTerms[i] = monthsDic[nextToken] + "-" + unfilteredTerms[i].PadLeft(2, '0');
                            unfilteredTerms[i + 1] = "";
                        }
                    }
                }
            }
            else
            {
                //check if its pattern dd(with postfix th/st/nd/rd) mounth yyyy
                if (token.Length > 2)
                {
                    if (!monthsDic.ContainsKey(token))
                    {
                        string postfix = token.Substring(token.Length - 2).ToLower();
                        switch (postfix)
                        {
                            case "th":
                            case "st":
                            case "nd":
                            case "rd":
                                token = token.Substring(0, token.Length - 2);
                                checkForDate(token, i);
                                break;
                        }
                    }
                }
                //check if the term is a month name
                if (monthsDic.ContainsKey(token))
                {
                    //check the next token if exists
                    if (i + 1 < unfilteredTerms.Length)
                    {
                        //check if the next token is a number between 1-30
                        int day = 0, year = 0;
                        unfilteredTerms[i + 1] = unfilteredTerms[i + 1].Trim(filteredSigns);
                        if (Int32.TryParse(unfilteredTerms[i + 1], out day))
                        {
                            if (day >= 1000 && day < 10000)
                            {
                                //this is a year not a day
                                year = day;
                                //we will keep the term in the format of yyyy-mm
                                unfilteredTerms[i] = unfilteredTerms[i + 1] + "-" + monthsDic[token];
                                unfilteredTerms[i + 1] = "";
                            }
                            else //check if the day is valid
                            {
                                year = checkIfDayIsValid(token, i, day, year, unfilteredTerms[i + 1], true);
                            }
                        }
                    }
                }
            }
        }

        private int checkIfDayIsValid(string token, int i, int day, int year, string dayToken, Boolean startWithMonth)
        {
            //this is a number
            switch (token)
            {
                case "january":
                case "jan":
                case "march":
                case "mar":
                case "may":
                case "july":
                case "jul":
                case "august":
                case "aug":
                case "october":
                case "oct":
                case "december":
                case "dec":
                    if (day > 0 && day < 32)
                    {
                        if (!startWithMonth && i + 2 < unfilteredTerms.Length && Int32.TryParse(unfilteredTerms[i + 2].Trim(filteredSigns), out year))
                        {
                            unfilteredTerms[i + 2] = unfilteredTerms[i + 2].Trim(filteredSigns);
                            //check if the i+2 token is a number
                            if (year < 100 && year > 0) //if its 2 digits year (yy)
                            {
                                //complete the prefix
                                yearModification(i, year, token, dayToken, true);
                            }
                            else if (!startWithMonth)
                            {
                                return 0;
                            }
                            else //4 digits year
                            {
                                yearModification(i, year, token, dayToken, false);
                            }
                        }
                        //check if there is no year mentioned
                        else if (startWithMonth)
                        {
                            unfilteredTerms[i] = monthsDic[token] + "-" + unfilteredTerms[i + 1].PadLeft(2, '0');
                            unfilteredTerms[i + 1] = "";
                            return 0;
                        }
                        else
                        {
                            unfilteredTerms[i] = monthsDic[token] + "-" + unfilteredTerms[i].PadLeft(2, '0');
                            unfilteredTerms[i + 1] = "";
                            return 0;
                        }
                    }
                    else //not a valid day
                    {
                        return -1;
                    }
                    break;
                case "february":
                case "feb":
                    if (day > 0 && day < 30)
                    {
                        if (!startWithMonth && i + 2 < unfilteredTerms.Length && Int32.TryParse(unfilteredTerms[i + 2].Trim(filteredSigns), out year))
                        {
                            unfilteredTerms[i + 2] = unfilteredTerms[i + 2].Trim(filteredSigns);
                            //check if the i+2 token is a number
                            yearModification(i, year, token, dayToken, false);
                        }
                        else if (!startWithMonth)
                        {
                            return 0;
                        }
                        //check if there is no year mentioned
                        else
                        {
                            unfilteredTerms[i] = monthsDic[token] + "-" + unfilteredTerms[i + 1].PadLeft(2, '0');
                            unfilteredTerms[i + 1] = "";
                            return 0;
                        }
                    }
                    else //not a valid day
                    {
                        return -1;
                    }
                    break;
                case "april":
                case "apr":
                case "june":
                case "jun":
                case "september":
                case "sep":
                case "november":
                case "nov":
                    if (day > 0 && day < 31)
                    {
                        if (!startWithMonth && i + 2 < unfilteredTerms.Length && Int32.TryParse(unfilteredTerms[i + 2].Trim(filteredSigns), out year))
                        {
                            unfilteredTerms[i + 2] = unfilteredTerms[i + 2].Trim(filteredSigns);
                            //check if the i+2 token is a number
                            yearModification(i, year, token, dayToken, false);
                        }
                        else if (!startWithMonth)
                        {
                            return 0;
                        }
                        //check if there is no year mentioned
                        else
                        {
                            unfilteredTerms[i] = monthsDic[token] + "-" + unfilteredTerms[i + 1].PadLeft(2, '0');
                            unfilteredTerms[i + 1] = "";
                            return 0;
                        }
                    }
                    else //not a valid day
                    {
                        return -1;
                    }
                    break;
                default:
                    break;
            }
            return year;
        }

        private void yearModification(int i, int year, string nextToken, string day, bool usePrefix)
        {
            if (year > 0 && year < 100)
            {
                string prefix = "";
                //check if the number is between 0-20. if it is then assign 20 as the prefix
                if (usePrefix)
                {
                    if (year <= 20)
                    {
                        prefix = "20";
                    }
                    else
                    {
                        prefix = "19";
                    }
                    unfilteredTerms[i] = prefix + unfilteredTerms[i + 2] + "-" + monthsDic[nextToken] + "-" + day.PadLeft(2, '0');
                    unfilteredTerms[i + 1] = "";
                    unfilteredTerms[i + 2] = "";
                }
            }
            else if (year > 999 && year < 10000)
            {
                //the year is 4 digits
                unfilteredTerms[i] = unfilteredTerms[i + 2] + "-" + monthsDic[nextToken] + "-" + day.PadLeft(2, '0');
                unfilteredTerms[i + 1] = "";
                unfilteredTerms[i + 2] = "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="index"></param>
        private void checkForDollarSign(string token, int index)
        {
            if (token != "" && token[0] == '$' && token.Length > 1)
            {
                for (int i = 1; i < token.Length; i++)
                {
                    if (!Char.IsDigit(token[i]) && token[i] != '.')
                    {
                        return;
                    }
                }
                unfilteredTerms[index] = token.Substring(1);
                numParsing(token.Substring(1), index);
                unfilteredTerms[index] += " Dollars";
            }
        }

        private bool removeStopWords(string token, int i)
        {
            if (stopWordsDic.ContainsKey(token))
            {
                unfilteredTerms[i] = "";
                return true;
            }
            return false;
        }

        private void numParsing(string token, int i)
        {
            double res = 0;
            if (Double.TryParse(token, out res))
            {
                //check the next term if its a "million", "billion","trillion", percentage or $ sign
                bool relevant;
                if (i + 1 < unfilteredTerms.Length)
                {
                    //check for the word between 
                    if (i > 0)
                    {
                        string prevTerm = unfilteredTerms[i - 1];
                        if (prevTerm == "between")
                        {
                            if (unfilteredTerms[i + 1] == "and")
                            {
                                if (unfilteredTerms.Length > i + 2)
                                {
                                    //remove the word between
                                    filteredTokenes.RemoveAt(filteredTokenes.Count - 1);
                                    //add between number and number
                                    filteredTokenes.Add("between " + unfilteredTerms[i] + " and " + unfilteredTerms[i + 2]);
                                    //store as number - number
                                    unfilteredTerms[i + 1] = unfilteredTerms[i] + "-" + unfilteredTerms[i + 2];
                                }
                            }
                        }
                    }
                    //check if the next term is a fraction
                    if (i + 1 < unfilteredTerms.Length && checkSlash(unfilteredTerms[i + 1]))
                    {
                        unfilteredTerms[i] = unfilteredTerms[i] + " " + unfilteredTerms[i + 1];
                        if (i + 2 < unfilteredTerms.Length)
                        {
                            checkNextTerm(i, unfilteredTerms[i], unfilteredTerms[i + 2], out relevant, false);
                            if (relevant)
                            {
                                unfilteredTerms[i + 2] = "";
                            }
                        }
                        else
                        {
                            unfilteredTerms[i] = unfilteredTerms[i] + unfilteredTerms[i + 1];
                        }
                        unfilteredTerms[i + 1] = "";
                    }
                    else
                    {
                        //check the next term
                        string nextTerm = unfilteredTerms[i + 1].ToLower();
                        checkNextTerm(i, unfilteredTerms[i], nextTerm, out relevant, true);
                    }
                }
                if (res >= 1000000)
                {
                    //big number
                    double divideRes = (double)res / 1000000;
                    unfilteredTerms[i] = divideRes.ToString() + "M"; //add M to the end
                    if (i + 1 < unfilteredTerms.Length)
                        checkNextTerm(i, unfilteredTerms[i], unfilteredTerms[i + 1], out relevant, false);
                }
            }
            //check for terms in the pattern of: number m 
            else
            {
                int count = 0;
                string postfix = "";
                string num = "";
                foreach (char c in token)
                {
                    if (!Char.IsDigit(c) && c != '.')
                    {
                        num = token.Substring(0, count);
                        postfix = token.Substring(count);
                        break;
                    }
                    count++;
                }
                if (i + 1 < unfilteredTerms.Length && unfilteredTerms[i + 1].ToLower() == "dollars")
                {
                    switch (postfix)
                    {
                        case "m":
                            unfilteredTerms[i] = num + " M " + "Dollars";
                            unfilteredTerms[i + 1] = "";
                            break;
                        case "bn":
                            unfilteredTerms[i] = Double.Parse(num) * 1000 + " M " + "Dollars";
                            unfilteredTerms[i + 1] = "";
                            break;
                    }
                }
            }

        }

        private void checkNextTerm(int i, string res, string nextTerm, out bool relevant, bool checkNum)
        {
            relevant = false;
            nextTerm = nextTerm.Trim(filteredSigns);
            double resD = 0;
            if (checkNum)
            {
                res = res.Trim(filteredSigns);
                resD = Double.Parse(res);
            }
            switch (nextTerm)
            {
                case "bn":
                    if (i + 2 < unfilteredTerms.Length && unfilteredTerms[i + 2].Trim(filteredSigns).ToLower() == "dollars")
                    {
                        unfilteredTerms[i] = resD * 1000 + "M dollars";
                        unfilteredTerms[i + 1] = "";
                        unfilteredTerms[i + 2] = "";
                    }
                    else
                    {
                        if (i + 3 < unfilteredTerms.Length && unfilteredTerms[i + 2].ToLower() == "u.s." && unfilteredTerms[i + 3].ToLower() == "dollars")
                        {
                            unfilteredTerms[i] = resD * 1000 + "M dollars";
                            unfilteredTerms[i + 1] = "";
                            unfilteredTerms[i + 2] = "";
                            unfilteredTerms[i + 3] = "";
                        }
                    }
                    break;
                case "million":
                    unfilteredTerms[i] = unfilteredTerms[i] + "M";
                    if (i + 3 < unfilteredTerms.Length && unfilteredTerms[i + 2].ToLower() == "u.s." && unfilteredTerms[i + 3].ToLower() == "dollars")
                    {
                        unfilteredTerms[i] += " dollars";
                        unfilteredTerms[i + 2] = "";
                        unfilteredTerms[i + 3] = "";
                    }
                    unfilteredTerms[i + 1] = "";
                    break;
                case "billion":
                    unfilteredTerms[i] = resD * 1000 + "M";
                    if (i + 3 < unfilteredTerms.Length && unfilteredTerms[i + 2].ToLower() == "u.s." && unfilteredTerms[i + 3].ToLower() == "dollars")
                    {
                        unfilteredTerms[i] += " dollars";
                        unfilteredTerms[i + 2] = "";
                        unfilteredTerms[i + 3] = "";
                    }
                    unfilteredTerms[i + 1] = "";
                    break;
                case "trillion":
                    unfilteredTerms[i] = resD * 1000000 + "M";
                    if (i + 3 < unfilteredTerms.Length && unfilteredTerms[i + 2].ToLower() == "u.s." && unfilteredTerms[i + 3].ToLower() == "dollars")
                    {
                        unfilteredTerms[i] += " dollars";
                        unfilteredTerms[i + 2] = "";
                        unfilteredTerms[i + 3] = "";
                    }
                    unfilteredTerms[i + 1] = "";
                    break;
                case "percent":
                case "percentage":
                    relevant = true;
                    unfilteredTerms[i] = res + "%";
                    unfilteredTerms[i + 1] = "";
                    break;
                case "dollars":
                    relevant = true;
                    unfilteredTerms[i] = unfilteredTerms[i] + " Dollars";
                    unfilteredTerms[i + 1] = "";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// check if there is a slash between 2 numbers
        /// (if this is a fraction)
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool checkSlash(string token)
        {
            foreach (char c in token)
            {
                if (c == '/')
                {
                    return true;
                }
                else if (!Char.IsDigit(c))
                {
                    return false;
                }
            }
            return false;
        }

        public void parseDoc(string docNum, string docText, out Dictionary<string, TermInfo> fileTermsDict)
        {
            //test
            //Console.WriteLine("parse ok");
            //test
            //increase the num of docs that now in the ram
            //and check if we need to write to the disk
            this.docNum = docNum;
            this.docText = docText;
            //init the token list
            filteredTokenes = new List<string>();
            //extract the tokens from the textcdfd
            tokenize(out fileTermsDict);
        }

        public Dictionary<string, TermInfo> parseDoc(string docNum, string docText, Dictionary<string, TermInfo> fileTermsDict)
        {
            //test
            //Console.WriteLine("parse ok");
            //test
            //increase the num of docs that now in the ram
            //and check if we need to write to the disk
            this.docNum = docNum;
            this.docText = docText;
            //init the token list
            filteredTokenes = new List<string>();
            //extract the tokens from the textcdfd
            return tokenize(fileTermsDict);
        }
    }
}
