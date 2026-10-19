using System;

namespace HPKata.Core.Columns.Models
{
    /// <summary>
    /// Represents starter dowels anchored into footings or lower columns where size step-down exceeds 50mm.
    /// </summary>
    public sealed class ColumnStarterDowelModel
    {
        public string DowelMark { get; set; } = string.Empty;
        public int DiameterMm { get; set; } = 20;
        public int Count { get; set; } = 4;

        public double EmbedmentDepthMm { get; set; } = 800.0;
        public double ProjectionLengthMm { get; set; } = 900.0;
        public double TotalLengthMm => EmbedmentDepthMm + ProjectionLengthMm;

        public bool HasHook90 { get; set; } = true;
        public double HookLengthMm { get; set; } = 250.0;

        public double TotalCuttingLengthMm =>
            TotalLengthMm + (HasHook90 ? HookLengthMm : 0.0);
    }
}
