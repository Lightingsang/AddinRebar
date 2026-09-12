using Autodesk.Revit.DB;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Keeps Revit's warning dialogs out of the way of a run.
///
///     Reinforcing a column raises warnings as a matter of course — bars that touch, a tie that ends up
///     outside its cover. Left alone each one stops the run behind a modal dialog the user cannot answer,
///     because the tool's own dialog already owns the screen. Warnings are therefore cleared and written to
///     the log instead. Errors are left exactly where they are, so they still reach the orchestrator's
///     rollback and take the whole run back with them.
/// </summary>
internal static class RebarFailureHandling
{
    /// <summary>
    ///     Attaches the handler to a transaction that has already been started. Doing it before the start
    ///     would leave the options behind when Revit opens the transaction.
    /// </summary>
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
                if (failure.GetSeverity() != FailureSeverity.Warning) continue;

                Log.Warning(
                    "Revit warned while building column reinforcement, and the warning was cleared: {Warning}",
                    failure.GetDescriptionText());

                accessor.DeleteWarning(failure);
            }

            return FailureProcessingResult.Continue;
        }
    }
}
