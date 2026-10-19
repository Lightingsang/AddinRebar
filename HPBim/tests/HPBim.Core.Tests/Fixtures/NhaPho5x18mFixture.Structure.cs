namespace HPBim.Core.Tests.Fixtures;

using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class NhaPho5x18mFixture
{
    public static IReadOnlyList<BeamDef> CreateBeams()
    {
        string[] levels = ["LAU 1", "LAU 2", "SAN THUONG"];
        var beams = new List<BeamDef>(24);
        foreach (var lvl in levels)
        {
            double[] xs = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
            for (int i = 0; i < xs.Length; i++)
            {
                beams.Add(new BeamDef
                {
                    Id = $"D1_{lvl}_{i + 1}",
                    StartPoint = new Point2D(xs[i], 0.0),
                    EndPoint = new Point2D(xs[i], 5000.0),
                    WidthMm = 200.0,
                    HeightMm = 400.0,
                    Level = lvl,
                    BeamType = "D1"
                });
            }

            beams.Add(new BeamDef { Id = $"D2_{lvl}_A", StartPoint = new Point2D(0, 0), EndPoint = new Point2D(18000, 0), WidthMm = 200, HeightMm = 300, Level = lvl, BeamType = "D2" });
            beams.Add(new BeamDef { Id = $"D2_{lvl}_B", StartPoint = new Point2D(0, 5000), EndPoint = new Point2D(18000, 5000), WidthMm = 200, HeightMm = 300, Level = lvl, BeamType = "D2" });
            beams.Add(new BeamDef { Id = $"D3_{lvl}_1", StartPoint = new Point2D(0, 2500), EndPoint = new Point2D(5500, 2500), WidthMm = 200, HeightMm = 300, Level = lvl, BeamType = "D3" });
        }
        return beams;
    }

    public static IReadOnlyList<SlabDef> CreateSlabs()
    {
        string[] levels = ["LAU 1", "LAU 2", "SAN THUONG"];
        var slabs = new List<SlabDef>(12);
        foreach (var lvl in levels)
        {
            slabs.Add(new SlabDef { Id = $"S1_{lvl}", Boundary = new Polygon2D([Pt(0, 0), Pt(5500, 0), Pt(5500, 2500), Pt(0, 2500)]), ThicknessMm = 100, Level = lvl });
            slabs.Add(new SlabDef { Id = $"S2_{lvl}", Boundary = new Polygon2D([Pt(0, 2500), Pt(5500, 2500), Pt(5500, 5000), Pt(0, 5000)]), ThicknessMm = 100, Level = lvl });
            slabs.Add(new SlabDef { Id = $"S3_{lvl}", Boundary = new Polygon2D([Pt(5500, 0), Pt(9500, 0), Pt(9500, 5000), Pt(5500, 5000)]), ThicknessMm = 100, Level = lvl });
            slabs.Add(new SlabDef { Id = $"S4_{lvl}", Boundary = new Polygon2D([Pt(13000, 0), Pt(18000, 0), Pt(18000, 5000), Pt(13000, 5000)]), ThicknessMm = 100, Level = lvl });
        }
        return slabs;
    }

    public static IReadOnlyList<RebarDef> CreateRebars()
    {
        var rebars = new List<RebarDef>(33);
        rebars.AddRange(CreateFoundationRebars());
        rebars.AddRange(CreateSuperstructureRebars());
        rebars.AddRange(CreateStairRebars());
        return rebars;
    }

    private static IReadOnlyList<RebarDef> CreateFoundationRebars() =>
    [
        new() { Id = "RB-M-01", HostElementId = "M1_M2", BarMark = 1, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 1620, Quantity = 40, CoverMm = 50, Zone = "Foundation" },
        new() { Id = "RB-M-02", HostElementId = "M1_M2", BarMark = 2, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 820, Quantity = 80, CoverMm = 50, Zone = "Foundation" },
        new() { Id = "RB-M-03", HostElementId = "GM1_GM2", BarMark = 3, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 19600, Quantity = 8, CoverMm = 50, Zone = "Foundation" },
        new() { Id = "RB-M-04", HostElementId = "GM1_GM2", BarMark = 4, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 1060, Quantity = 162, CoverMm = 50, Zone = "Foundation" },
        new() { Id = "RB-M-05", HostElementId = "GM3_GM7", BarMark = 5, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 5960, Quantity = 20, CoverMm = 50, Zone = "Foundation" },
        new() { Id = "RB-M-06", HostElementId = "GM3_GM7", BarMark = 6, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 1060, Quantity = 95, CoverMm = 50, Zone = "Foundation" }
    ];

    private static IReadOnlyList<RebarDef> CreateSuperstructureRebars() =>
    [
        new() { Id = "RB-FL-01", HostElementId = "D1", BarMark = 1, DiameterMm = 18, SteelGrade = "CB300-V", LengthMm = 5200, Quantity = 45, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-02", HostElementId = "D1", BarMark = 2, DiameterMm = 18, SteelGrade = "CB300-V", LengthMm = 5200, Quantity = 45, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-03", HostElementId = "D1", BarMark = 3, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 1150, Quantity = 420, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-04", HostElementId = "D2", BarMark = 4, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 18400, Quantity = 18, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-05", HostElementId = "D2", BarMark = 5, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 18400, Quantity = 18, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-06", HostElementId = "D2", BarMark = 6, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 950, Quantity = 570, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-07", HostElementId = "D3", BarMark = 7, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 9700, Quantity = 6, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-08", HostElementId = "D3", BarMark = 8, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 950, Quantity = 150, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-09", HostElementId = "S1_S2", BarMark = 9, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 5800, Quantity = 102, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-10", HostElementId = "S1_S2", BarMark = 10, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 2800, Quantity = 222, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-11", HostElementId = "S1_S2", BarMark = 11, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 1420, Quantity = 648, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-12", HostElementId = "S3", BarMark = 12, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 4300, Quantity = 102, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-13", HostElementId = "S3", BarMark = 13, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 5300, Quantity = 81, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-14", HostElementId = "S3", BarMark = 14, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 1420, Quantity = 366, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-15", HostElementId = "S4", BarMark = 15, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 5300, Quantity = 102, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-16", HostElementId = "S4", BarMark = 16, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 5300, Quantity = 102, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-17", HostElementId = "S4", BarMark = 17, DiameterMm = 10, SteelGrade = "CB240-T", LengthMm = 1420, Quantity = 408, CoverMm = 15, Zone = "Superstructure" },
        new() { Id = "RB-FL-18", HostElementId = "C1", BarMark = 18, DiameterMm = 18, SteelGrade = "CB300-V", LengthMm = 3800, Quantity = 120, CoverMm = 25, Zone = "Superstructure" },
        new() { Id = "RB-FL-19", HostElementId = "C1", BarMark = 19, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 800, Quantity = 660, CoverMm = 25, Zone = "Superstructure" }
    ];

    private static IReadOnlyList<RebarDef> CreateStairRebars() =>
    [
        new() { Id = "RB-ST-01", HostElementId = "BT1", BarMark = 1, DiameterMm = 10, SteelGrade = "CB240-T", ShapeType = RebarShapeType.HookEnd, LengthMm = 3800, Quantity = 24, CoverMm = 20, Zone = "Stair" },
        new() { Id = "RB-ST-02", HostElementId = "BT1", BarMark = 2, DiameterMm = 8, SteelGrade = "CB240-T", LengthMm = 1100, Quantity = 51, CoverMm = 20, Zone = "Stair" },
        new() { Id = "RB-ST-03", HostElementId = "BT2", BarMark = 3, DiameterMm = 10, SteelGrade = "CB240-T", ShapeType = RebarShapeType.HookEnd, LengthMm = 3800, Quantity = 24, CoverMm = 20, Zone = "Stair" },
        new() { Id = "RB-ST-04", HostElementId = "BT2", BarMark = 4, DiameterMm = 8, SteelGrade = "CB240-T", LengthMm = 1100, Quantity = 51, CoverMm = 20, Zone = "Stair" },
        new() { Id = "RB-ST-05", HostElementId = "CN", BarMark = 5, DiameterMm = 10, SteelGrade = "CB240-T", ShapeType = RebarShapeType.HookEnd, LengthMm = 2600, Quantity = 27, CoverMm = 20, Zone = "Stair" },
        new() { Id = "RB-ST-06", HostElementId = "DCT", BarMark = 6, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 3000, Quantity = 12, CoverMm = 25, Zone = "Stair" },
        new() { Id = "RB-ST-07", HostElementId = "DCN", BarMark = 7, DiameterMm = 16, SteelGrade = "CB300-V", LengthMm = 3000, Quantity = 12, CoverMm = 25, Zone = "Stair" },
        new() { Id = "RB-ST-08", HostElementId = "DCT_DCN", BarMark = 8, DiameterMm = 8, SteelGrade = "CB240-T", ShapeType = RebarShapeType.ClosedStirrup, LengthMm = 900, Quantity = 162, CoverMm = 25, Zone = "Stair" }
    ];
}
