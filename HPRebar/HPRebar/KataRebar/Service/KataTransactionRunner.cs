using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Runs one step of a Kata Rebar generation in its own transaction and makes its outcome explicit:
/// Revit warnings are logged and cleared, a Revit error rolls the step back without a dialog, and any
/// step that does not end committed throws so the caller rolls back the whole group.
/// </summary>
public sealed class KataTransactionRunner
{
    private readonly Document _doc;
    private readonly List<string> _warnings = new();

    public KataTransactionRunner(Document doc)
    {
        _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    }

    /// <summary>Revit warnings seen in every step so far (e.g. a bar reaching outside its host).</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    public T Run<T>(string name, Func<T> step)
    {
        var failures = new FailureCollector(_warnings);
        using var transaction = new Transaction(_doc, name);
        var options = transaction.GetFailureHandlingOptions()
            .SetFailuresPreprocessor(failures)
            .SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(options);
        transaction.Start();

        T result;
        try
        {
            result = step();
        }
        catch
        {
            if (transaction.GetStatus() == TransactionStatus.Started) transaction.RollBack();
            throw;
        }

        var status = transaction.Commit();
        if (status != TransactionStatus.Committed)
        {
            string reason = failures.Errors.Count > 0 ? string.Join("; ", failures.Errors) : status.ToString();
            throw new InvalidOperationException($"Revit huỷ bước '{name}': {reason}");
        }

        return result;
    }

    private sealed class FailureCollector : IFailuresPreprocessor
    {
        private readonly List<string> _warnings;

        public FailureCollector(List<string> warnings) => _warnings = warnings;

        public List<string> Errors { get; } = new();

        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (var failure in accessor.GetFailureMessages())
            {
                string text = failure.GetDescriptionText();
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    Log.Warning("Kata Rebar: Revit warning cleared: {Warning}", text);
                    _warnings.Add(text);
                    accessor.DeleteWarning(failure);
                }
                else
                {
                    Log.Error("Kata Rebar: Revit error: {Error}", text);
                    Errors.Add(text);
                }
            }

            return Errors.Count > 0 ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
        }
    }
}
