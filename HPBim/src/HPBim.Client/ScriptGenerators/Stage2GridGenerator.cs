namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 2: Creating orthogonal structural Grids.
/// </summary>
public sealed class Stage2GridGenerator : IStageScriptGenerator
{
    public int StageIndex => 2;
    public string StageName => "Grids";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var grids = model.Grids
            .Select(g => new
            {
                name = g.Name,
                x0 = g.StartPoint.X,
                y0 = g.StartPoint.Y,
                x1 = g.EndPoint.X,
                y1 = g.EndPoint.Y,
                direction = g.Direction.ToString()
            })
            .ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { grids },
            Label = $"Stage 2: Generate {grids.Count} Grids"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        var gridItems = args.List("grids");
        var existingGrids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);

        int createdCount = 0;
        foreach (var item in gridItems)
        {
            string name = item.Require("name");
            if (existingGrids.ContainsKey(name)) continue;

            double x0 = item.Double("x0");
            double y0 = item.Double("y0");
            double x1 = item.Double("x1");
            double y1 = item.Double("y1");

            var p0 = new XYZ(Ft(x0), Ft(y0), 0);
            var p1 = new XYZ(Ft(x1), Ft(y1), 0);

            if (p0.DistanceTo(p1) < 0.001) continue;

            var line = Line.CreateBound(p0, p1);
            var grid = Grid.Create(doc, line);
            grid.Name = name;
            existingGrids[name] = grid;
            createdCount++;
        }

        return new { stage = 2, totalGrids = gridItems.Count, createdGrids = createdCount };
        """;
}
