using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace novel_generator_2
{
    class Word
    {
        public string word;
       // public string definition;
        public List<Word> appearsInDefinitionFor;
        public List<SubDefinition> subDefinitions;

        //returns the length of appearsInDefintionsFor, excluding the entries that also 
        //appear in the hashset
        public int numAppearsInDefinitionsForExcludingThese(HashSet<string> these)
        {
            int i = 0;

            foreach (Word word in appearsInDefinitionFor)
            {
                if (these.Contains(word.word) == false)
                {
                    i++;
                }
            }

            return i;
        }
    }
}
