using System;

namespace HPKata.Core.Columns.Models
{
    public enum ColumnSpliceType
    {
        ContinuousStraight,
        LapSplice,
        MechanicalCoupler,
        Welded
    }

    /// <summary>
    /// Describes each continuous or spliced longitudinal column rebar run.
    /// </summary>
    public sealed class ColumnBarRunModel
    {
        public string BarMark { get; set; } = string.Empty;
        public int DiameterMm { get; set; } = 20;
        public double LengthMm { get; set; }
        public double StartZMm { get; set; }
        public double EndZMm { get; set; }

        public double LocalX { get; set; }
        public double LocalY { get; set; }

        public bool HasBottomHook { get; set; }
        public double BottomHookLengthMm { get; set; }

        public bool HasTopHook { get; set; }
        public double TopHookLengthMm { get; set; }

        public bool HasBottomCrank { get; set; }
        public double BottomCrankOffsetMm { get; set; }
        public double BottomCrankLengthMm { get; set; }

        public bool HasTopCrank { get; set; }
        public double TopCrankOffsetMm { get; set; }
        public double TopCrankLengthMm { get; set; }

        public ColumnSpliceType SpliceType { get; set; } = ColumnSpliceType.LapSplice;
        public double LapLengthMm { get; set; }

        public double TotalCuttingLengthMm
        {
            get
            {
                double total = LengthMm;
                if (HasBottomHook) total += BottomHookLengthMm;
                if (HasTopHook) total += TopHookLengthMm;
                return Math.Round(total, 1);
            }
        }
    }
}
