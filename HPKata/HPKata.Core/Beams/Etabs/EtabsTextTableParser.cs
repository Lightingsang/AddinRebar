using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace HPKata.Core.Beams.Etabs
{
    /// <summary>
    /// Parses delimited text tables copied from ETABS / SAP2000 or exported to text files.
    /// </summary>
    public static class EtabsTextTableParser
    {
        public static char DetectDelimiter(string headerLine)
        {
            if (headerLine.Contains("\t")) return '\t';
            if (headerLine.Contains(",")) return ',';
            if (headerLine.Contains(";")) return ';';
            return ' ';
        }

        public static List<string[]> ParseDelimitedRows(string tableContent)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrWhiteSpace(tableContent)) return rows;

            using (var reader = new StringReader(tableContent))
            {
                string? firstLine = null;
                while ((firstLine = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(firstLine)) break;
                }

                if (firstLine == null) return rows;

                char delimiter = DetectDelimiter(firstLine);
                ProcessLine(firstLine, delimiter, rows);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    ProcessLine(line, delimiter, rows);
                }
            }

            return rows;
        }

        private static void ProcessLine(string line, char delimiter, List<string[]> rows)
        {
            string[] rawParts;
            if (delimiter != ' ' && line.Contains(delimiter.ToString()))
            {
                rawParts = line.Split(delimiter);
            }
            else
            {
                char lineDelim = DetectDelimiter(line);
                if (lineDelim != ' ' && line.Contains(lineDelim.ToString()))
                {
                    rawParts = line.Split(lineDelim);
                }
                else
                {
                    rawParts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            for (int i = 0; i < rawParts.Length; i++)
            {
                rawParts[i] = rawParts[i].Trim();
            }

            if (rawParts.Length > 0)
            {
                rows.Add(rawParts);
            }
        }

        public static double ParseDoubleInvariant(string value, double defaultValue = 0.0)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            string cleaned = value.Trim();
            if (cleaned.Contains(",") && cleaned.Contains("."))
            {
                if (cleaned.LastIndexOf('.') > cleaned.LastIndexOf(','))
                {
                    cleaned = cleaned.Replace(",", string.Empty);
                }
                else
                {
                    cleaned = cleaned.Replace(".", string.Empty).Replace(',', '.');
                }
            }
            else if (cleaned.Contains(","))
            {
                cleaned = cleaned.Replace(',', '.');
            }

            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}
