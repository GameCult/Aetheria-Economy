using System;
using System.Collections.Generic;
using UnityEngine;

//Generates random names based on the statistical weight of letter sequences
//in a collection of sample names
public class MarkovNameGenerator
{
    //constructor
    public MarkovNameGenerator(IEnumerable<string> sampleNames, int order, int minLength, int maxLength)
    {
        //fix parameter values
        if (order < 1)
            order = 1;
        if (minLength < 1)
            minLength = 1;

        _order = order;
        _minLength = minLength;
        _maxLength = maxLength;

        //split comma delimited lines
        foreach (string line in sampleNames)
        {
            string[] tokens = line.Split(' ','-','\'','(',')');
            foreach (string word in tokens)
            {
                string upper = word.Trim().ToUpper();
                if (upper.Length < order + 1)
                    continue;                   
                _samples.Add(upper + '|'); // Add end character
            }
        }

        //Build chains            
        foreach (string word in _samples)
        {              
            for (int letter = 0; letter < word.Length - order; letter++)
            {
                string token = word.Substring(letter, order);
                List<char> entry = null;
                if (_chains.ContainsKey(token))
                    entry = _chains[token];
                else
                {
                    entry = new List<char>();
                    _chains[token] = entry;
                }
                entry.Add(word[letter + order]);
            }
        }
    }

    //Get the next random name
    public string NextName
    {
        get
        {
                            
            string s = "";
            do
            {
                int n = UnityEngine.Random.Range(0,_samples.Count);
                //int nameLength = _samples[n].Length;
                //s = _samples[n].Substring(UnityEngine.Random.Range(0, _samples[n].Length - _order), _order);//get a random token somewhere in middle of sample word
                s = _samples[n].Substring(0, _order);//get a random token at the start of a sample word
                while (true)//(s.Length < nameLength)
                {
                    string token = s.Substring(s.Length - _order, _order);
                    char c = GetLetter(token);
                    if (c != '?' && c != '|')
                        s += c;//GetLetter(token);
                    else
                        break;
                }

//                if (s.Contains(" "))
//                {
//                    string[] tokens = s.Split(' ');
//                    s = "";
//                    for (int t = 0; t < tokens.Length; t++)
//                    {
//                        if (tokens[t] == "")
//                            continue;
//                        if (tokens[t].Length == 1)
//                            tokens[t] = tokens[t].ToUpper();
//                        else
//                            tokens[t] = tokens[t].Substring(0, 1) + tokens[t].Substring(1).ToLower();
//                        if (s != "")
//                            s += " ";
//                        s += tokens[t];
//                    }
//                }
//                else
                s = s.Substring(0, 1) + s.Substring(1).ToLower();
            }
            while (_used.Contains(s) || s.Length < _minLength || s.Length > _maxLength);
            _used.Add(s);
            return s;
        }
    }

    //Reset the used names
    public void Reset()
    {
        _used.Clear();
    }

    //private members
    private Dictionary<string, List<char>> _chains = new Dictionary<string, List<char>>();
    private List<string> _samples = new List<string>();
    private static List<string> _used = new List<string>();
    private int _order;
    private int _minLength;
    private int _maxLength;

    //Get a random letter from the chain
    private char GetLetter(string token)
    {
        if (!_chains.ContainsKey(token))
            return '?';
        List<char> letters = _chains[token];
        int n = UnityEngine.Random.Range(0,letters.Count);
        return letters[n];
    }
}