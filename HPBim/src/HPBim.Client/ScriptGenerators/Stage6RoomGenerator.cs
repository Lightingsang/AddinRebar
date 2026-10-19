namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 6: Creating Rooms within wall boundaries after Document regeneration.
/// </summary>
public sealed class Stage6RoomGenerator : IStageScriptGenerator
{
    public int StageIndex => 6;
    public string StageName => "Rooms";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var rooms = model.Rooms.Select((r, idx) => new
        {
            name = r.Name,
            number = (idx + 1).ToString("D2"),
            level = r.Level,
            x = r.TagLocation.X,
            y = r.TagLocation.Y,
            rotation = r.TagRotationDeg
        }).ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { rooms },
            Label = $"Stage 6: Generate {rooms.Count} Rooms"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        doc.Regenerate();

        var levelsByName = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var roomItems = args.List("rooms");
        int createdCount = 0;
        foreach (var r in roomItems)
        {
            string lvlName = r.Require("level");
            if (!levelsByName.TryGetValue(lvlName, out var level)) continue;

            double xFt = Ft(r.Double("x"));
            double yFt = Ft(r.Double("y"));
            var uv = new UV(xFt, yFt);

            try
            {
                var room = doc.Create.NewRoom(level, uv);
                if (room != null)
                {
                    room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(r.Require("name"));
                    room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.Set(r.Str("number", $"{createdCount + 1:D2}"));
                    createdCount++;
                }
            }
            catch (Exception)
            {
                // Catch point-outside-boundary in unfinished models without halting pipeline.
            }
        }

        return new { stage = 6, totalRooms = roomItems.Count, createdRooms = createdCount };
        """;
}
