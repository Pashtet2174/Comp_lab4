using System.Text.RegularExpressions;
namespace Comp_Lab1;

public enum SearchPatternType
{
    IntegerNumber,
    InitialsAndLastName,
    HslColor
}

// Класс для хранения информации о найденном совпадении
public class RegexMatchResult
{
    public string Value { get; set; }
    public int Line { get; set; }
    public int StartPos { get; set; }
    public int Length { get; set; }
}

// Основной класс-анализатор
public class RegexAnalyzer
{
    private readonly Dictionary<SearchPatternType, string> _patterns = new Dictionary<SearchPatternType, string>
    {
        { SearchPatternType.IntegerNumber, @"(?<!\w)[+-]?\d+(?!\w)" },
        { SearchPatternType.InitialsAndLastName, @"[А-ЯЁ]\.[А-ЯЁ]\.\s?[А-ЯЁ][а-яё]+" },
        { SearchPatternType.HslColor, @"hsl\(\d{1,3},\s?\d{1,3}%,\s?\d{1,3}%\)" }
    };

    public List<RegexMatchResult> FindMatches(string text, SearchPatternType patternType)
    {
        var results = new List<RegexMatchResult>();
        if (string.IsNullOrEmpty(text)) return results;

        string pattern = _patterns[patternType];
            
        // Разбиваем текст на строки, чтобы было легко определять номер строки и позицию
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            MatchCollection matches = Regex.Matches(lines[i], pattern);
            foreach (Match match in matches)
            {
                results.Add(new RegexMatchResult
                {
                    Value = match.Value,
                    Line = i + 1, // Строки обычно нумеруются с 1
                    StartPos = match.Index + 1, // Позиции символов тоже с 1
                    Length = match.Length
                });
            }
        }

        return results;
    }
}