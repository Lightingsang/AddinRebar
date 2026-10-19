namespace HPBim.Client.Pipeline;

/// <summary>
/// Parameter-driven C# Roslyn script template executed in Revit.
/// Unifies Preview (dry-run rollback) and Apply (commit) using the identical script and plan args.
/// Enforces Rule 0: Zero guesswork, zero implicit symbol fallbacks.
/// </summary>
public static partial class BimPlanRunnerScriptTemplate
{
    public static string GetScriptCode() =>
        GetPreambleScript() +
        GetStructureScript() +
        GetArchitectureScript() +
        GetMepAndRebarScript() +
        GetLoopCloseScript() +
        GetReceiptScript() +
        GetEpilogueScript();

    private static string GetPreambleScript() =>
        GetPreambleHelpersScript() +
        GetPreamblePreFlightScript() +
        GetPreambleDatumCollectorsScript() +
        GetPreambleTypeCollectorsScript();

    private static string GetPreambleHelpersScript() => """
        // =========================================================================
        // HPBim Unified Plan Runner (Single Source of Truth IR)
        // =========================================================================
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        string NormalizeSourceKey(ScriptArgs it)
        {
            string raw = it.Str("sourceKey", string.Empty);
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith("{")) return raw;
            var obj = it.Obj("sourceKey");
            string d = obj.Str("docIdentifier", string.Empty);
            string c = obj.Str("category", string.Empty);
            string h = obj.Str("primaryHandle", string.Empty);
            return $"[{d}]#[{c}]:{h}";
        }

        """;

    private static string GetPreamblePreFlightScript() => """
        if (doc == null)
        {
            throw new InvalidOperationException(
                "[PRE_FLIGHT_FAILED] [ERR_NO_ACTIVE_DOCUMENT] No active document open in Revit.");
        }

        var targetDoc = args.Obj("targetDoc");
        string expectedTitle = targetDoc.Str("title", string.Empty);
        string expectedPath = targetDoc.Str("pathName", string.Empty);
        string expectedVersionGuidStr = targetDoc.Str("versionGuid", string.Empty);
        Guid expectedVersionGuid = Guid.TryParse(expectedVersionGuidStr, out var parsedGuid) ? parsedGuid : Guid.Empty;

        var docVer = Document.GetDocumentVersion(doc);
        Guid actualVersionGuid = docVer != null ? docVer.VersionGUID : Guid.Empty;

        bool hasExpectedTitle = !string.IsNullOrEmpty(expectedTitle);
        bool hasExpectedPath = !string.IsNullOrEmpty(expectedPath);
        bool hasExpectedVersion = expectedVersionGuid != Guid.Empty;

        if (hasExpectedTitle || hasExpectedPath || hasExpectedVersion)
        {
            bool titleMismatch = hasExpectedTitle && !string.Equals(doc.Title, expectedTitle, StringComparison.OrdinalIgnoreCase);
            bool pathMismatch = hasExpectedPath && !string.IsNullOrEmpty(doc.PathName) && !string.Equals(doc.PathName, expectedPath, StringComparison.OrdinalIgnoreCase);
            bool versionMismatch = hasExpectedVersion && actualVersionGuid != expectedVersionGuid;

            if (titleMismatch || pathMismatch)
            {
                throw new InvalidOperationException(
                    $"[PRE_FLIGHT_FAILED] [ERR_DOCUMENT_MISMATCH] Target document mismatch: expected '{expectedTitle}' (Path: '{expectedPath}', VersionGUID {expectedVersionGuid:D}), but active document is '{doc.Title}' (Path: '{doc.PathName}', VersionGUID {actualVersionGuid:D})");
            }

            if (versionMismatch)
            {
                throw new InvalidOperationException(
                    $"[PRE_FLIGHT_FAILED] [ERR_STALE_DOCUMENT_SNAPSHOT] Target document version snapshot is stale: expected VersionGUID {expectedVersionGuid:D}, but active document VersionGUID is {actualVersionGuid:D}");
            }
        }

        var items = args.List("items");
        var records = new List<Dictionary<string, object>>();

        """;

