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
    // Используем Compiled для ускорения работы в Windows Forms
    private readonly Dictionary<SearchPatternType, Regex> _compiledPatterns = new Dictionary<SearchPatternType, Regex>
    {
        { 
            SearchPatternType.IntegerNumber, 
            new Regex(@"(?<!\w)[+-]?\d+(?!\w)", RegexOptions.Compiled | RegexOptions.Multiline) 
        },
        { 
            SearchPatternType.InitialsAndLastName, 
            new Regex(@"[А-ЯЁ]\.\s?[А-ЯЁ]\.\s?[А-ЯЁ][а-яё]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline) 
        },
        { 
            SearchPatternType.HslColor, 
            // Тут IgnoreCase особенно полезен: найдет и hsl, и HSL
            new Regex(@"hsl\(\d{1,3},\s*\d{1,3}%,\s*\d{1,3}%\)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline) 
        }
    };

    public List<RegexMatchResult> FindMatches(string text, SearchPatternType patternType)
    {
        var results = new List<RegexMatchResult>();
        if (string.IsNullOrWhiteSpace(text)) return results;

        var regex = _compiledPatterns[patternType];
            
        // Вариант со Split хорош для получения номера строки, оставляем его
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