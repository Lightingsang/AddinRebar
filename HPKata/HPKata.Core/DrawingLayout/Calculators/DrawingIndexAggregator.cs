using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HPKata.Core.DrawingLayout.Models;

namespace HPKata.Core.DrawingLayout.Calculators
{
    /// <summary>
    /// Master drawing register aggregator with natural alphanumeric sorting,
    /// duplicate detection, sequence gap analysis, and table DTO generation.
    /// </summary>
    public static class DrawingIndexAggregator
    {
        public static DrawingIndexTableDto BuildTable(IEnumerable<DrawingIndexItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var table = new DrawingIndexTableDto();
            var sortedList = items.OrderBy(i => i.SheetNumber, new NaturalStringComparer()).ToList();

            CheckDuplicatesAndGaps(sortedList, table.Warnings);

            for (int i = 0; i < sortedList.Count; i++)
            {
                var it = sortedList[i];
                string[] row =
                {
                    (i + 1).ToString(),
                    it.SheetNumber,
                    it.DrawingTitle,
                    it.Scale,
                    it.Revision,
                    it.Category
                };
                table.Rows.Add(row);
            }

            return table;
        }

        private static void CheckDuplicatesAndGaps(List<DrawingIndexItem> sorted, List<string> warnings)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int lastNum = -1;

            foreach (var item in sorted)
            {
                if (!seen.Add(item.SheetNumber))
                {
                    warnings.Add($"Trùng số hiệu bản vẽ: {item.SheetNumber}");
                }

                int num = ExtractTrailingNumber(item.SheetNumber);
                if (num > 0)
                {
                    if (lastNum > 0 && num > lastNum + 1)
                    {
                        warnings.Add($"Gián đoạn số hiệu bản vẽ giữa {lastNum} và {num}");
                    }
                    lastNum = num;
                }
            }
        }

        private static int ExtractTrailingNumber(string input)
        {
            var match = Regex.Match(input, @"\d+$");
            return match.Success && int.TryParse(match.Value, out int n) ? n : -1;
        }

        private sealed class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                var regex = new Regex(@"(\d+)|(\D+)");
                var matchesX = regex.Matches(x);
                var matchesY = regex.Matches(y);

                int count = Math.Min(matchesX.Count, matchesY.Count);
                for (int i = 0; i < count; i++)
                {
                    string strX = matchesX[i].Value;
                    string strY = matchesY[i].Value;

                    if (int.TryParse(strX, out int numX) && int.TryParse(strY, out int numY))
                    {
                        int numCompare = numX.CompareTo(numY);
                        if (numCompare != 0) return numCompare;
                    }
                    else
                    {
                        int strCompare = string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
                        if (strCompare != 0) return strCompare;
                    }
                }

                return matchesX.Count.CompareTo(matchesY.Count);
            }
        }
    }
}
