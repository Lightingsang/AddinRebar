using Autodesk.Revit.DB;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Keeps Revit from showing a failure dialog while the AI is driving: warnings are deleted, anything
///     worse rolls the transaction back. The script's result then carries the error text instead of a
///     modal box nobody is sitting in front of.
/// </summary>
public sealed class AutoDismissFailurePreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        foreach (var failure in failuresAccessor.GetFailureMessages())
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
            {
                failuresAccessor.DeleteWarning(failure);
                continue;
            }

            return FailureProcessingResult.ProceedWithRollBack;
        }

        return FailureProcessingResult.Continue;
    }

    /// <summary>Applies this preprocessor plus a clean rollback to a transaction the bridge owns.</summary>
    public static void ApplyTo(Transaction transaction)
    {
        var options = transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(new AutoDismissFailurePreprocessor())
            .SetClearAfterRollback(true);

        transaction.SetFailureHandlingOptions(options);
    }
}
