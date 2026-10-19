namespace HPGrasshopper.E2E.Tests.Harness;

/// <summary>
/// Production and seed script definitions used by Tier 4 Real-World Workload tests.
/// Extracted to adhere strictly to Rule C2 (file length) and Rule M3 (method length).
/// </summary>
public static class GrasshopperWorkloadScripts
{
    public const string TowerGridScript = """
        var elevationList = args.Doubles("elevations");
        var spacingList = args.Doubles("gridSpacings");
        double lineLength = args.Double("gridLength", 30.0);
        const double feetPerMeter = 3.280839895013123;
        var createdLevels = new List<long>();
        var createdGrids = new List<long>();
        for (int levelIndex = 0; levelIndex < elevationList.Count; levelIndex++)
        {
            double elevationFeet = elevationList[levelIndex] * feetPerMeter;
            var levelInstance = Autodesk.Revit.DB.Level.Create(revitDoc, elevationFeet);
            createdLevels.Add(levelInstance.Id.Value);
        }
        double currentX = 0.0;
        for (int gridIndex = 0; gridIndex < spacingList.Count; gridIndex++)
        {
            var startPoint = new Autodesk.Revit.DB.XYZ(currentX * feetPerMeter, -lineLength * 0.5 * feetPerMeter, 0.0);
            var endPoint = new Autodesk.Revit.DB.XYZ(currentX * feetPerMeter, lineLength * 0.5 * feetPerMeter, 0.0);
            var gridCurve = Autodesk.Revit.DB.Line.CreateBound(startPoint, endPoint);
            var gridInstance = Autodesk.Revit.DB.Grid.Create(revitDoc, gridCurve);
            createdGrids.Add(gridInstance.Id.Value);
            currentX += spacingList[gridIndex];
        }
        return new { success = true, levelsCount = createdLevels.Count, gridsCount = createdGrids.Count };
        """;

    public const string FacadePanelizationScript = """
        double width = args.Double("surfaceWidthMeters", 30.0);
        double height = args.Double("surfaceHeightMeters", 15.0);
        int uDiv = args.Int("uDivisions");
        int vDiv = args.Int("vDivisions");
        string family = args.Str("panelFamilyName", "Adaptive-Quad-Panel");
        double cellW = width / Math.Max(1, uDiv);
        double cellH = height / Math.Max(1, vDiv);
        var panels = new List<object>();
        for (int v = 0; v < vDiv; v++)
        {
            for (int u = 0; u < uDiv; u++)
            {
                double xMin = u * cellW, xMax = (u + 1) * cellW;
                double zMin = v * cellH, zMax = (v + 1) * cellH;
                panels.Add(new {
                    panelIndex = (v * uDiv) + u + 1,
                    corners = new[] {
                        new { x = Math.Round(xMin, 3), z = Math.Round(zMin, 3) },
                        new { x = Math.Round(xMax, 3), z = Math.Round(zMin, 3) },
                        new { x = Math.Round(xMax, 3), z = Math.Round(zMax, 3) },
                        new { x = Math.Round(xMin, 3), z = Math.Round(zMax, 3) }
                    }
                });
            }
        }
        return new { success = true, totalPanels = panels.Count, uDiv, vDiv, targetFamily = family, panels };
        """;

    public const string BeamRebarStirrupScript = """
        long beamId = args.Long("beamElementId");
        double denseSpacing = args.Double("denseSpacingMm", 100.0);
        double sparseSpacing = args.Double("sparseSpacingMm", 200.0);
        double zoneRatio = args.Double("supportZoneRatio", 0.25);
        double span = args.Double("spanMeters", 6.0);
        double supportZone = span * zoneRatio;
        double midZone = span - (2.0 * supportZone);
        int leftCount = Math.Max(2, (int)Math.Round((supportZone * 1000.0) / denseSpacing));
        int midCount = Math.Max(2, (int)Math.Round((midZone * 1000.0) / sparseSpacing));
        int rightCount = leftCount;
        int total = leftCount + midCount + rightCount;
        return new {
            success = true, beamId, span,
            supportLeft = new { length = supportZone, count = leftCount, spacing = denseSpacing },
            midSpan = new { length = midZone, count = midCount, spacing = sparseSpacing },
            supportRight = new { length = supportZone, count = rightCount, spacing = denseSpacing },
            totalStirrups = total
        };
        """;

    public const string DryRunCurveScript = """
        var normal = new Autodesk.Revit.DB.XYZ(0, 0, 1);
        var curves = new List<Autodesk.Revit.DB.Curve>();
        curves.Add(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(0,0,0), new Autodesk.Revit.DB.XYZ(10,0,0)));
        return new { success = true, createdCurves = curves.Count };
        """;

    public const string BeamDeflectionCandidateScript = """
        double span = args.Double("spanMeters", 5.0);
        double depth = args.Double("depthMeters", 0.5);
        double deflectionLimit = span / 250.0;
        return new { span, depth, deflectionLimit, isFeasible = depth >= (span / 16.0) };
        """;

    public const string SolarRadiationScript = """
        var sun = args.Obj("sunVector");
        var surfaces = args.List("targetSurfaceNormals");
        double intensity = args.Double("directRadiationWatts", 1000.0);
        double sx = sun.Double("x"), sy = sun.Double("y"), sz = sun.Double("z");
        double slen = Math.Sqrt((sx * sx) + (sy * sy) + (sz * sz));
        if (slen > 1e-4) { sx /= slen; sy /= slen; sz /= slen; }
        var results = new List<object>();
        double totalPower = 0.0;
        for (int i = 0; i < surfaces.Count; i++)
        {
            var s = surfaces[i];
            double nx = s.Double("normalX"), ny = s.Double("normalY"), nz = s.Double("normalZ");
            double area = s.Double("area", 1.0);
            double dot = (sx * nx) + (sy * ny) + (sz * nz);
            double cosine = Math.Max(0.0, dot);
            double power = cosine * intensity * area;
            totalPower += power;
            results.Add(new { id = s.Int("id"), cosine = Math.Round(cosine, 4), power = Math.Round(power, 2) });
        }
        return new { success = true, panelCount = results.Count, totalIncidentPowerWatts = Math.Round(totalPower, 2), panels = results };
        """;
}
