namespace HPBim.Core.Tests.Fixtures;

using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class NhaPho5x18mFixture
{
    public static IReadOnlyList<WallDef> CreateWalls()
    {
        var walls = new List<WallDef>(26);
        walls.AddRange(CreateFloor1Walls());
        walls.AddRange(CreateFloor2Walls());
        walls.AddRange(CreateFloor3Walls());
        return walls;
    }

    private static IReadOnlyList<WallDef> CreateFloor1Walls() =>
    [
        CreateWall("W1_01", (0, 0), (18000, 0), 220, "TRET", "LAU 1", [CreateOp("W1_01_S1", 15600, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W1_02", (0, 5000), (18000, 5000), 220, "TRET", "LAU 1", [CreateOp("W1_02_S1", 6200, 1800, 1400, 900, OpeningKind.Window)]),
        CreateWall("W1_03", (0, 0), (0, 5000), 220, "TRET", "LAU 1", [CreateOp("W1_03_D1", 700, 3600, 2600, 0, OpeningKind.Opening)]),
        CreateWall("W1_04", (18000, 0), (18000, 5000), 220, "TRET", "LAU 1", [CreateOp("W1_04_D5", 1800, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W1_05", (5500, 0), (5500, 5000), 110, "TRET", "LAU 1", [CreateOp("W1_05_OP", 1400, 2200, 2400, 0, OpeningKind.Opening)]),
        CreateWall("W1_06", (9500, 0), (9500, 1900), 110, "TRET", "LAU 1", [CreateOp("W1_06_D4", 500, 800, 2100, 0, OpeningKind.Door)]),
        CreateWall("W1_07", (9500, 1900), (11400, 1900), 110, "TRET", "LAU 1", []),
        CreateWall("W1_08", (13000, 0), (13000, 5000), 110, "TRET", "LAU 1", [CreateOp("W1_08_OP", 2000, 2600, 2400, 0, OpeningKind.Opening)])
    ];

    private static IReadOnlyList<WallDef> CreateFloor2Walls() =>
    [
        CreateWall("W2_01", (0, 0), (18000, 0), 220, "LAU 1", "LAU 2", [CreateOp("W2_01_S1", 4200, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W2_02", (0, 5000), (18000, 5000), 220, "LAU 1", "LAU 2", [CreateOp("W2_02_S1", 4200, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W2_03", (0, 0), (0, 5000), 220, "LAU 1", "LAU 2", [CreateOp("W2_03_S3", 1200, 2600, 2300, 0, OpeningKind.Window)]),
        CreateWall("W2_04", (18000, 0), (18000, 5000), 220, "LAU 1", "LAU 2", [CreateOp("W2_04_S1", 1500, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W2_05", (1500, 0), (1500, 5000), 220, "LAU 1", "LAU 2", [CreateOp("W2_05_D6", 2100, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W2_06", (7800, 0), (7800, 5000), 110, "LAU 1", "LAU 2", [CreateOp("W2_06_D2", 2000, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W2_07", (13000, 0), (13000, 5000), 110, "LAU 1", "LAU 2", [CreateOp("W2_07_D2", 2000, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W2_08", (10500, 0), (10500, 2400), 110, "LAU 1", "LAU 2", [CreateOp("W2_08_D4", 900, 800, 2100, 0, OpeningKind.Door)]),
        CreateWall("W2_09", (10500, 2400), (13000, 2400), 110, "LAU 1", "LAU 2", [])
    ];

    private static IReadOnlyList<WallDef> CreateFloor3Walls() =>
    [
        CreateWall("W3_01", (0, 0), (18000, 0), 220, "LAU 2", "SAN THUONG", [CreateOp("W3_01_S1", 4200, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W3_02", (0, 5000), (18000, 5000), 220, "LAU 2", "SAN THUONG", [CreateOp("W3_02_S1", 4200, 1600, 1400, 900, OpeningKind.Window)]),
        CreateWall("W3_03", (0, 0), (0, 5000), 220, "LAU 2", "SAN THUONG", [CreateOp("W3_03_S3", 1200, 2600, 2300, 0, OpeningKind.Window)]),
        CreateWall("W3_04", (18000, 0), (18000, 5000), 220, "LAU 2", "SAN THUONG", [CreateOp("W3_04_S1", 1600, 1400, 1400, 900, OpeningKind.Window)]),
        CreateWall("W3_05", (1500, 0), (1500, 5000), 220, "LAU 2", "SAN THUONG", [CreateOp("W3_05_D6", 2100, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W3_06", (7800, 0), (7800, 5000), 110, "LAU 2", "SAN THUONG", [CreateOp("W3_06_D3", 1900, 1600, 2200, 0, OpeningKind.Door2)]),
        CreateWall("W3_07", (13000, 0), (13000, 5000), 110, "LAU 2", "SAN THUONG", [CreateOp("W3_07_D2", 2000, 900, 2200, 0, OpeningKind.Door)]),
        CreateWall("W3_08", (15600, 0), (15600, 2200), 110, "LAU 2", "SAN THUONG", [CreateOp("W3_08_D4", 700, 800, 2100, 0, OpeningKind.Door)]),
        CreateWall("W3_09", (15600, 2200), (18000, 2200), 110, "LAU 2", "SAN THUONG", [])
    ];

    private static WallDef CreateWall(
        string id, (double X, double Y) p1, (double X, double Y) p2,
        double thk, string bLevel, string tLevel, IReadOnlyList<OpeningDef> ops) =>
        new()
        {
            Id = id,
            StartPoint = new Point2D(p1.X, p1.Y),
            EndPoint = new Point2D(p2.X, p2.Y),
            ThicknessMm = thk,
            BaseLevel = bLevel,
            TopLevel = tLevel,
            Openings = ops
        };

    private static OpeningDef CreateOp(string id, double dist, double w, double h, double sill, OpeningKind kind) =>
        new()
        {
            Id = id,
            DistanceAlongWallMm = dist,
            WidthMm = w,
            HeightMm = h,
            SillHeightMm = sill,
            Kind = kind
        };

    public static IReadOnlyList<RoomDef> CreateRooms() =>
    [
        new() { Name = "Gara o to", TagLocation = new Point2D(2750, 2500), TheoreticalAreaM2 = 26.4, Level = "TRET" },
        new() { Name = "Phong khach", TagLocation = new Point2D(7500, 3300), TheoreticalAreaM2 = 19.2, Level = "TRET" },
        new() { Name = "Gieng troi + cau thang", TagLocation = new Point2D(11250, 3600), TheoreticalAreaM2 = null, Level = "TRET" },
        new() { Name = "WC 1", TagLocation = new Point2D(10600, 1700), TheoreticalAreaM2 = 3.0, Level = "TRET" },
        new() { Name = "Bep + phong an", TagLocation = new Point2D(15500, 2500), TheoreticalAreaM2 = 24.0, Level = "TRET" },

        new() { Name = "Ban cong", TagLocation = new Point2D(750, 2500), TheoreticalAreaM2 = 3.6, Level = "LAU 1", TagRotationDeg = 90 },
        new() { Name = "Phong ngu master 1", TagLocation = new Point2D(4650, 2500), TheoreticalAreaM2 = 30.5, Level = "LAU 1" },
        new() { Name = "Sanh + thong tang", TagLocation = new Point2D(9150, 3600), TheoreticalAreaM2 = null, Level = "LAU 1" },
        new() { Name = "WC 2", TagLocation = new Point2D(11750, 2100), TheoreticalAreaM2 = 5.6, Level = "LAU 1" },
        new() { Name = "Phong ngu master 2", TagLocation = new Point2D(15500, 3500), TheoreticalAreaM2 = 24.0, Level = "LAU 1" },

        new() { Name = "Ban cong", TagLocation = new Point2D(750, 2500), TheoreticalAreaM2 = 3.6, Level = "LAU 2", TagRotationDeg = 90 },
        new() { Name = "Phong tho gia tien", TagLocation = new Point2D(4650, 2500), TheoreticalAreaM2 = 30.5, Level = "LAU 2" },
        new() { Name = "Sanh + thong tang", TagLocation = new Point2D(9150, 3600), TheoreticalAreaM2 = null, Level = "LAU 2" },
        new() { Name = "Phong ngu master 3", TagLocation = new Point2D(14400, 3500), TheoreticalAreaM2 = 17.0, Level = "LAU 2" },
        new() { Name = "WC 3", TagLocation = new Point2D(16800, 1900), TheoreticalAreaM2 = 5.3, Level = "LAU 2" },
        new() { Name = "San phoi", TagLocation = new Point2D(16850, 4100), TheoreticalAreaM2 = null, Level = "LAU 2" }
    ];
}
