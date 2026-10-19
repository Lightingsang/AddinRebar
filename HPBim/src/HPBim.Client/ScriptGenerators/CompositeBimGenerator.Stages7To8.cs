namespace HPBim.Client.ScriptGenerators;

public sealed partial class CompositeBimGenerator
{
    private static string GetStage7MepScript() => """
        // --- STAGE 7: MEP ---
        var fixtureSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
            .Cast<FamilySymbol>()
            .ToList();

        var defaultFixtureSym = fixtureSymbols.FirstOrDefault();
        if (defaultFixtureSym != null && !defaultFixtureSym.IsActive) defaultFixtureSym.Activate();

        var fixtureItems = args.List("fixtures");
        int countFixtures = 0;
        foreach (var fx in fixtureItems)
        {
            if (defaultFixtureSym == null) break;
            string lvlName = fx.Require("level");
            if (!existingLevels.TryGetValue(lvlName, out var lvl)) continue;

            var pt = ToXyz(fx.Double("x"), fx.Double("y"), fx.Double("z"));
            var inst = doc.Create.NewFamilyInstance(pt, defaultFixtureSym, lvl, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(fx.Require("id"));
            countFixtures++;
        }

        var pipingSysTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipingSystemType>(StringComparer.OrdinalIgnoreCase);
        foreach (Autodesk.Revit.DB.Plumbing.PipingSystemType pst in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)))
        {
            if (!pipingSysTypes.ContainsKey(pst.Name)) pipingSysTypes[pst.Name] = pst;
        }

        var pipeTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipeType>(StringComparer.OrdinalIgnoreCase);
        foreach (Autodesk.Revit.DB.Plumbing.PipeType pt in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)))
        {
            if (!pipeTypes.ContainsKey(pt.Name)) pipeTypes[pt.Name] = pt;
        }

        string reqSysName = args.Str("pipingSystemType", "Domestic Cold Water");
        string reqPipeName = args.Str("pipeType", "Standard");

        pipingSysTypes.TryGetValue(reqSysName, out var pipingSystemType);
        pipeTypes.TryGetValue(reqPipeName, out var pipeType);

        var pipeItems = args.List("pipes");
        int countPipes = 0;
        if (pipeItems.Count > 0 && (pipingSystemType == null || pipeType == null))
        {
            throw new InvalidOperationException($"[ERR_SYMBOL_NOT_FOUND] PipingSystemType '{reqSysName}' or PipeType '{reqPipeName}' not found in document.");
        }

        if (pipingSystemType != null && pipeType != null)
        {
            foreach (var p in pipeItems)
            {
                string lvlName = p.Require("level");
                if (!existingLevels.TryGetValue(lvlName, out var lvl)) continue;

                var pts = p.List("points");
                if (pts.Count < 2) continue;

                double diamFt = Ft(p.Double("diameterMm", 25));
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var p0 = ToXyz(pts[i].Double("x"), pts[i].Double("y"), pts[i].Double("z"));
                    var p1 = ToXyz(pts[i + 1].Double("x"), pts[i + 1].Double("y"), pts[i + 1].Double("z"));
                    if (p0.DistanceTo(p1) < 0.01) continue;

                    var pipe = Autodesk.Revit.DB.Plumbing.Pipe.Create(doc, pipingSystemType.Id, pipeType.Id, lvl.Id, p0, p1);
                    pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(diamFt);
                    pipe.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(p.Require("id"));
                    countPipes++;
                }
            }
        }
        """;

    private static string GetStage8RebarScript() => """
        // --- STAGE 8: REBARS ---
        doc.Regenerate();
        var rebarBarTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(Autodesk.Revit.DB.Structure.RebarBarType))
            .Cast<Autodesk.Revit.DB.Structure.RebarBarType>()
            .ToList();

        var hostCandidates = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .Where(e => e.Category != null && (
                e.Category.BuiltInCategory == BuiltInCategory.OST_StructuralColumns ||
                e.Category.BuiltInCategory == BuiltInCategory.OST_StructuralFraming))
            .ToList();

        var hostsByMark = new Dictionary<string, Element>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in hostCandidates)
        {
            string mark = h.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
            if (!string.IsNullOrEmpty(mark)) hostsByMark[mark] = h;
        }

        var rebarItems = args.List("rebars");
        int countRebars = 0;
        foreach (var rb in rebarItems)
        {
            if (rebarBarTypes.Count == 0 || hostCandidates.Count == 0) break;

            double diamMm = rb.Double("diameterMm", 16);
            double diamFt = Ft(diamMm);
            var barType = rebarBarTypes.OrderBy(b => Math.Abs(b.BarModelDiameter - diamFt)).FirstOrDefault() ?? rebarBarTypes[0];

            string hostId = rb.Str("hostId", string.Empty);
            Element host = null;
            if (!string.IsNullOrEmpty(hostId)) hostsByMark.TryGetValue(hostId, out host);
            host ??= hostCandidates.FirstOrDefault();
            if (host == null) continue;

            double lenFt = Math.Max(Ft(100), Ft(rb.Double("lengthMm", 3000)));
            var bbox = host.get_BoundingBox(null);
            var origin = bbox != null ? bbox.Min : XYZ.Zero;

            var p0 = new XYZ(origin.X + Ft(30), origin.Y + Ft(30), origin.Z);
            var p1 = new XYZ(p0.X, p0.Y, p0.Z + lenFt);
            if (p0.DistanceTo(p1) < 0.01) continue;

            var curves = new List<Curve> { Line.CreateBound(p0, p1) };
            var normal = XYZ.BasisX;

            try
            {
                var rebar = Autodesk.Revit.DB.Structure.Rebar.CreateFromCurves(
                    doc,
                    Autodesk.Revit.DB.Structure.RebarStyle.Standard,
                    barType,
                    null,
                    null,
                    host,
                    normal,
                    curves,
                    Autodesk.Revit.DB.Structure.RebarHookOrientation.Left,
                    Autodesk.Revit.DB.Structure.RebarHookOrientation.Left,
                    true,
                    true);

                if (rebar != null)
                {
                    int qty = rb.Int("qty", 1);
                    if (qty > 1)
                    {
                        double spacingFt = Ft(rb.Double("spacingMm", 150));
                        rebar.GetShapeDrivenAccessor()?.SetLayoutAsNumberWithSpacing(qty, spacingFt, true, true, true);
                    }
                    countRebars++;
                }
            }
            catch (Exception)
            {
                // Fallback gracefully if host lacks valid geometry cover or boundaries.
            }
        }
        """;

    private static string GetCompositeReturnScript() => """
        return new
        {
            composite = true,
            levels = countLevels,
            grids = countGrids,
            columns = countCols,
            beams = countBeams,
            slabs = countSlabs,
            walls = countWalls,
            openings = countOpenings,
            rooms = countRooms,
            fixtures = countFixtures,
            pipes = countPipes,
            rebars = countRebars
        };
        """;
}
