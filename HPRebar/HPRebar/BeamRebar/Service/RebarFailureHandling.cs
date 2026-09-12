using Autodesk.Revit.DB;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Preprocessor attached to inner transactions to swallow non-fatal Revit warnings
/// (such as rebar geometry slightly extending outside the host volume or bar overlap).
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
