using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace novel_generator_2
{
    class Program
    {
        const int targeWordCount = 50000;

        static Dictionary<string, Word> words;

        static void Main(string[] args)
        {
            if(Directory.Exists(Directory.GetCurrentDirectory() + "/cache/") == false)
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/cache/");
            }

            words = new Dictionary<string, Word>();
            
            populateWords("love");

            int wordCount = 0;
            HashSet<string> usedWords = new HashSet<string>();

            string novel = run("love", "", ref wordCount, usedWords);

            File.WriteAllText(Directory.GetCurrentDirectory() + "/novel.txt", novel);
        }

        static Word populateWords(string root)
        {
            if(words.ContainsKey(root) == false)
            {
                Word rootWord = new Word();
                words.Add(root, rootWord);
                rootWord.word = root;
                
                string rootDefinition = definitionsForWord(root);

                //rootWord.definition = rootDefinition;
                List<string> subDefinitionStrings = allSubDefinitionsFromDefinition(rootDefinition);

                rootWord.subDefinitions = new List<SubDefinition>();
                rootWord.appearsInDefinitionFor = new List<Word>();

                foreach(string subDefinitionString in subDefinitionStrings)
                {
                    SubDefinition subDefinition = new SubDefinition();
                    subDefinition.subDefinition = subDefinitionString;
                    List<string> nounStrings = allNounsFromDefinition(subDefinitionString);

                    rootWord.subDefinitions.Add(subDefinition);

                    subDefinition.nouns = new List<Word>();

                    foreach(string nounString in nounStrings)
                    {
                        Word noun = populateWords(nounString);
                        noun.appearsInDefinitionFor.Add(rootWord);
                        subDefinition.nouns.Add(noun);
                    }
                }

                return rootWord;
            }
            else
            {
                return words[root];
            }
        }

        static string run(string wordString, string whatWeHaveSoFar, ref int whatWeHaveSoFarWordCount, HashSet<string> usedNouns)
        {
            usedNouns.Add(wordString);

            Word word = words[wordString];

            List<NounDefinitionPair> pairs = nounDefinitionPairsOrderedByRarity(word, usedNouns);

            foreach (NounDefinitionPair pair in pairs)
            {
                string thisDefinition = "what is " + wordString + "?\n\n" + pair.definition.subDefinition + "\n\n";
                string whatWeHaveSoFar2 = whatWeHaveSoFar + thisDefinition;
                
                int whatWeHaveSoFarWordCount2 = whatWeHaveSoFarWordCount + thisDefinition.Split(' ').Length;
                
                if (whatWeHaveSoFarWordCount2 > targeWordCount)
                {
                    whatWeHaveSoFarWordCount = whatWeHaveSoFarWordCount2;
                    return whatWeHaveSoFar2;
                }

                if (usedNouns.Contains(pair.noun.word) == false)
                {
                    int whatWeHaveSoFarWordCount3 = whatWeHaveSoFarWordCount2;

                    //we are cloning the hashset here as we dont want to modify the existing one
                    string whatWeHaveSoFar3 = run(pair.noun.word, whatWeHaveSoFar2, ref whatWeHaveSoFarWordCount3, new HashSet<string>(usedNouns));
                    if (whatWeHaveSoFarWordCount3 > targeWordCount)
                    {
                        whatWeHaveSoFarWordCount = whatWeHaveSoFarWordCount3;
                        return whatWeHaveSoFar3;
                    }
                }
            }

            whatWeHaveSoFarWordCount = 0;
            return "";
        }

        //goes through the definition and pulls out all the nouns and their parent subdefinitions
        //returns in the order of how rare the noun is, i.e. how little it appears in the 
        //definitions of other words, (excluding the words that have already been used)
        static List<NounDefinitionPair> nounDefinitionPairsOrderedByRarity(Word noun, HashSet<string> usedNouns)
        {
            //we will get better time complexity if we return a unique list of nouns
            HashSet<string> uniqueSubNouns = new HashSet<string>();

            List<NounDefinitionPair> pairs = new List<NounDefinitionPair>();

            foreach (SubDefinition subDefinition in noun.subDefinitions)
            {
                foreach (Word subNoun in subDefinition.nouns)
                {
                    if (uniqueSubNouns.Contains(subNoun.word))
                    {
                        continue;
                    }

                    uniqueSubNouns.Add(subNoun.word);

                    NounDefinitionPair pair = new NounDefinitionPair();

                    pair.definition = subDefinition;
                    pair.noun = subNoun;
                    pairs.Add(pair);
                }
            }

            pairs = pairs.OrderBy(p => p.noun.numAppearsInDefinitionsForExcludingThese(usedNouns)).ToList();

            return pairs;
        }

        static string definitionsForWord(string word)
        {
            string pathToFile = Directory.GetCurrentDirectory() + "/cache/" + word;
            string definition;

            if(File.Exists(pathToFile))
            {
                return File.ReadAllText(pathToFile);
            }
            else
            {
                XDocument doc = XDocument.Load("http://services.aonaware.com/DictService/DictService.asmx/DefineInDict?dictid=wn&word=" + word);

                string firstFullDefinition = doc.Descendants().Where(p => p.Name.LocalName == "WordDefinition").Select(p => p.Value).ToList()[0];

                File.WriteAllText(pathToFile, firstFullDefinition);

                return firstFullDefinition;
            }
        }

        static List<String> allSubDefinitionsFromDefinition(string definition)
        {
            string[] subDefinitions = definition.Split(':');
            List<string> goodSubDefinitions = new List<string>();

            for (int i = 1; i < subDefinitions.Length; i++)
            {
                //remove numbers and new lines
                subDefinitions[i] = Regex.Replace(subDefinitions[i], "[(\\d|\\n)-]", "");

                //combine spaces
                subDefinitions[i] = Regex.Replace(subDefinitions[i], "\\s\\s+", " ");

                if (subDefinitions[i].Contains("[ant")) //remove antonyms
                {
                    subDefinitions[i] = subDefinitions[i].Replace("[ant", "");
                    goodSubDefinitions.Add(subDefinitions[i]);
                    i++;
                }
                else if (subDefinitions[i].Contains("[syn")) //remove synonyms
                {
                    subDefinitions[i] = subDefinitions[i].Replace("[syn", "");
                    goodSubDefinitions.Add(subDefinitions[i]);
                    i++;
                }
                else
                {
                    goodSubDefinitions.Add(subDefinitions[i]);
                }
            }

            return goodSubDefinitions;
        }

        static List<string> allNounsFromDefinition(string definition)
        {
            //matches all the words that come after 'the' or 'a'
            MatchCollection matches = Regex.Matches(definition, "(?<=\\b(the|a)\\s)(\\w+){4,}");
            List<string> nouns = new HashSet<string>(matches.Cast<Match>().Select(m => m.Value)).ToList();

            return nouns;
        }
    }
}