    private static string GetPreambleDatumCollectorsScript() => """
        // Pre-flight caching of datums and types
        var existingLevels = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);
        foreach (Level l in new FilteredElementCollector(doc).OfClass(typeof(Level)))
        {
            if (!existingLevels.ContainsKey(l.Name)) existingLevels[l.Name] = l;
        }

        var existingGrids = new Dictionary<string, Grid>(StringComparer.OrdinalIgnoreCase);
        foreach (Grid g in new FilteredElementCollector(doc).OfClass(typeof(Grid)))
        {
            if (!existingGrids.ContainsKey(g.Name)) existingGrids[g.Name] = g;
        }

        var existingViews = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewPlan)).Cast<ViewPlan>().ToList();

        var existingViewLevelIds = existingViews
            .Where(v => v.GenLevel != null).Select(v => v.GenLevel.Id).ToHashSet();

        var existingViewNames = new HashSet<string>(existingViews.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

        ViewFamilyType planType = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);

        var columnSymbols = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (FamilySymbol s in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralColumns))
        {
            if (!columnSymbols.ContainsKey(s.Name)) columnSymbols[s.Name] = s;
            if (s.Family != null)
            {
                string fq = s.Family.Name + ":" + s.Name;
                if (!columnSymbols.ContainsKey(fq)) columnSymbols[fq] = s;
            }
        }

        var beamSymbols = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (FamilySymbol s in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming))
        {
            if (!beamSymbols.ContainsKey(s.Name)) beamSymbols[s.Name] = s;
            if (s.Family != null)
            {
                string fq = s.Family.Name + ":" + s.Name;
                if (!beamSymbols.ContainsKey(fq)) beamSymbols[fq] = s;
            }
        }

        """;

    private static string GetPreambleTypeCollectorsScript() => """
        var wallTypes = new Dictionary<string, WallType>(StringComparer.OrdinalIgnoreCase);
        foreach (WallType w in new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(w => w.Kind == WallKind.Basic))
        {
            if (!wallTypes.ContainsKey(w.Name)) wallTypes[w.Name] = w;
        }

        var floorTypes = new Dictionary<string, FloorType>(StringComparer.OrdinalIgnoreCase);
        foreach (FloorType f in new FilteredElementCollector(doc).OfClass(typeof(FloorType)))
        {
            if (!floorTypes.ContainsKey(f.Name)) floorTypes[f.Name] = f;
        }

        var doorWinSymbols = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (FamilySymbol s in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Where(e => e.Category != null && (e.Category.BuiltInCategory == BuiltInCategory.OST_Doors || e.Category.BuiltInCategory == BuiltInCategory.OST_Windows)))
        {
            if (!doorWinSymbols.ContainsKey(s.Name)) doorWinSymbols[s.Name] = s;
            if (s.Family != null)
            {
                string fq = s.Family.Name + ":" + s.Name;
                if (!doorWinSymbols.ContainsKey(fq)) doorWinSymbols[fq] = s;
            }
        }

        var fixtureSymbols = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (FamilySymbol s in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_PlumbingFixtures))
        {
            if (!fixtureSymbols.ContainsKey(s.Name)) fixtureSymbols[s.Name] = s;
            if (s.Family != null)
            {
                string fq = s.Family.Name + ":" + s.Name;
                if (!fixtureSymbols.ContainsKey(fq)) fixtureSymbols[fq] = s;
            }
        }

        var rebarBarTypes = new Dictionary<string, Autodesk.Revit.DB.Structure.RebarBarType>(StringComparer.OrdinalIgnoreCase);
        foreach (Autodesk.Revit.DB.Structure.RebarBarType b in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Structure.RebarBarType)))
        {
            if (!rebarBarTypes.ContainsKey(b.Name)) rebarBarTypes[b.Name] = b;
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

        var createdWalls = new Dictionary<string, Wall>(StringComparer.OrdinalIgnoreCase);
        var createdElements = new Dictionary<string, Element>(StringComparer.OrdinalIgnoreCase);

        // 2. Sequential Execution of Items
        foreach (var item in items)
        {
            string sourceKey = NormalizeSourceKey(item);
            string category = item.Require("category");
            string mark = item.Str("mark", string.Empty);
            string targetUniqueId = item.Str("targetUniqueId", string.Empty);
            var res = item.Obj("resource");
            var geom = item.Obj("geometry");

            using (var subTx = new SubTransaction(doc))
            {
                subTx.Start();
                try
                {
        """;

    private static string GetLoopCloseScript() => """
                }
                catch (Exception ex)
                {
                    subTx.RollBack();
                    records.Add(new Dictionary<string, object>
                    {
                        ["sourceKey"] = sourceKey,
                        ["category"] = category,
                        ["mark"] = mark,
                        ["status"] = "Failed",
                        ["errorCode"] = "ERR_REVIT_API_EXCEPTION",
                        ["errorReason"] = $"{ex.GetType().Name}: {ex.Message}"
                    });
                }
            }
        }
        """;

    private static string GetEpilogueScript() => """
        return new
        {
            planId = args.Str("planId", string.Empty),
            total = items.Count,
            created = records.Count(r => (string)r["status"] == "Created"),
            updated = records.Count(r => (string)r["status"] == "Updated"),
            noOp = records.Count(r => (string)r["status"] == "NoOp"),
            skipped = records.Count(r => (string)r["status"] == "Skipped"),
            failed = records.Count(r => (string)r["status"] == "Failed"),
            records = records
        };
        """;
}
