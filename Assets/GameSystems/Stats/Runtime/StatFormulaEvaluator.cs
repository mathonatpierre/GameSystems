using System;
using System.Globalization;

namespace GameSystems.Stats
{
    public static class StatFormulaEvaluator
    {
        public static bool TryEvaluate(string expression, IStatProvider stats,
            out float value, out string error)
        {
            value = 0f;
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "Formula is empty.";
                return false;
            }

            try
            {
                var parser = new Parser(expression, stats);
                value = parser.Parse();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        sealed class Parser
        {
            readonly string expression;
            readonly IStatProvider stats;
            int index;

            public Parser(string expression, IStatProvider stats)
            {
                this.expression = expression;
                this.stats = stats;
            }

            public float Parse()
            {
                float value = ParseExpression();
                SkipWhitespace();
                if (!IsEnd) throw Error($"Unexpected token '{Current}'.");
                return value;
            }

            float ParseExpression()
            {
                float value = ParseTerm();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('+')) value += ParseTerm();
                    else if (Match('-')) value -= ParseTerm();
                    else return value;
                }
            }

            float ParseTerm()
            {
                float value = ParseFactor();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('*')) value *= ParseFactor();
                    else if (Match('/'))
                    {
                        float divisor = ParseFactor();
                        if (Math.Abs(divisor) < .000001f) throw Error("Division by zero.");
                        value /= divisor;
                    }
                    else return value;
                }
            }

            float ParseFactor()
            {
                SkipWhitespace();
                if (Match('+')) return ParseFactor();
                if (Match('-')) return -ParseFactor();
                if (Match('('))
                {
                    float value = ParseExpression();
                    SkipWhitespace();
                    if (!Match(')')) throw Error("Missing closing parenthesis.");
                    return value;
                }

                if (Match('[')) return ParseStatReference();
                return ParseNumber();
            }

            float ParseStatReference()
            {
                int start = index;
                while (!IsEnd && Current != ']') index++;
                if (IsEnd) throw Error("Missing closing bracket in stat reference.");
                string id = expression.Substring(start, index - start).Trim();
                index++;
                if (string.IsNullOrWhiteSpace(id)) throw Error("Empty stat reference.");
                if (stats == null) throw Error($"No stat provider for [{id}].");
                StatDefinition stat = stats.FindStat(id);
                if (stat == null) throw Error($"Unknown stat [{id}].");
                return stats.GetStatValue(stat);
            }

            float ParseNumber()
            {
                SkipWhitespace();
                int start = index;
                bool hasDecimal = false;
                while (!IsEnd)
                {
                    if (char.IsDigit(Current)) { index++; continue; }
                    if (Current == '.' && !hasDecimal)
                    {
                        hasDecimal = true;
                        index++;
                        continue;
                    }
                    break;
                }

                if (start == index) throw Error($"Expected number, stat reference or parenthesis near '{Current}'.");
                string token = expression.Substring(start, index - start);
                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    throw Error($"Invalid number '{token}'.");
                return value;
            }

            bool Match(char expected)
            {
                SkipWhitespace();
                if (IsEnd || Current != expected) return false;
                index++;
                return true;
            }

            void SkipWhitespace()
            {
                while (!IsEnd && char.IsWhiteSpace(Current)) index++;
            }

            char Current => expression[index];
            bool IsEnd => index >= expression.Length;

            Exception Error(string message) => new FormatException($"{message} Position {index}.");
        }
    }
}
