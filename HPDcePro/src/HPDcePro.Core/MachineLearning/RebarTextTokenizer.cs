using System;
using System.Collections.Generic;
using System.Text;

namespace HPDcePro.Core.MachineLearning
{
    /// <summary>
    /// Pure C# tokenizer and parser for Vietnamese structural rebar annotations.
    /// Extracts bar quantity, diameter, spacing, shape, and length from varied CAD callouts.
    /// </summary>
    public static class RebarTextTokenizer
    {
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var sb = new StringBuilder(input);
            sb.Replace("%%c", "∅");
            sb.Replace("%%C", "∅");
            sb.Replace("\u03a6", "∅"); // Greek Phi uppercase
            sb.Replace("\u03c6", "∅"); // Greek Phi lowercase
            sb.Replace("\u2300", "∅"); // Diameter sign
            sb.Replace("\u2205", "∅"); // Empty set commonly used as diameter

            // Replace 'd' or 'D' with '∅' only when immediately followed by digits
            for (int i = 0; i < sb.Length - 1; i++)
            {
                if ((sb[i] == 'd' || sb[i] == 'D') && char.IsDigit(sb[i + 1]))
                {
                    if (i == 0 || !char.IsLetter(sb[i - 1]))
                    {
                        sb[i] = '∅';
                    }
                }
            }
            return sb.ToString();
        }

        public static List<RebarToken> Tokenize(string text)
        {
            var tokens = new List<RebarToken>();
            string norm = Normalize(text);
            if (string.IsNullOrWhiteSpace(norm)) return tokens;

            int i = 0;
            int tokenIndex = 0;
            while (i < norm.Length)
            {
                if (char.IsDigit(norm[i]))
                {
                    int start = i;
                    while (i < norm.Length && char.IsDigit(norm[i])) i++;
                    int len = i - start;
                    string numStr = norm.Substring(start, len);
                    int val = int.Parse(numStr);

                    string features = RebarFeatureExtractor.ExtractFeatures(norm, start, len, val);
                    var token = new RebarToken
                    {
                        Index = tokenIndex++,
                        Text = numStr,
                        NumericValue = val,
                        FeatureString = features,
                        PredictedType = ClassifyToken(features, val)
                    };
                    tokens.Add(token);
                }
                else
                {
                    i++;
                }
            }
            return tokens;
        }

        private static RebarTokenType ClassifyToken(string features, int val)
        {
            if (features.Contains("AFTERD_1") || features.Contains("PREV_d"))
            {
                return RebarTokenType.DK;
            }
            if (features.Contains("PREV_spc"))
            {
                return RebarTokenType.KC;
            }
            if (features.Contains("BEFORED_1") || features.Contains("NEXT_x"))
            {
                return RebarTokenType.SL;
            }
            if (features.Contains("INRING_1") || features.Contains("PREV_lbr") || features.Contains("SHLET_1"))
            {
                return RebarTokenType.SH;
            }
            if (features.Contains("AFTERL_1") || val >= 1000)
            {
                return RebarTokenType.L;
            }
            if (features.Contains("DKCHUAN_1") && val >= 10 && val <= 32)
            {
                return RebarTokenType.DK;
            }
            if (val <= 10)
            {
                return RebarTokenType.SL;
            }
            return RebarTokenType.KC;
        }

        public static RebarCallout ParseCallout(string text)
        {
            var callout = new RebarCallout { RawText = text };
            var tokens = Tokenize(text);

            foreach (var t in tokens)
            {
                if (!t.NumericValue.HasValue) continue;
                int val = t.NumericValue.Value;

                switch (t.PredictedType)
                {
                    case RebarTokenType.SH:
                        callout.BarMark = t.Text;
                        break;
                    case RebarTokenType.SL:
                        callout.Quantity = val;
                        break;
                    case RebarTokenType.DK:
                        callout.Diameter = val;
                        break;
                    case RebarTokenType.L:
                        callout.Length = val;
                        break;
                    case RebarTokenType.KC:
                        callout.Spacing = val;
                        break;
                    case RebarTokenType.KC1:
                        callout.LegA = val;
                        break;
                    case RebarTokenType.KC2:
                        callout.LegB = val;
                        break;
                    case RebarTokenType.KC3:
                        callout.LegC = val;
                        break;
                    case RebarTokenType.KC4:
                        callout.LegD = val;
                        break;
                }
            }
            return callout;
        }
    }
}
