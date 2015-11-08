written by Mark Tanner
website: www.marktanner.org
contact: admin@marktanner.org

the novel is a series of questions and answers. 
the question is in the form of "what is noun X"
where noun X is a noun in the answer of the previous question.
the first question is "what is love?"
there can be no duplicate questions
answers must be short and concise

i had a lot of trouble creating code that ran in less than 1000 years.
my breakthrough was this:
lets say we have the noun "tree", and "tree" only exists in the definition
of one word "park".
when we process the word "park" and dont choose "tree" from the list of nouns
in the definition of "park", "tree" then becomes dead, i.e. we can never get
to "tree" because nothing else has it in its definition.
so when choosing a noun from the list of nouns in a definition, we choose the
noun that appears in the least of amount definitions.
when we are counting how many definitions a noun exists in, we only count definitions
of nouns that havent been chosen yet.