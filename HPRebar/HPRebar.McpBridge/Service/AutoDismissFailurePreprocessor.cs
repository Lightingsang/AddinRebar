using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Keeps Revit from showing a failure dialog while the AI is driving: warnings are deleted, anything
///     worse rolls the transaction back. The script's result then carries the error text instead of a
///     modal box nobody is sitting in front of.
/// </summary>
public sealed class AutoDismissFailurePreprocessor : IFailuresPreprocessor
{
    private readonly List<string> _warnings = new();

    public IReadOnlyList<string> Warnings => _warnings;

    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        foreach (var failure in failuresAccessor.GetFailureMessages())
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
            {
                var desc = failure.GetDescriptionText();
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    _warnings.Add(desc);
                }
                failuresAccessor.DeleteWarning(failure);
                continue;
            }

            return FailureProcessingResult.ProceedWithRollBack;
        }

        return FailureProcessingResult.Continue;
    }

    /// <summary>Applies this preprocessor plus a clean rollback to a transaction the bridge owns.</summary>
    public static AutoDismissFailurePreprocessor ApplyTo(Transaction transaction)
    {
        var preprocessor = new AutoDismissFailurePreprocessor();
        var options = transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(preprocessor)
            .SetClearAfterRollback(true);

        transaction.SetFailureHandlingOptions(options);
        return preprocessor;
    }
}
