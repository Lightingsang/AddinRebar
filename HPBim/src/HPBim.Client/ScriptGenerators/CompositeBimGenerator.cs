namespace HPBim.Client.ScriptGenerators;

using System;
using System.Collections.Generic;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates a single unified script executing all 8 BIM generation stages sequentially
///     inside one atomic TransactionGroup for non-destructive dry-run rollback.
/// </summary>
public sealed partial class CompositeBimGenerator
{
    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var s1 = new Stage1LevelGenerator().Generate(model, options);
        var s2 = new Stage2GridGenerator().Generate(model, options);
        var s3 = new Stage3StructureGenerator().Generate(model, options);
        var s4 = new Stage4WallGenerator().Generate(model, options);
        var s5 = new Stage5OpeningGenerator().Generate(model, options);
        var s6 = new Stage6RoomGenerator().Generate(model, options);
        var s7 = new Stage7MepGenerator().Generate(model, options);
        var s8 = new Stage8RebarGenerator().Generate(model, options);

        var combinedArgs = ExtractCombinedArgs(s1, s2, s3, s4, s5, s6, s7, s8);

        return new StageExecutionPayload
        {
            StageIndex = 0,
            StageName = "CompositeBimPipeline",
            ScriptCode = GetScriptCode(),
            Args = combinedArgs,
            Label = $"Composite BIM Pipeline (Atomic Run for {model.ProjectName})"
        };
    }

    private static Dictionary<string, object> ExtractCombinedArgs(params StageExecutionPayload[] payloads)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in payloads)
        {
            if (p.Args == null) continue;
            var props = p.Args.GetType().GetProperties();
            foreach (var prop in props)
            {
                var val = prop.GetValue(p.Args);
                if (val != null) result[prop.Name] = val;
            }
        }
        return result;
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}
        {{DynamicTypeScriptHelper.GetEnsureColumnSymbolScript()}}
        {{DynamicTypeScriptHelper.GetEnsureBeamSymbolScript()}}
        {{DynamicTypeScriptHelper.GetEnsureFloorTypeScript()}}
        {{DynamicTypeScriptHelper.GetEnsureWallTypeScript()}}

        {{GetStage1LevelsScript()}}
        {{GetStage2GridsScript()}}
        {{GetStage3ColumnsScript()}}
        {{GetStage3BeamsScript()}}
        {{GetStage3SlabsScript()}}
        {{GetStage4WallsScript()}}
        {{GetStage5OpeningsScript()}}
        {{GetStage6RoomsScript()}}
        {{GetStage7MepScript()}}
        {{GetStage8RebarScript()}}
        {{GetCompositeReturnScript()}}
        """;
}
