using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Etabs;

namespace HPKata.Core.Walls.Etabs
{
    /// <summary>
    /// Maps delimited ETABS shear wall pier design table output into typed EtabsPierDesign records.
    /// </summary>
    public static class EtabsWallTableMapper
    {
        public static List<EtabsPierDesign> MapTableContent(string textContent)
        {
            var results = new List<EtabsPierDesign>();
            var rows = EtabsTextTableParser.ParseDelimitedRows(textContent);
            if (rows.Count < 2) return results;

            int storyCol = FindColumnIndex(rows[0], "story", "tang");
            int pierCol = FindColumnIndex(rows[0], "pier", "wall", "mark", "id");
            int twCol = FindColumnIndex(rows[0], "thickness", "tw", "thick");
            int lwCol = FindColumnIndex(rows[0], "length", "lw", "len");
            int pCol = FindColumnIndex(rows[0], "p", "axial");
            int v2Col = FindColumnIndex(rows[0], "v2", "shear");
            int m3Col = FindColumnIndex(rows[0], "m3", "moment");
            int asbCol = FindColumnIndex(rows[0], "asb", "boundary", "edge");
            int avhCol = FindColumnIndex(rows[0], "avh", "web", "av/s");

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Length == 0) continue;

                var design = CreateDesignFromRow(row, storyCol, pierCol, twCol, lwCol, pCol, v2Col, m3Col, asbCol, avhCol);
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

        private static EtabsPierDesign? CreateDesignFromRow(
            string[] row, int storyCol, int pierCol, int twCol, int lwCol,
            int pCol, int v2Col, int m3Col, int asbCol, int avhCol)
        {
            string story = storyCol >= 0 && storyCol < row.Length ? row[storyCol] : "Story1";
            string pier = pierCol >= 0 && pierCol < row.Length ? row[pierCol] : "P1";

            if (string.IsNullOrWhiteSpace(pier)) return null;

            double tw = twCol >= 0 && twCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[twCol], 300.0) : 300.0;
            double lw = lwCol >= 0 && lwCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[lwCol], 3000.0) : 3000.0;
            double p = pCol >= 0 && pCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[pCol], 0.0) : 0.0;
            double v2 = v2Col >= 0 && v2Col < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[v2Col], 0.0) : 0.0;
            double m3 = m3Col >= 0 && m3Col < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[m3Col], 0.0) : 0.0;
            double asb = asbCol >= 0 && asbCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[asbCol], 0.0) : 0.0;
            double avh = avhCol >= 0 && avhCol < row.Length
                ? EtabsTextTableParser.ParseDoubleInvariant(row[avhCol], 0.0) : 0.0;

            return new EtabsPierDesign
            {
                StoryName = story,
                PierMark = pier,
                ThicknessMm = tw,
                LengthMm = lw,
                AxialForceP_kN = p,
                ShearV2_kN = v2,
                MomentM3_kNm = m3,
                RequiredBoundaryAsbMm2 = asb,
                RequiredShearAvhPerSMm2PerM = avh
            };
        }
    }
}
