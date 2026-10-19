namespace HPBim.Core.Tests.Fixtures;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class NhaPho5x18mFixture
{
    public const string ProjectName = "Nha Pho 5x18m (3 Tang)";

    public static BimModel CreateModel()
    {
        return new BimModel
        {
            ProjectName = ProjectName,
            Levels = CreateLevels(),
            Grids = CreateGrids(),
            Columns = CreateColumns(),
            Walls = CreateWalls(),
            Rooms = CreateRooms(),
            Beams = CreateBeams(),
            Slabs = CreateSlabs(),
            Rebars = CreateRebars(),
            Fixtures = CreateFixtures(),
            Pipes = CreatePipes()
        };
    }

    public static IReadOnlyList<LevelDef> CreateLevels() =>
    [
        new("TRET", 0.150, 3650.0, 1, true),
        new("LAU 1", 3.800, 3400.0, 2, true),
        new("LAU 2", 7.200, 3400.0, 3, true),
        new("SAN THUONG", 10.600, 2800.0, 4, true),
        new("MAI CHE THANG", 13.400, 0.0, 5, false)
    ];

    public static IReadOnlyList<AxisGrid> CreateGrids()
    {
        double[] xCoords = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        string[] xNames = ["1", "2", "3", "4", "5"];
        var grids = new List<AxisGrid>(7);

        for (int i = 0; i < xCoords.Length; i++)
        {
            grids.Add(AxisGrid.CreateOrthogonal(xNames[i], xCoords[i], GridDirection.X, 0.0, 5000.0));
        }

        grids.Add(AxisGrid.CreateOrthogonal("A", 0.0, GridDirection.Y, 0.0, 18000.0));
        grids.Add(AxisGrid.CreateOrthogonal("B", 5000.0, GridDirection.Y, 0.0, 18000.0));
        return grids;
    }

    public static IReadOnlyList<ColumnDef> CreateColumns()
    {
        double[] xCoords = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        string[] xNames = ["1", "2", "3", "4", "5"];
        double[] yCoords = [0.0, 5000.0];
        string[] yNames = ["A", "B"];
        var cols = new List<ColumnDef>(10);

        for (int i = 0; i < xCoords.Length; i++)
        {
            for (int j = 0; j < yCoords.Length; j++)
            {
                cols.Add(new ColumnDef
                {
                    Id = $"C_{xNames[i]}_{yNames[j]}",
                    CenterLocation = new Point2D(xCoords[i], yCoords[j]),
                    WidthMm = 220.0,
                    DepthMm = 220.0,
                    BaseLevel = "TRET",
                    TopLevel = "SAN THUONG",
                    Material = "Concrete - B20"
                });
            }
        }
        return cols;
    }
}
