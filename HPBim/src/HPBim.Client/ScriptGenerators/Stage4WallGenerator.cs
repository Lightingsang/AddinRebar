namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 4: Creating architectural and partitioning Walls (110mm / 220mm).
/// </summary>
public sealed class Stage4WallGenerator : IStageScriptGenerator
{
    public int StageIndex => 4;
    public string StageName => "Walls";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var walls = model.Walls.Select(w => new
        {
            id = w.Id,
            x0 = w.StartPoint.X,
            y0 = w.StartPoint.Y,
            x1 = w.EndPoint.X,
            y1 = w.EndPoint.Y,
            thicknessMm = w.ThicknessMm,
            baseLevel = w.BaseLevel,
            topLevel = w.TopLevel,
            baseOffsetMm = w.BaseOffsetMm,
            topOffsetMm = w.TopOffsetMm
        }).ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { walls },
            Label = $"Stage 4: Generate {walls.Count} Walls"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}
        {{DynamicTypeScriptHelper.GetEnsureWallTypeScript()}}

        var levelsByName = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var wallItems = args.List("walls");
        int createdCount = 0;
        foreach (var w in wallItems)
        {
            string id = w.Require("id");
            string baseLvlName = w.Require("baseLevel");
            if (!levelsByName.TryGetValue(baseLvlName, out var baseLvl)) continue;

            double thicknessMm = w.Double("thicknessMm", 220);
            string typeName = $"HP_Tuong_{(int)thicknessMm}mm";
            var wallType = EnsureWallType(doc, typeName, thicknessMm);

            var p0 = ToXyz(w.Double("x0"), w.Double("y0"), 0);
            var p1 = ToXyz(w.Double("x1"), w.Double("y1"), 0);
            if (p0.DistanceTo(p1) < 0.001) continue;

            double defaultHeightFt = Ft(3000.0);
            string topLvlName = w.Str("topLevel", string.Empty);
            Level topLvl = null;
            if (!string.IsNullOrEmpty(topLvlName) && levelsByName.TryGetValue(topLvlName, out topLvl))
            {
                defaultHeightFt = Math.Max(Ft(500), topLvl.Elevation - baseLvl.Elevation);
            }

            var line = Line.CreateBound(p0, p1);
            var wall = Wall.Create(doc, line, wallType.Id, baseLvl.Id, defaultHeightFt, Ft(w.Double("baseOffsetMm")), false, false);
            if (topLvl != null)
            {
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.Set(topLvl.Id);
                wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET)?.Set(Ft(w.Double("topOffsetMm")));
            }

            wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(id);
            wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING)?.Set(1);
            createdCount++;
        }

        return new { stage = 4, totalWalls = wallItems.Count, createdWalls = createdCount };
        """;
}
