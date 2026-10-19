namespace HPGrasshopper.McpBridge.Support;

using System.Collections.Generic;
using Autodesk.Revit.DB;

/// <summary>
///     Implements IFailuresPreprocessor to suppress modal warnings and errors during script execution.
/// </summary>
public sealed class AutoDismissFailurePreprocessor : IFailuresPreprocessor
{
    private readonly List<string> _warnings = new();

    public IReadOnlyList<string> Warnings => _warnings;

    public static AutoDismissFailurePreprocessor ApplyTo(Transaction transaction)
    {
        var preprocessor = new AutoDismissFailurePreprocessor();
        var options = transaction.GetFailureHandlingOptions();
        options.SetFailuresPreprocessor(preprocessor);
        options.SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(options);
        return preprocessor;
    }

    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        var messages = failuresAccessor.GetFailureMessages();
        foreach (var msg in messages)
        {
            if (msg.GetSeverity() == FailureSeverity.Warning)
            {
                _warnings.Add(msg.GetDescriptionText());
                failuresAccessor.DeleteWarning(msg);
            }
        }
        return FailureProcessingResult.Continue;
    }
}
