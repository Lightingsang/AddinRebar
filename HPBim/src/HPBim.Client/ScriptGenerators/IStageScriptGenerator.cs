namespace HPBim.Client.ScriptGenerators;

using HPBim.Core.Models;

/// <summary>
///     Contract for generating executable Revit C# scripts for a specific BIM creation stage.
/// </summary>
public interface IStageScriptGenerator
{
    /// <summary>
    ///     Index of the stage (1 through 8).
    /// </summary>
    int StageIndex { get; }

    /// <summary>
    ///     Descriptive name of the stage.
    /// </summary>
    string StageName { get; }

    /// <summary>
    ///     Generates the script payload containing C# code and parameters.
    /// </summary>
    StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null);
}
