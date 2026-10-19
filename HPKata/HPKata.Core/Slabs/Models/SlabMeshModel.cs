using System;

namespace HPKata.Core.Slabs.Models
{
    public enum SlabDirection
    {
        DirectionX,
        DirectionY,
        Diagonal45,
        AntiDiagonal45
    }

    public enum SlabLayerType
    {
        Bottom1,
        Bottom2,
        TopSupport,
        Cantilever,
        CornerCrack,
        ChairStool,
        OpeningTrimmer
    }

    /// <summary>
    /// Represents an individual layer or mesh within a slab reinforcement layout.
    /// </summary>
    public class SlabMeshModel
    {
        public string LayerMark { get; set; } = "B1";
        public SlabDirection Direction { get; set; } = SlabDirection.DirectionX;
        public SlabLayerType LayerType { get; set; } = SlabLayerType.Bottom1;
        public int DiameterMm { get; set; } = 10;
        public double SpacingMm { get; set; } = 150.0;
        public double CogHookHeightMm { get; set; } = 80.0;
        public int BarCount { get; set; } = 20;
        public double CuttingLengthMm { get; set; } = 4100.0;
        public string ShapeCode { get; set; } = "02";
        public string Notes { get; set; } = string.Empty;
    }
}
