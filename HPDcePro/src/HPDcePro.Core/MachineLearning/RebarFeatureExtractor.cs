using System;
using System.Collections.Generic;
using System.Text;

namespace HPDcePro.Core.MachineLearning
{
    /// <summary>
    /// Contextual NLP feature extractor for numerical tokens in Vietnamese rebar strings.
    /// Emits feature vectors matching MayHoc/mauhoc.db schema.
    /// </summary>
    public static class RebarFeatureExtractor
    {
        private static readonly HashSet<int> s_standardDiameters = new HashSet<int>
        {
            6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32
        };

        public static string ExtractFeatures(string text, int tokenStart, int tokenLength, int numericValue)
        {
            var sb = new StringBuilder();

            AppendPrevFeature(sb, text, tokenStart);
            AppendNextFeature(sb, text, tokenStart + tokenLength);

            bool afterL = IsAfterL(text, tokenStart);
            sb.Append(afterL ? "AFTERL_1 " : "AFTERL_0 ");

            bool inRing = IsInsideBrackets(text, tokenStart);
            sb.Append(inRing ? "INRING_1 " : "INRING_0 ");

            bool isStandardDk = s_standardDiameters.Contains(numericValue);
            sb.Append(isStandardDk ? "DKCHUAN_1 " : "DKCHUAN_0 ");

            int nDigits = tokenLength;
            sb.Append("NDIGIT_").Append(nDigits).Append(" ");

            sb.Append("INSEQ_0 ");

            bool beforeD = IsBeforeDiameter(text, tokenStart + tokenLength);
            sb.Append(beforeD ? "BEFORED_1 " : "BEFORED_0 ");

            bool afterD = IsAfterDiameter(text, tokenStart);
            sb.Append(afterD ? "AFTERD_1 " : "AFTERD_0 ");

            bool shLet = HasLetterSuffix(text, tokenStart + tokenLength);
            sb.Append(shLet ? "SHLET_1" : "SHLET_0");

            return sb.ToString().TrimEnd();
        }

        private static void AppendPrevFeature(StringBuilder sb, string text, int start)
        {
            if (start <= 0) { sb.Append("PREV_edge "); return; }
            int p = start - 1;
            while (p >= 0 && char.IsWhiteSpace(text[p])) p--;
            if (p < 0) { sb.Append("PREV_edge "); return; }

            char c = text[p];
            if (c == '[') sb.Append("PREV_lbr ");
            else if (c == ']') sb.Append("PREV_rbr ");
            else if (c == '∅' || c == 'd' || c == 'D' || c == '\u03a6' || c == '\u03c6') sb.Append("PREV_d ");
            else if (c == 'x' || c == 'X' || c == '*') sb.Append("PREV_x ");
            else if ((c == 'a' || c == 'A' || c == '@') && (p == 0 || !char.IsLetter(text[p - 1]))) sb.Append("PREV_spc ");
            else if (c == ',') sb.Append("PREV_comma ");
            else if (c == '=' || c == ':' || c == '+') sb.Append("PREV_sym ");
            else sb.Append("PREV_other ");
        }

        private static void AppendNextFeature(StringBuilder sb, string text, int end)
        {
            if (end >= text.Length) { sb.Append("NEXT_edge "); return; }
            int p = end;
            while (p < text.Length && char.IsWhiteSpace(text[p])) p++;
            if (p >= text.Length) { sb.Append("NEXT_edge "); return; }

            char c = text[p];
            if (c == 'a') sb.Append("NEXT_a ");
            else if (c == 'b') sb.Append("NEXT_b ");
            else if (c == 'c') sb.Append("NEXT_c ");
            else if (c == 'x' || c == 'X' || c == '*') sb.Append("NEXT_x ");
            else if (c == ',') sb.Append("NEXT_comma ");
            else if (c == '∅' || c == 'd' || c == 'D') sb.Append("NEXT_d ");
            else sb.Append("NEXT_other ");
        }

        private static bool IsAfterL(string text, int start)
        {
            int lIdx = text.LastIndexOf('L', Math.Max(0, start - 1));
            if (lIdx < 0) lIdx = text.LastIndexOf('l', Math.Max(0, start - 1));
            return lIdx >= 0;
        }

        private static bool IsInsideBrackets(string text, int pos)
        {
            int lbr = text.LastIndexOf('[', pos);
            if (lbr < 0) return false;
            int rbr = text.IndexOf(']', lbr);
            return rbr > pos;
        }

        private static bool IsBeforeDiameter(string text, int end)
        {
            int p = end;
            while (p < text.Length && char.IsWhiteSpace(text[p])) p++;
            if (p < text.Length && (text[p] == '∅' || text[p] == 'd' || text[p] == 'D' || text[p] == '\u03a6')) return true;
            if (p < text.Length && (text[p] == 'x' || text[p] == 'X' || text[p] == '*'))
            {
                p++;
                while (p < text.Length && char.IsWhiteSpace(text[p])) p++;
                return p < text.Length && (text[p] == '∅' || text[p] == 'd' || text[p] == 'D' || text[p] == '\u03a6');
            }
            return false;
        }

        private static bool IsAfterDiameter(string text, int start)
        {
            int p = start - 1;
            while (p >= 0 && char.IsWhiteSpace(text[p])) p--;
            return p >= 0 && (text[p] == '∅' || text[p] == 'd' || text[p] == 'D' || text[p] == '\u03a6' || text[p] == '\u03c6');
        }

        private static bool HasLetterSuffix(string text, int end)
        {
            if (end >= text.Length) return false;
            char c = text[end];
            if (!char.IsLetter(c) || c == 'x' || c == 'X') return false;
            // If the letter is 'a' followed by digits (e.g. a150), it is spacing, not a mark suffix
            if ((c == 'a' || c == 'A') && end + 1 < text.Length && char.IsDigit(text[end + 1]))
            {
                return false;
            }
            return true;
        }
    }
}
