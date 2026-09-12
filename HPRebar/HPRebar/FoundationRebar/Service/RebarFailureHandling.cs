using Autodesk.Revit.DB;
using Serilog;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Preprocessor attached to transactions to swallow benign non-fatal Revit warnings
/// (e.g. rebar curve slightly extending outside host volume or minor bar clearances).
/// </summary>
public static class RebarFailureHandling
{
    public static void Apply(Transaction transaction)
    {
        var options = transaction.GetFailureHandlingOptions();
        options = options.SetFailuresPreprocessor(new SwallowWarnings());
        options = options.SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(options);
    }

    private sealed class SwallowWarnings : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (var failure in accessor.GetFailureMessages())
            {
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    Log.Warning("Revit non-fatal warning swallowed: {Warning}", failure.GetDescriptionText());
                    accessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
