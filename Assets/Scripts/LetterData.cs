using UnityEngine;
using System.Collections.Generic;

public class LetterData : MonoBehaviour
{
    public string letter; 
    public int value;   

    private static readonly Dictionary<char, int> letterValues = new Dictionary<char, int>()
    {
        { 'A', 1 }, { 'B', 4 }, { 'C', 4 }, { 'D', 2 }, { 'E', 1 },
        { 'F', 4 }, { 'G', 3 }, { 'H', 3 }, { 'I', 1 }, { 'J', 10 },
        { 'K', 5 }, { 'L', 2 }, { 'M', 4 }, { 'N', 2 }, { 'O', 1 },
        { 'P', 4 }, { 'Q', 10 }, { 'R', 1 }, { 'S', 1 }, { 'T', 1 },
        { 'U', 2 }, { 'V', 5 }, { 'W', 4 }, { 'X', 8 }, { 'Y', 3 },
        { 'Z', 10 }
    };

    public void SetLetter(string c)
    {
        letter = c.ToUpper();
        value = letterValues.ContainsKey(letter[0]) ? letterValues[letter[0]] : 0;
    }
}
