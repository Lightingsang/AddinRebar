using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Takeoff
{
    public enum TakeoffElementType
    {
        Beam,
        Column,
        Wall,
        Slab,
        Footing
    }

    public enum RebarDiameterCategory
    {
        SmallLe10,   // d <= 10mm
        MediumLe18,  // 10mm < d <= 18mm
        LargeGt18    // d > 18mm
    }

    public class ElementTakeoffItem
    {
        public string StoryName { get; set; } = "Tầng điển hình";
        public TakeoffElementType ElementType { get; set; } = TakeoffElementType.Beam;
        public string Mark { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }
        public double ConcreteVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
    }

    public class RebarTakeoffItem
    {
        public string StoryName { get; set; } = "Tầng điển hình";
        public TakeoffElementType ElementType { get; set; } = TakeoffElementType.Beam;
        public string Mark { get; set; } = string.Empty;
        public double DiameterMm { get; set; }
        public double TotalLengthM { get; set; }
        public double WeightKg { get; set; }
        public RebarDiameterCategory Category { get; set; } = RebarDiameterCategory.MediumLe18;
    }

    public class StoryTakeoffSummary
    {
        public string StoryName { get; set; } = string.Empty;
        public double ConcreteVolumeM3 { get; set; }
        public double FormworkAreaM2 { get; set; }
        public double RebarKgLe10 { get; set; }
        public double RebarKgLe18 { get; set; }
        public double RebarKgGt18 { get; set; }
        public double TotalRebarKg => Math.Round(RebarKgLe10 + RebarKgLe18 + RebarKgGt18, 2);
        public double TotalRebarTons => Math.Round(TotalRebarKg / 1000.0, 3);
    }

    public class QuantityTakeoffResult
    {
        public List<ElementTakeoffItem> Elements { get; set; } = new List<ElementTakeoffItem>();
        public List<RebarTakeoffItem> Rebars { get; set; } = new List<RebarTakeoffItem>();
        public List<StoryTakeoffSummary> StorySummaries { get; set; } = new List<StoryTakeoffSummary>();

        public double TotalConcreteVolumeM3 { get; set; }
        public double TotalFormworkAreaM2 { get; set; }
        public double TotalRebarKgLe10 { get; set; }
        public double TotalRebarKgLe18 { get; set; }
        public double TotalRebarKgGt18 { get; set; }
        public double TotalRebarKg => Math.Round(TotalRebarKgLe10 + TotalRebarKgLe18 + TotalRebarKgGt18, 2);
        public double TotalRebarTons => Math.Round(TotalRebarKg / 1000.0, 3);
    }
}
