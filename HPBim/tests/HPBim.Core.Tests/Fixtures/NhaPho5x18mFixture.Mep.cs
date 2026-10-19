namespace HPBim.Core.Tests.Fixtures;

using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public static partial class NhaPho5x18mFixture
{
    public static IReadOnlyList<MepFixtureDef> CreateFixtures() =>
    [
        // Tang 1
        new() { Id = "FX_1_01", FamilyName = "XE-OTO", Category = MepCategory.Furniture, Location = new Point3D(2600, 2500, 150), RotationDeg = 90, Level = "TRET" },
        new() { Id = "FX_1_02", FamilyName = "XE-MAY", Category = MepCategory.Furniture, Location = new Point3D(4600, 900, 150), RotationDeg = 90, Level = "TRET" },
        new() { Id = "FX_1_03", FamilyName = "SOFA", Category = MepCategory.Furniture, Location = new Point3D(7300, 4400, 150), RotationDeg = 180, Level = "TRET" },
        new() { Id = "FX_1_04", FamilyName = "BAN-TRA", Category = MepCategory.Furniture, Location = new Point3D(7300, 3300, 150), RotationDeg = 0, Level = "TRET" },
        new() { Id = "FX_1_05", FamilyName = "TIVI", Category = MepCategory.Electrical, Location = new Point3D(7300, 700, 150), RotationDeg = 0, Level = "TRET" },
        new() { Id = "FX_1_06", FamilyName = "BON-CAU", Category = MepCategory.Sanitary, Location = new Point3D(10100, 800, 150), RotationDeg = 90, Level = "TRET" },
        new() { Id = "FX_1_07", FamilyName = "LAVABO", Category = MepCategory.Sanitary, Location = new Point3D(11000, 500, 150), RotationDeg = 0, Level = "TRET" },
        new() { Id = "FX_1_08", FamilyName = "BEP-NAU", Category = MepCategory.Kitchen, Location = new Point3D(14200, 4600, 150), RotationDeg = 180, Level = "TRET" },
        new() { Id = "FX_1_09", FamilyName = "CHAU-RUA-CHEN", Category = MepCategory.Sanitary, Location = new Point3D(15400, 4600, 150), RotationDeg = 180, Level = "TRET" },
        new() { Id = "FX_1_10", FamilyName = "TU-LANH", Category = MepCategory.Kitchen, Location = new Point3D(13600, 4600, 150), RotationDeg = 180, Level = "TRET" },
        new() { Id = "FX_1_11", FamilyName = "BAN-AN-6", Category = MepCategory.Furniture, Location = new Point3D(15900, 2200, 150), RotationDeg = 90, Level = "TRET" },

        // Tang 2
        new() { Id = "FX_2_01", FamilyName = "GIUONG-DOI", Category = MepCategory.Furniture, Location = new Point3D(4200, 2500, 3800), RotationDeg = 90, Level = "LAU 1" },
        new() { Id = "FX_2_02", FamilyName = "TU-QUAN-AO", Category = MepCategory.Furniture, Location = new Point3D(6900, 2500, 3800), RotationDeg = 90, Level = "LAU 1" },
        new() { Id = "FX_2_03", FamilyName = "BON-CAU", Category = MepCategory.Sanitary, Location = new Point3D(11000, 700, 3800), RotationDeg = 90, Level = "LAU 1" },
        new() { Id = "FX_2_04", FamilyName = "LAVABO", Category = MepCategory.Sanitary, Location = new Point3D(12400, 500, 3800), RotationDeg = 0, Level = "LAU 1" },
        new() { Id = "FX_2_05", FamilyName = "VOI-SEN", Category = MepCategory.Sanitary, Location = new Point3D(11200, 1800, 3800), RotationDeg = 0, Level = "LAU 1" },
        new() { Id = "FX_2_06", FamilyName = "PHEU-THU", Category = MepCategory.Sanitary, Location = new Point3D(12000, 1500, 3800), RotationDeg = 0, Level = "LAU 1" },
        new() { Id = "FX_2_07", FamilyName = "GIUONG-DOI", Category = MepCategory.Furniture, Location = new Point3D(15200, 2500, 3800), RotationDeg = 90, Level = "LAU 1" },
        new() { Id = "FX_2_08", FamilyName = "TU-QUAN-AO", Category = MepCategory.Furniture, Location = new Point3D(17400, 2500, 3800), RotationDeg = 90, Level = "LAU 1" },
        new() { Id = "FX_2_09", FamilyName = "TIVI", Category = MepCategory.Electrical, Location = new Point3D(13400, 2500, 3800), RotationDeg = 90, Level = "LAU 1" },

        // Tang 3
        new() { Id = "FX_3_01", FamilyName = "GIUONG-DOI", Category = MepCategory.Furniture, Location = new Point3D(14000, 3900, 7200), RotationDeg = 0, Level = "LAU 2" },
        new() { Id = "FX_3_02", FamilyName = "TU-QUAN-AO", Category = MepCategory.Furniture, Location = new Point3D(13400, 2600, 7200), RotationDeg = 90, Level = "LAU 2" },
        new() { Id = "FX_3_03", FamilyName = "BON-CAU", Category = MepCategory.Sanitary, Location = new Point3D(16300, 700, 7200), RotationDeg = 90, Level = "LAU 2" },
        new() { Id = "FX_3_04", FamilyName = "LAVABO", Category = MepCategory.Sanitary, Location = new Point3D(17300, 500, 7200), RotationDeg = 0, Level = "LAU 2" },
        new() { Id = "FX_3_05", FamilyName = "VOI-SEN", Category = MepCategory.Sanitary, Location = new Point3D(16600, 1600, 7200), RotationDeg = 0, Level = "LAU 2" },
        new() { Id = "FX_3_06", FamilyName = "PHEU-THU", Category = MepCategory.Sanitary, Location = new Point3D(17400, 1400, 7200), RotationDeg = 0, Level = "LAU 2" }
    ];

    public static IReadOnlyList<PipeRunDef> CreatePipes() =>
    [
        new() { Id = "P_SUP_T1", SystemType = PipeSystemType.WaterSupply, DiameterMm = 25, Material = "PPR", Level = "TRET", Points = [new Point3D(600, 4600, 300), new Point3D(14000, 4600, 300), new Point3D(14000, 4200, 300)] },
        new() { Id = "P_SOIL_T1", SystemType = PipeSystemType.SoilDrain, DiameterMm = 114, Material = "uPVC", Level = "TRET", Slope = 0.02, Points = [new Point3D(10100, 900, -200), new Point3D(16500, 900, -300), new Point3D(16500, 300, -350)] },
        new() { Id = "P_WASTE_T1", SystemType = PipeSystemType.WasteDrain, DiameterMm = 60, Material = "uPVC", Level = "TRET", Slope = 0.02, Points = [new Point3D(11000, 400, -200), new Point3D(15900, 400, -300), new Point3D(15900, 4400, -350)] }
    ];
}
