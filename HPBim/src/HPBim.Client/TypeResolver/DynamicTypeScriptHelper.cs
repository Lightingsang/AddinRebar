namespace HPBim.Client.TypeResolver;

/// <summary>
///     Generates embedded C# routines for Revit scripts to dynamically resolve, duplicate,
///     and activate element types and family symbols.
/// </summary>
public static class DynamicTypeScriptHelper
{
    public static string GetCommonHelpersScript() => """
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        void SetParamValue(FamilySymbol symbol, string[] paramNames, double valueFt)
        {
            foreach (var name in paramNames)
            {
                var param = symbol.LookupParameter(name);
                if (param != null && !param.IsReadOnly)
                {
                    param.Set(valueFt);
                    return;
                }
            }
        }
        """;

    public static string GetEnsureWallTypeScript() => """
        WallType EnsureWallType(Document doc, string targetName, double thicknessMm)
        {
            var wallTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(w => w.Kind == WallKind.Basic)
                .ToList();

            var exact = wallTypes.FirstOrDefault(w => string.Equals(w.Name, targetName, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            double targetWidthFt = Ft(thicknessMm);
            var matching = wallTypes.FirstOrDefault(w => Math.Abs(w.Width - targetWidthFt) < 0.005);
            if (matching != null) return matching;

            var baseType = wallTypes.FirstOrDefault() ?? throw new InvalidOperationException("No basic wall type found in project.");
            var newType = (WallType)baseType.Duplicate(targetName);
            var cs = newType.GetCompoundStructure();
            if (cs != null)
            {
                int layerIdx = cs.StructuralMaterialIndex >= 0 ? cs.StructuralMaterialIndex : 0;
                if (layerIdx >= 0 && layerIdx < cs.LayerCount)
                {
                    cs.SetLayerWidth(layerIdx, targetWidthFt);
                    newType.SetCompoundStructure(cs);
                }
            }
            return newType;
        }
        """;

    public static string GetEnsureColumnSymbolScript() => """
        FamilySymbol EnsureColumnSymbol(Document doc, string targetName, double widthMm, double depthMm)
        {
            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .Cast<FamilySymbol>()
                .ToList();

            var exact = symbols.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                if (!exact.IsActive) exact.Activate();
                return exact;
            }

            var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural column symbol found.");
            var newSymbol = (FamilySymbol)baseSymbol.Duplicate(targetName);
            SetParamValue(newSymbol, new[] { "b", "Width", "B", "b_Col" }, Ft(widthMm));
            SetParamValue(newSymbol, new[] { "h", "Depth", "H", "h_Col" }, Ft(depthMm));
            if (!newSymbol.IsActive) newSymbol.Activate();
            return newSymbol;
        }
        """;

    public static string GetEnsureBeamSymbolScript() => """
        FamilySymbol EnsureBeamSymbol(Document doc, string targetName, double widthMm, double heightMm)
        {
            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilySymbol>()
                .ToList();

            var exact = symbols.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                if (!exact.IsActive) exact.Activate();
                return exact;
            }

            var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural framing symbol found.");
            var newSymbol = (FamilySymbol)baseSymbol.Duplicate(targetName);
            SetParamValue(newSymbol, new[] { "b", "Width", "B", "b_Beam" }, Ft(widthMm));
            SetParamValue(newSymbol, new[] { "h", "Height", "H", "h_Beam" }, Ft(heightMm));
            if (!newSymbol.IsActive) newSymbol.Activate();
            return newSymbol;
        }
        """;

    public static string GetEnsureFloorTypeScript() => """
        FloorType EnsureFloorType(Document doc, string targetName, double thicknessMm)
        {
            var floorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();

            var exact = floorTypes.FirstOrDefault(f => string.Equals(f.Name, targetName, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            double targetWidthFt = Ft(thicknessMm);
            var baseType = floorTypes.FirstOrDefault() ?? throw new InvalidOperationException("No floor type found in project.");
            var newType = (FloorType)baseType.Duplicate(targetName);
            var cs = newType.GetCompoundStructure();
            if (cs != null)
            {
                int layerIdx = cs.StructuralMaterialIndex >= 0 ? cs.StructuralMaterialIndex : 0;
                if (layerIdx >= 0 && layerIdx < cs.LayerCount)
                {
                    cs.SetLayerWidth(layerIdx, targetWidthFt);
                    newType.SetCompoundStructure(cs);
                }
            }
            return newType;
        }
        """;

    public static string GetEnsureLevelScript() => """
        Level EnsureLevel(Document doc, string levelName, double elevationM)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            var existing = levels.FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var newLevel = Level.Create(doc, elevationM * (1000.0 / 304.8));
            newLevel.Name = levelName;
            return newLevel;
        }
        """;
}
