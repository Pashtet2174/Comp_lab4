/*using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Comp_Lab1
{
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
        private readonly Regex _intRegex = new Regex(@"(?<!\w)[+-]?\d+(?!\w)", RegexOptions.Compiled);

        public List<RegexMatchResult> FindMatches(string text, SearchPatternType patternType)
        {
            var results = new List<RegexMatchResult>();
            if (string.IsNullOrWhiteSpace(text)) return results;

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string currentLine = lines[i];

                if (patternType == SearchPatternType.IntegerNumber)
                {
                    foreach (Match match in _intRegex.Matches(currentLine))
                    {
                        results.Add(CreateResult(match.Value, i + 1, match.Index + 1, match.Length));
                    }
                }
                else
                {
                    int j = 0;
                    while (j < currentLine.Length)
                    {
                        int matchLength = 0;

                        if (patternType == SearchPatternType.InitialsAndLastName)
                            matchLength = TrySimulateFioFsm(currentLine, j);
                        else if (patternType == SearchPatternType.HslColor)
                            matchLength = TrySimulateHslFsm(currentLine, j);

                        if (matchLength > 0)
                        {
                            results.Add(CreateResult(
                                currentLine.Substring(j, matchLength), 
                                i + 1, 
                                j + 1, 
                                matchLength
                            ));
                            j += matchLength; 
                        }
                        else
                        {
                            j++;
                        }
                    }
                }
            }
            return results;
        }

        private RegexMatchResult CreateResult(string val, int line, int pos, int len)
        {
            return new RegexMatchResult { Value = val, Line = line, StartPos = pos, Length = len };
        }

        // --- ГРАФ АВТОМАТА ДЛЯ ФИО (И. И. Иванов) ---
        private int TrySimulateFioFsm(string text, int start)
        {
            int state = 0;
            int i = start;

            while (i < text.Length)
            {
                char c = text[i];
                switch (state)
                {
                    case 0: 
                        if (char.IsUpper(c)) state = 1; else return 0;
                        break;
                    case 1: 
                        if (c == '.') state = 2; else return 0;
                        break;
                    case 2: 
                        if (char.IsWhiteSpace(c)) state = 3;
                        else if (char.IsUpper(c)) state = 4;
                        else return 0;
                        break;
                    case 3: 
                        if (char.IsUpper(c)) state = 4; else return 0;
                        break;
                    case 4: 
                        if (c == '.') state = 5; else return 0;
                        break;
                    case 5: 
                        if (char.IsWhiteSpace(c)) state = 6;
                        else if (char.IsUpper(c)) state = 7;
                        else return 0;
                        break;
                    case 6: 
                        if (char.IsUpper(c)) state = 7; else return 0;
                        break;
                    case 7: 
                        if (char.IsLower(c)) state = 8; else return 0;
                        break;
                    case 8: 
                        if (char.IsLower(c)) state = 8;
                        else return (i - start); 
                        break;
                }
                i++;
            }
            return (state == 8) ? (i - start) : 0;
        }

        // --- ГРАФ АВТОМАТА ДЛЯ HSL (hsl(0, 0%, 0%)) ---
        private int TrySimulateHslFsm(string text, int start)
        {
            int state = 0;
            int digits = 0;
            int i = start;

            while (i < text.Length)
            {
                char c = text[i];
                char low = char.ToLower(c);

                switch (state)
                {
                    case 0: if (low == 'h') state = 1; else return 0; break;
                    case 1: if (low == 's') state = 2; else return 0; break;
                    case 2: if (low == 'l') state = 3; else return 0; break;
                    case 3: if (c == '(') state = 4; else return 0; break;
                    case 4: 
                        if (char.IsDigit(c)) { digits = 1; state = 5; } else return 0; break;
                    case 5: 
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == ',') state = 6; else return 0; break;
                    case 6: 
                        if (char.IsWhiteSpace(c)) break;
                        if (char.IsDigit(c)) { digits = 1; state = 7; } else return 0; break;
                    case 7: 
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == '%') state = 8; else return 0; break;
                    case 8: if (c == ',') state = 9; else return 0; break;
                    case 9: 
                        if (char.IsWhiteSpace(c)) break;
                        if (char.IsDigit(c)) { digits = 1; state = 10; } else return 0; break;
                    case 10: 
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == '%') state = 11; else return 0; break;
                    case 11: 
                        if (c == ')') return (i - start + 1); else return 0;
                }
                i++;
            }
            return 0;
        }
    }
}*/