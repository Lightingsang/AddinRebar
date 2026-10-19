using System;

namespace HPDcePro.Core.MachineLearning
{
    /// <summary>
    /// Represents an individual parsed token from a rebar annotation string.
    /// </summary>
    public class RebarToken
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
        public int? NumericValue { get; set; }
        public string FeatureString { get; set; } = string.Empty;
        public RebarTokenType PredictedType { get; set; } = RebarTokenType.Unknown;
        public double Confidence { get; set; } = 1.0;

        public override string ToString() => $"Token[{Index}]: '{Text}' (Val={NumericValue}, Type={PredictedType})";
    }

    /// <summary>
    /// Structured representation of a completely decoded rebar callout string.
    /// </summary>
    public class RebarCallout
    {
        public string RawText { get; set; } = string.Empty;
        public string BarMark { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public int Diameter { get; set; } = 0;
        public int Spacing { get; set; } = 0;
        public double Length { get; set; } = 0.0;
        public double LegA { get; set; } = 0.0;
        public double LegB { get; set; } = 0.0;
        public double LegC { get; set; } = 0.0;
        public double LegD { get; set; } = 0.0;
    }
}
