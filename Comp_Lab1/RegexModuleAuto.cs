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

    // Класс для хранения информации о найденном совпадении
    public virtual class RegexMatchResult
    {
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int Length { get; set; }
    }

    public class RegexAnalyzer
    {
        // Оставляем Regex только для целых чисел (1 блок)
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
                    // Стандартный поиск через Regex для чисел
                    foreach (Match match in _intRegex.Matches(currentLine))
                    {
                        results.Add(CreateResult(match.Value, i + 1, match.Index + 1, match.Length));
                    }
                }
                else
                {
                    // Поиск через Конечный Автомат (FSM) для ФИО и HSL
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
                            j += matchLength; // Пропускаем найденное
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
                    case 0: // Ожидаем 1-ю заглавную букву
                        if (char.IsUpper(c)) state = 1; else return 0;
                        break;
                    case 1: // Точка после первого инициала
                        if (c == '.') state = 2; else return 0;
                        break;
                    case 2: // Пробел или сразу 2-я заглавная
                        if (char.IsWhiteSpace(c)) state = 3;
                        else if (char.IsUpper(c)) state = 4;
                        else return 0;
                        break;
                    case 3: // Строго 2-я заглавная после пробела
                        if (char.IsUpper(c)) state = 4; else return 0;
                        break;
                    case 4: // Точка после второго инициала
                        if (c == '.') state = 5; else return 0;
                        break;
                    case 5: // Пробел или сразу заглавная Фамилии
                        if (char.IsWhiteSpace(c)) state = 6;
                        else if (char.IsUpper(c)) state = 7;
                        else return 0;
                        break;
                    case 6: // Строго заглавная фамилии
                        if (char.IsUpper(c)) state = 7; else return 0;
                        break;
                    case 7: // Первая строчная фамилии
                        if (char.IsLower(c)) state = 8; else return 0;
                        break;
                    case 8: // Остальные строчные фамилии (цикл)
                        if (char.IsLower(c)) state = 8;
                        else return (i - start); // Успех, встретили не букву
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
                    case 4: // Первая цифра H
                        if (char.IsDigit(c)) { digits = 1; state = 5; } else return 0; break;
                    case 5: // Цифры H или запятая
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == ',') state = 6; else return 0; break;
                    case 6: // Пробелы или цифра S
                        if (char.IsWhiteSpace(c)) break;
                        if (char.IsDigit(c)) { digits = 1; state = 7; } else return 0; break;
                    case 7: // Цифры S или %
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == '%') state = 8; else return 0; break;
                    case 8: if (c == ',') state = 9; else return 0; break;
                    case 9: // Пробелы или цифра L
                        if (char.IsWhiteSpace(c)) break;
                        if (char.IsDigit(c)) { digits = 1; state = 10; } else return 0; break;
                    case 10: // Цифры L или %
                        if (char.IsDigit(c)) { if (++digits > 3) return 0; }
                        else if (c == '%') state = 11; else return 0; break;
                    case 11: // Финальная скобка
                        if (c == ')') return (i - start + 1); else return 0;
                }
                i++;
            }
            return 0;
        }
    }
}*/