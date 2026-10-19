namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 7: Creating MEP Plumbing Fixtures and Piping Networks.
/// </summary>
public sealed class Stage7MepGenerator : IStageScriptGenerator
{
    public int StageIndex => 7;
    public string StageName => "MEP";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var fixtures = model.Fixtures.Select(f => new
        {
            id = f.Id,
            category = f.Category.ToString(),
            familyName = f.FamilyName,
            x = f.Location.X,
            y = f.Location.Y,
            z = f.Location.Z,
            rotation = f.RotationDeg,
            level = f.Level
        }).ToList();

        var pipes = model.Pipes.Select(p => new
        {
            id = p.Id,
            systemType = p.SystemType.ToString(),
            diameterMm = p.DiameterMm,
            level = p.Level,
            points = p.Points.Select(pt => new { x = pt.X, y = pt.Y, z = pt.Z }).ToList()
        }).ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { fixtures, pipes },
            Label = $"Stage 7: MEP ({fixtures.Count} fixtures, {pipes.Count} pipe runs)"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        var levelsByName = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var fixtureSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
            .Cast<FamilySymbol>()
            .ToList();

        var defaultFixtureSym = fixtureSymbols.FirstOrDefault();
        if (defaultFixtureSym != null && !defaultFixtureSym.IsActive)
        {
            defaultFixtureSym.Activate();
        }

        var fixtureItems = args.List("fixtures");
        int createdFixtures = 0;
        foreach (var fx in fixtureItems)
        {
            if (defaultFixtureSym == null) break;
            string lvlName = fx.Require("level");
            if (!levelsByName.TryGetValue(lvlName, out var lvl)) continue;

            var pt = ToXyz(fx.Double("x"), fx.Double("y"), fx.Double("z"));
            var inst = doc.Create.NewFamilyInstance(pt, defaultFixtureSym, lvl, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(fx.Require("id"));
            createdFixtures++;
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
        int createdPipes = 0;
        if (pipeItems.Count > 0 && (pipingSystemType == null || pipeType == null))
        {
            throw new InvalidOperationException($"[ERR_SYMBOL_NOT_FOUND] PipingSystemType '{reqSysName}' or PipeType '{reqPipeName}' not found in document.");
        }

        if (pipingSystemType != null && pipeType != null)
        {
            foreach (var p in pipeItems)
            {
                string lvlName = p.Require("level");
                if (!levelsByName.TryGetValue(lvlName, out var lvl)) continue;

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
                    createdPipes++;
                }
            }
        }

        return new { stage = 7, fixtures = createdFixtures, pipes = createdPipes };
        """;
}
