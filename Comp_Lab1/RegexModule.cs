using System.Text.RegularExpressions;
namespace Comp_Lab1;

public enum SearchPatternType
{
    IntegerNumber,
    InitialsAndLastName,
    HslColor
}

public class RegexMatchResult
{
    public string Value { get; set; }
    public int Line { get; set; }
    public int StartPos { get; set; }
    public int Length { get; set; }
}
public class RegexAnalyzer
{
    private readonly Dictionary<SearchPatternType, Regex> _compiledPatterns = new Dictionary<SearchPatternType, Regex>
    {
        { 
            SearchPatternType.IntegerNumber, 
            new Regex(@"(?<![\w+-])[+-]?(0|[1-9]\d*)(?![\w])", RegexOptions.Compiled | RegexOptions.Multiline) 
        },
        { 
            SearchPatternType.InitialsAndLastName, 
            new Regex(@"[А-ЯЁ]\.\s?[А-ЯЁ]\.\s?[А-ЯЁ][а-яё]+", RegexOptions.Compiled | RegexOptions.Multiline) 
        },
        { 
            SearchPatternType.HslColor, 
            new Regex(@"hsl\(\d{1,3},\s*\d{1,3}%,\s*\d{1,3}%\)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline) 
        }
    };

    public List<RegexMatchResult> FindMatches(string text, SearchPatternType patternType)
    {
        var results = new List<RegexMatchResult>();
        if (string.IsNullOrWhiteSpace(text)) return results;

        var regex = _compiledPatterns[patternType];
            
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            MatchCollection matches = regex.Matches(lines[i]);
            foreach (Match match in matches)
            {
                results.Add(new RegexMatchResult
                {
                    Value = match.Value,
                    Line = i + 1,
                    StartPos = match.Index + 1,
                    Length = match.Length
                });
            }
        }

        return results;
    }
}