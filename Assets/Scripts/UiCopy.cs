using System.Text.RegularExpressions;

public static class UiCopy
{
    // Titles and short labels use initial capitals. Units and control notation retain their meaning.
    public static string Title(string value)
    {
        if(string.IsNullOrEmpty(value))return value;
        return Regex.Replace(value,@"[A-Za-z]+(?:'[A-Za-z]+)?",match=>
        {
            string word=match.Value;
            if(word=="SUV"||word=="UI"||word=="WASD"||word=="km"||word=="h"||word=="m"||word=="s"||word=="x")return word;
            return char.ToUpperInvariant(word[0])+word.Substring(1).ToLowerInvariant();
        });
    }
}
