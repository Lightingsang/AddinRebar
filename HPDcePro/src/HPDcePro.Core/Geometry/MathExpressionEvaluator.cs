using System;
using System.Collections.Generic;
using System.Globalization;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Pure C# recursive descent mathematical expression parser and evaluator.
    /// Replaces CalculateExpression, CalculateExpression2, and CalculateExpression3 native C++ exports.
    /// </summary>
    public class MathExpressionEvaluator
    {
        private readonly string _expression;
        private readonly IDictionary<string, double>? _variables;
        private int _pos;

        public MathExpressionEvaluator(string expression, IDictionary<string, double>? variables = null)
        {
            _expression = expression ?? string.Empty;
            _variables = variables;
            _pos = 0;
        }

        public static double Evaluate(string expr)
        {
            return new MathExpressionEvaluator(expr).Parse();
        }

        public static double EvaluateWithContext(string expr, IDictionary<string, double> vars)
        {
            return new MathExpressionEvaluator(expr, vars).Parse();
        }

        public static double EvaluateParametric(string expr, double t)
        {
            var vars = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { { "t", t } };
            return new MathExpressionEvaluator(expr, vars).Parse();
        }

        public double Parse()
        {
            _pos = 0;
            double result = ParseExpression();
            SkipWhitespace();
            if (_pos < _expression.Length)
            {
                throw new FormatException($"Unexpected character '{_expression[_pos]}' at position {_pos}");
            }
            return result;
        }

        private double ParseExpression()
        {
            double result = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (_pos >= _expression.Length) break;
                char c = _expression[_pos];
                if (c == '+')
                {
                    _pos++;
                    result += ParseTerm();
                }
                else if (c == '-')
                {
                    _pos++;
                    result -= ParseTerm();
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        private double ParseTerm()
        {
            double result = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (_pos >= _expression.Length) break;
                char c = _expression[_pos];
                if (c == '*')
                {
                    _pos++;
                    result *= ParseFactor();
                }
                else if (c == '/')
                {
                    _pos++;
                    double divisor = ParseFactor();
                    if (Math.Abs(divisor) < 1e-15)
                    {
                        throw new DivideByZeroException("Division by zero in mathematical expression.");
                    }
                    result /= divisor;
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        private double ParseFactor()
        {
            SkipWhitespace();
            if (_pos >= _expression.Length) return 0.0;

            if (_expression[_pos] == '+')
            {
                _pos++;
                return ParseFactor();
            }
            if (_expression[_pos] == '-')
            {
                _pos++;
                return -ParseFactor();
            }

            if (_expression[_pos] == '(')
            {
                _pos++;
                double val = ParseExpression();
                SkipWhitespace();
                if (_pos < _expression.Length && _expression[_pos] == ')')
                {
                    _pos++;
                }
                return val;
            }

            if (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.')
            {
                return ParseNumber();
            }

            if (char.IsLetter(_expression[_pos]) || _expression[_pos] == '_')
            {
                return ParseIdentifierOrFunction();
            }

            throw new FormatException($"Unexpected character '{_expression[_pos]}' at position {_pos}");
        }

        private double ParseNumber()
        {
            int start = _pos;
            while (_pos < _expression.Length && (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.'))
            {
                _pos++;
            }
            string numStr = _expression.Substring(start, _pos - start);
            return double.Parse(numStr, CultureInfo.InvariantCulture);
        }

        private double ParseIdentifierOrFunction()
        {
            int start = _pos;
            while (_pos < _expression.Length && (char.IsLetterOrDigit(_expression[_pos]) || _expression[_pos] == '_'))
            {
                _pos++;
            }
            string name = _expression.Substring(start, _pos - start);
            SkipWhitespace();

            if (_pos < _expression.Length && _expression[_pos] == '(')
            {
                _pos++;
                double arg = ParseExpression();
                SkipWhitespace();
                if (_pos < _expression.Length && _expression[_pos] == ')') _pos++;
                return EvaluateFunction(name, arg);
            }

            if (_variables != null && _variables.TryGetValue(name, out double val))
            {
                return val;
            }
            throw new KeyNotFoundException($"Variable or function '{name}' is not recognized in expression context.");
        }

        private double EvaluateFunction(string name, double arg)
        {
            switch (name.ToLowerInvariant())
            {
                case "sin": return Math.Sin(arg);
                case "cos": return Math.Cos(arg);
                case "tan": return Math.Tan(arg);
                case "sqrt": return Math.Sqrt(arg);
                case "abs": return Math.Abs(arg);
                case "rad": return MathUtils.DegToRad(arg);
                case "deg": return MathUtils.RadToDeg(arg);
                default:
                    throw new NotSupportedException($"Function '{name}' is not supported.");
            }
        }

        private void SkipWhitespace()
        {
            while (_pos < _expression.Length && char.IsWhiteSpace(_expression[_pos]))
            {
                _pos++;
            }
        }
    }
}
