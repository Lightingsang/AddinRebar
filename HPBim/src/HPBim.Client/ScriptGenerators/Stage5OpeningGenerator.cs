namespace HPBim.Client.ScriptGenerators;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 5: Creating wall-hosted Doors and Windows.
/// </summary>
public sealed class Stage5OpeningGenerator : IStageScriptGenerator
{
    public int StageIndex => 5;
    public string StageName => "Openings";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var openings = new List<object>();
        foreach (var wall in model.Walls)
        {
            if (wall.Openings == null || wall.Openings.Count == 0) continue;
            foreach (var op in wall.Openings)
            {
                string targetSymbol = op.Kind == OpeningKind.Door
                    ? TypeResolutionRules.GetDoorSymbolName(op.WidthMm, op.HeightMm)
                    : TypeResolutionRules.GetWindowSymbolName(op.WidthMm, op.HeightMm);

                openings.Add(new
                {
                    id = op.Id,
                    hostWallId = string.IsNullOrWhiteSpace(op.HostWallId) ? wall.Id : op.HostWallId,
                    kind = op.Kind.ToString(),
                    symbolName = targetSymbol,
                    distMm = op.DistanceAlongWallMm,
                    widthMm = op.WidthMm,
                    heightMm = op.HeightMm,
                    sillHeightMm = op.SillHeightMm,
                    baseLevel = wall.BaseLevel
                });
            }
        }

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { openings },
            Label = $"Stage 5: Generate {openings.Count} Openings (Doors/Windows)"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        var openingItems = args.List("openings");
        var allWalls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .Cast<Wall>()
            .ToList();

        var wallsById = new Dictionary<string, Wall>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in allWalls)
        {
            string mark = w.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
            string comment = w.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty;
            if (!string.IsNullOrEmpty(mark)) wallsById[mark] = w;
            if (!string.IsNullOrEmpty(comment)) wallsById[comment] = w;
        }

        var doorSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Doors)
            .Cast<FamilySymbol>()
            .ToList();

        var windowSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Windows)
            .Cast<FamilySymbol>()
            .ToList();

        int createdCount = 0;
        foreach (var op in openingItems)
        {
            string hostWallId = op.Require("hostWallId");
            if (!wallsById.TryGetValue(hostWallId, out var hostWall)) continue;

            string kind = op.Str("kind", "Door");
            bool isDoor = string.Equals(kind, "Door", StringComparison.OrdinalIgnoreCase);
            var symbolList = isDoor ? doorSymbols : windowSymbols;
            if (symbolList.Count == 0) continue;

            string targetName = op.Str("symbolName", string.Empty);
            var symbol = symbolList.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase))
                ?? symbolList.FirstOrDefault();
            if (symbol == null) continue;

            if (!symbol.IsActive) symbol.Activate();

            var locCurve = hostWall.Location as LocationCurve;
            if (locCurve?.Curve == null) continue;

            double distFt = Ft(op.Double("distMm") + (op.Double("widthMm") / 2.0));
            double param = Math.Clamp(distFt / locCurve.Curve.Length, 0.05, 0.95);
            var centerPoint = locCurve.Curve.Evaluate(param, true);
            double sillFt = Ft(op.Double("sillHeightMm"));
            var pt = new XYZ(centerPoint.X, centerPoint.Y, centerPoint.Z + sillFt);

            var level = doc.GetElement(hostWall.LevelId) as Level;
            var inst = doc.Create.NewFamilyInstance(pt, symbol, hostWall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(op.Require("id"));
            createdCount++;
        }

        return new { stage = 5, totalOpenings = openingItems.Count, createdOpenings = createdCount };
        """;
}
