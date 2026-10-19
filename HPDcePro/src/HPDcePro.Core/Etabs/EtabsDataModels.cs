using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Etabs
{
    public sealed class EtabsStory
    {
        public string Name { get; set; } = string.Empty;
        public double ElevationMm { get; set; }
        public double HeightMm { get; set; }
    }

    public sealed class EtabsGridLine
    {
        public string GridSystem { get; set; } = "G1";
        public string AxisId { get; set; } = string.Empty;
        public double CoordinateMm { get; set; }
        public bool IsXDirection { get; set; }
    }

    public sealed class EtabsFrameSection
    {
        public string SectionName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = "B25";
        public string ShapeType { get; set; } = "Concrete Rectangular";
        public double WidthMm { get; set; } = 300.0;
        public double DepthMm { get; set; } = 600.0;
    }

    public sealed class EtabsFrameMember
    {
        public string UniqueName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string StoryName { get; set; } = string.Empty;
        public Point3D Point1 { get; set; }
        public Point3D Point2 { get; set; }
        public double LengthMm { get; set; }
    }

    public sealed class EtabsMemberForce
    {
        public string UniqueName { get; set; } = string.Empty;
        public double StationMm { get; set; }
        public double P_Kn { get; set; }
        public double V2_Kn { get; set; }
        public double V3_Kn { get; set; }
        public double T_Knm { get; set; }
        public double M2_Knm { get; set; }
        public double M3_Knm { get; set; }
        public string StepType { get; set; } = "Max";
    }

    public sealed class EtabsRebarDesignItem
    {
        public string UniqueName { get; set; } = string.Empty;
        public double StationMm { get; set; }
        public double TopAsRequiredMm2 { get; set; }
        public double BottomAsRequiredMm2 { get; set; }
        public double ShearAsRequiredMm2PerM { get; set; }
    }

    public sealed class EtabsExchangeModel
    {
        public string ProjectName { get; set; } = "DuAnEtabs";
        public List<EtabsStory> Stories { get; } = new List<EtabsStory>();
        public List<EtabsGridLine> Grids { get; } = new List<EtabsGridLine>();
        public List<EtabsFrameSection> Sections { get; } = new List<EtabsFrameSection>();
        public List<EtabsFrameMember> Frames { get; } = new List<EtabsFrameMember>();
        public List<EtabsMemberForce> Forces { get; } = new List<EtabsMemberForce>();
        public List<EtabsRebarDesignItem> RebarDesigns { get; } = new List<EtabsRebarDesignItem>();
    }

    public enum EtabsSyncItemStatus
    {
        Matched = 1,
        Updated = 2,
        OrphanedInCad = 3,
        OrphanedInEtabs = 4
    }

    public sealed class EtabsSyncItem
    {
        public string CadMark { get; set; } = string.Empty;
        public string EtabsUniqueName { get; set; } = string.Empty;
        public EtabsSyncItemStatus Status { get; set; } = EtabsSyncItemStatus.Matched;
        public double SpatialDeviationMm { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class EtabsSyncReport
    {
        public int MatchedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int OrphanCount { get; set; }
        public List<EtabsSyncItem> Items { get; } = new List<EtabsSyncItem>();
        public List<string> Warnings { get; } = new List<string>();
    }

    public sealed class RebarArrangementResult
    {
        public int BarCount { get; set; }
        public double DiameterMm { get; set; }
        public double TotalAsMm2 { get; set; }
        public double ExcessAsMm2 { get; set; }
        public string Designation { get; set; } = string.Empty;
    }
}
