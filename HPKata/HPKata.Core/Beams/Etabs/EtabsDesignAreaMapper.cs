using System;
using System.Collections.Generic;

namespace HPKata.Core.Beams.Etabs
{
    /// <summary>
    /// Reinforcement demand and internal forces at a specific station along an ETABS frame.
    /// </summary>
    public sealed class EtabsStationData
    {
        public double StationMm { get; set; }
        public double AsTopMm2 { get; set; }
        public double AsBotMm2 { get; set; }
        public double AvOverSMm2PerMm { get; set; }
        public double Moment3KnM { get; set; }
        public double Shear2Kn { get; set; }
    }

    /// <summary>
    /// Frame element with aggregated design results across all stations.
    /// </summary>
    public sealed class EtabsFrameDesign
    {
        public string FrameName { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public double LengthMm { get; set; }
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public List<EtabsStationData> Stations { get; } = new List<EtabsStationData>();

        public double AsTopLeftRequired => GetStationAsTop(0.0);
        public double AsTopMidRequired => GetStationAsTop(LengthMm / 2.0);
        public double AsTopRightRequired => GetStationAsTop(LengthMm);

        public double AsBotLeftRequired => GetStationAsBot(0.0);
        public double AsBotMidRequired => GetStationAsBot(LengthMm / 2.0);
        public double AsBotRightRequired => GetStationAsBot(LengthMm);

        public double AvOverSMax => GetMaxAvOverS();

        private double GetStationAsTop(double targetStationMm)
        {
            if (Stations.Count == 0) return 0.0;
            EtabsStationData closest = Stations[0];
            double minDiff = Math.Abs(closest.StationMm - targetStationMm);

            foreach (var st in Stations)
            {
                double diff = Math.Abs(st.StationMm - targetStationMm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = st;
                }
            }
            return closest.AsTopMm2;
        }

        private double GetStationAsBot(double targetStationMm)
        {
            if (Stations.Count == 0) return 0.0;
            EtabsStationData closest = Stations[0];
            double minDiff = Math.Abs(closest.StationMm - targetStationMm);

            foreach (var st in Stations)
            {
                double diff = Math.Abs(st.StationMm - targetStationMm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = st;
                }
            }
            return closest.AsBotMm2;
        }

        private double GetMaxAvOverS()
        {
            double max = 0.0;
            foreach (var st in Stations)
            {
                if (st.AvOverSMm2PerMm > max) max = st.AvOverSMm2PerMm;
            }
            return max;
        }
    }

    /// <summary>
    /// Maps parsed ETABS text table rows into structured EtabsFrameDesign records.
    /// </summary>
    public static class EtabsDesignAreaMapper
    {
        private readonly struct ColumnIndices
        {
            public int Story { get; init; }
            public int Beam { get; init; }
            public int Station { get; init; }
            public int TopArea { get; init; }
            public int BotArea { get; init; }
            public int Shear { get; init; }
        }

        public static Dictionary<string, EtabsFrameDesign> MapDesignTable(List<string[]> rows)
        {
            var frames = new Dictionary<string, EtabsFrameDesign>(StringComparer.OrdinalIgnoreCase);
            if (rows.Count < 2) return frames;

            var cols = ResolveIndices(rows[0]);
            for (int i = 1; i < rows.Count; i++)
            {
                ProcessRow(rows[i], cols, frames);
            }

            return frames;
        }

        private static ColumnIndices ResolveIndices(string[] header) =>
            new()
            {
                Story = FindColumnIndex(header, "Story"),
                Beam = FindColumnIndex(header, "Beam", "Frame", "Line", "Element"),
                Station = FindColumnIndex(header, "Station", "Loc", "Location"),
                TopArea = FindColumnIndex(header, "TopArea", "Top Area", "AsTop"),
                BotArea = FindColumnIndex(header, "BotArea", "Bot Area", "AsBot"),
                Shear = FindColumnIndex(header, "ShearRebar", "VRebar", "Av/s", "Av_s")
            };

        private static void ProcessRow(
            string[] row, ColumnIndices cols, Dictionary<string, EtabsFrameDesign> frames)
        {
            if (cols.Beam < 0 || cols.Beam >= row.Length) return;
            string frameName = row[cols.Beam];
            if (string.IsNullOrWhiteSpace(frameName)) return;

            if (!frames.TryGetValue(frameName, out var frame))
            {
                frame = new EtabsFrameDesign
                {
                    FrameName = frameName,
                    Story = cols.Story >= 0 && cols.Story < row.Length ? row[cols.Story] : string.Empty
                };
                frames[frameName] = frame;
            }

            double station = ParseDoubleCol(row, cols.Station);
            frame.Stations.Add(new EtabsStationData
            {
                StationMm = station,
                AsTopMm2 = ParseDoubleCol(row, cols.TopArea),
                AsBotMm2 = ParseDoubleCol(row, cols.BotArea),
                AvOverSMm2PerMm = ParseDoubleCol(row, cols.Shear)
            });

            if (station > frame.LengthMm) frame.LengthMm = station;
        }

        private static double ParseDoubleCol(string[] row, int idx) =>
            idx >= 0 && idx < row.Length ? EtabsTextTableParser.ParseDoubleInvariant(row[idx]) : 0.0;

        private static int FindColumnIndex(string[] header, params string[] candidateNames)
        {
            for (int i = 0; i < header.Length; i++)
            {
                string col = header[i].Replace(" ", "").Replace("_", "");
                foreach (string candidate in candidateNames)
                {
                    string cand = candidate.Replace(" ", "").Replace("_", "");
                    if (string.Equals(col, cand, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }
}
