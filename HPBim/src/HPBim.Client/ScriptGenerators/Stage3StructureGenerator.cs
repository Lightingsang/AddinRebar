namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 3: Creating structural Columns, Beams, and Floors (Slabs).
/// </summary>
public sealed class Stage3StructureGenerator : IStageScriptGenerator
{
    public int StageIndex => 3;
    public string StageName => "Structure";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var columns = model.Columns.Select(c => new
        {
            id = c.Id,
            x = c.CenterLocation.X,
            y = c.CenterLocation.Y,
            widthMm = c.WidthMm,
            depthMm = c.DepthMm,
            baseLevel = c.BaseLevel,
            topLevel = c.TopLevel,
            baseOffsetMm = c.BaseOffsetMm,
            topOffsetMm = c.TopOffsetMm
        }).ToList();

        var beams = model.Beams.Select(b => new
        {
            id = b.Id,
            x0 = b.StartPoint.X,
            y0 = b.StartPoint.Y,
            x1 = b.EndPoint.X,
            y1 = b.EndPoint.Y,
            widthMm = b.WidthMm,
            heightMm = b.HeightMm,
            level = b.Level
        }).ToList();

        var slabs = model.Slabs.Select(s => new
        {
            id = s.Id,
            level = s.Level,
            thicknessMm = s.ThicknessMm,
            offsetMm = s.ElevationOffsetMm,
            points = s.Boundary.Vertices.Select(v => new { x = v.X, y = v.Y }).ToList()
        }).ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { columns, beams, slabs },
            Label = $"Stage 3: Structure ({columns.Count} cols, {beams.Count} beams, {slabs.Count} slabs)"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}
        {{DynamicTypeScriptHelper.GetEnsureColumnSymbolScript()}}
        {{DynamicTypeScriptHelper.GetEnsureBeamSymbolScript()}}
        {{DynamicTypeScriptHelper.GetEnsureFloorTypeScript()}}

        var levelsByName = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var columnItems = args.List("columns");
        int createdCols = 0;
        foreach (var col in columnItems)
        {
            string id = col.Require("id");
            string baseLvlName = col.Require("baseLevel");
            string topLvlName = col.Str("topLevel", baseLvlName);
            if (!levelsByName.TryGetValue(baseLvlName, out var baseLvl)) continue;

            double w = col.Double("widthMm", 220);
            double d = col.Double("depthMm", 220);
            string symName = $"HP_Cot_C{(int)w}x{(int)d}";
            var symbol = EnsureColumnSymbol(doc, symName, w, d);

            var loc = ToXyz(col.Double("x"), col.Double("y"), 0);
            var instance = doc.Create.NewFamilyInstance(loc, symbol, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);

            if (levelsByName.TryGetValue(topLvlName, out var topLvl))
            {
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLvl.Id);
            }

            double rotDeg = col.Double("rotationDegrees", col.Double("rotationDeg", 0.0));
            if (Math.Abs(rotDeg) > 1e-4)
            {
                var axis = Line.CreateBound(loc, loc + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(doc, instance.Id, axis, rotDeg * (Math.PI / 180.0));
            }

            instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            createdCols++;
        }

        var beamItems = args.List("beams");
        int createdBeams = 0;
        foreach (var b in beamItems)
        {
            string id = b.Require("id");
            string lvlName = b.Require("level");
            if (!levelsByName.TryGetValue(lvlName, out var lvl)) continue;

            double w = b.Double("widthMm", 200);
            double h = b.Double("heightMm", 400);
            string symName = $"HP_Dam_D{(int)w}x{(int)h}";
            var symbol = EnsureBeamSymbol(doc, symName, w, h);

            var p0 = ToXyz(b.Double("x0"), b.Double("y0"), 0);
            var p1 = ToXyz(b.Double("x1"), b.Double("y1"), 0);
            if (p0.DistanceTo(p1) < 0.001) continue;

            var curve = Line.CreateBound(p0, p1);
            var beam = doc.Create.NewFamilyInstance(curve, symbol, lvl, Autodesk.Revit.DB.Structure.StructuralType.Beam);
            beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            createdBeams++;
        }

        var slabItems = args.List("slabs");
        int createdSlabs = 0;
        foreach (var s in slabItems)
        {
            string id = s.Require("id");
            string lvlName = s.Require("level");
            if (!levelsByName.TryGetValue(lvlName, out var lvl)) continue;

            double thick = s.Double("thicknessMm", 100);
            var fType = EnsureFloorType(doc, $"HP_San_{(int)thick}mm", thick);

            var pts = s.List("points");
            if (pts.Count < 3) continue;

            var loop = new CurveLoop();
            for (int i = 0; i < pts.Count; i++)
            {
                var cur = ToXyz(pts[i].Double("x"), pts[i].Double("y"), 0);
                var next = ToXyz(pts[(i + 1) % pts.Count].Double("x"), pts[(i + 1) % pts.Count].Double("y"), 0);
                if (cur.DistanceTo(next) > 0.001)
                {
                    loop.Append(Line.CreateBound(cur, next));
                }
            }

            if (loop.IsOpen()) continue;
            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, fType.Id, lvl.Id, true, null, 0);
            floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            createdSlabs++;
        }

        return new { stage = 3, columns = createdCols, beams = createdBeams, slabs = createdSlabs };
        """;
}
