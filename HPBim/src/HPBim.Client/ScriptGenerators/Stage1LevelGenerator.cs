namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 1: Creating and naming Levels and associated Plan Views.
/// </summary>
public sealed class Stage1LevelGenerator : IStageScriptGenerator
{
    public int StageIndex => 1;
    public string StageName => "Levels";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var levels = model.Levels
            .OrderBy(l => l.ElevationM)
            .Select(l => new
            {
                name = l.Name,
                elevationM = l.ElevationM,
                elevationMm = l.ElevationMm,
                floorNumber = l.FloorNumber,
                isStructural = l.IsStructural
            })
            .ToList();

        var args = new
        {
            levels,
            createViews = options?.CreatePlanViews ?? true
        };

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = args,
            Label = $"Stage 1: Generate {levels.Count} Levels"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        var levelItems = args.List("levels");
        bool createViews = args.Bool("createViews", true);

        var existingLevels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        ViewFamilyType planType = null;
        if (createViews)
        {
            planType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);
        }

        var existingViewLevelIds = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .Select(v => v.GenLevel?.Id)
            .Where(id => id != null)
            .ToHashSet();

        int createdCount = 0;
        foreach (var item in levelItems)
        {
            string name = item.Require("name");
            double elevationM = item.Double("elevationM");
            double elevationFt = elevationM * (1000.0 / 304.8);

            Level level;
            if (existingLevels.TryGetValue(name, out var existing))
            {
                level = existing;
            }
            else
            {
                level = Level.Create(doc, elevationFt);
                level.Name = name;
                existingLevels[name] = level;
                createdCount++;
            }

            var pStory = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
            if (pStory != null && !pStory.IsReadOnly)
            {
                pStory.Set(1);
            }

            if (createViews && planType != null && !existingViewLevelIds.Contains(level.Id))
            {
                ViewPlan.Create(doc, planType.Id, level.Id);
                existingViewLevelIds.Add(level.Id);
            }
        }

        return new { stage = 1, totalLevels = levelItems.Count, createdLevels = createdCount };
        """;
}
