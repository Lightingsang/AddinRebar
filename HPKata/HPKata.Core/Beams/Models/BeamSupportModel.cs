using System;

namespace HPKata.Core.Beams.Models
{
    /// <summary>
    /// Support boundary classification for continuous concrete beams.
    /// </summary>
    public enum BeamSupportType
    {
        ExteriorLeft = 0,
        Interior = 1,
        ExteriorRight = 2,
        CantileverTip = 3
    }

    /// <summary>
    /// Represents an individual support joint (column or girder) supporting a continuous beam.
    /// </summary>
    public sealed class BeamSupportModel
    {
        public int SupportIndex { get; set; }
        public string SupportName { get; set; } = string.Empty;
        public double WidthMm { get; set; } = 400.0;
        public double CoordinateXMm { get; set; }
        public BeamSupportType SupportType { get; set; } = BeamSupportType.Interior;
        public string ConnectingColumnName { get; set; } = string.Empty;

        public bool IsExterior =>
            SupportType == BeamSupportType.ExteriorLeft ||
            SupportType == BeamSupportType.ExteriorRight;

        public void Validate()
        {
            if (WidthMm < 0.0)
            {
                throw new ArgumentException("Support width cannot be negative.", nameof(WidthMm));
            }
        }
    }
}
