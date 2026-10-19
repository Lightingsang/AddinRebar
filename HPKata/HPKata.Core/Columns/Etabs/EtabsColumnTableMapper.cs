using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Etabs;

namespace HPKata.Core.Columns.Etabs
{
    /// <summary>
    /// Maps delimited ETABS column design table output into typed EtabsColumnDesign records.
    /// </summary>
    public static class EtabsColumnTableMapper
    {
        public static List<EtabsColumnDesign> MapTableContent(string textContent)
        {
            var results = new List<EtabsColumnDesign>();
            var rows = EtabsTextTableParser.ParseDelimitedRows(textContent);
            if (rows.Count < 2) return results;

            int storyCol = FindColumnIndex(rows[0], "story", "tang");
            int markCol = FindColumnIndex(rows[0], "column", "col", "mark", "id");
            int widthCol = FindColumnIndex(rows[0], "width", "b", "dimb");
            int depthCol = FindColumnIndex(rows[0], "depth", "h", "dimh");
            int pCol = FindColumnIndex(rows[0], "p", "axial");
            int m2Col = FindColumnIndex(rows[0], "m2", "mmajor");
            int m3Col = FindColumnIndex(rows[0], "m3", "mminor");
            int astCol = FindColumnIndex(rows[0], "ast", "rebar", "area", "as_req");
            int avsCol = FindColumnIndex(rows[0], "av/s", "avs", "shear");

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Length == 0) continue;

                var design = CreateDesignFromRow(row, storyCol, markCol, widthCol, depthCol, pCol, m2Col, m3Col, astCol, avsCol);
                if (design != null)
                {
                    results.Add(design);
                }
            }

            return results;
        }

        private static int FindColumnIndex(string[] headerRow, params string[] candidates)
        {
            for (int i = 0; i < headerRow.Length; i++)
            {
                string h = headerRow[i].Trim().ToLowerInvariant();
                foreach (string c in candidates)
                {
                    if (h == c) return i;
                }
            }

            for (int i = 0; i < headerRow.Length; i++)
            {
                string h = headerRow[i].Trim().ToLowerInvariant();
                foreach (string c in candidates)
                {
                    if (c.Length > 1 && h.Contains(c)) return i;
                }
            }
            return -1;
        }

        private static EtabsColumnDesign? CreateDesignFromRow(
            string[] row, int storyCol, int markCol, int widthCol, int depthCol,
            int pCol, int m2Col, int m3Col, int astCol, int avsCol)
        {
            string story = storyCol >= 0 && storyCol < row.Length ? row[storyCol] : "Story1";
            string mark = markCol >= 0 && markCol < row.Length ? row[markCol] : "C1";

            if (string.IsNullOrWhiteSpace(mark)) return null;

            double width = widthCol >= 0 && widthCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[widthCol], 400.0) : 400.0;
            double depth = depthCol >= 0 && depthCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[depthCol], 400.0) : 400.0;
            double p = pCol >= 0 && pCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[pCol], 0.0) : 0.0;
            double m2 = m2Col >= 0 && m2Col < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[m2Col], 0.0) : 0.0;
            double m3 = m3Col >= 0 && m3Col < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[m3Col], 0.0) : 0.0;
            double ast = astCol >= 0 && astCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[astCol], 0.0) : 0.0;
            double avs = avsCol >= 0 && avsCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[avsCol], 0.0) : 0.0;

            return new EtabsColumnDesign
            {
                StoryName = story,
                ColumnMark = mark,
                WidthMm = width,
                DepthMm = depth,
                AxialForceP_kN = p,
                MomentM2_kNm = m2,
                MomentM3_kNm = m3,
                RequiredAstMm2 = ast,
                RequiredAvPerSMm2PerM = avs
            };
        }
    }
}
