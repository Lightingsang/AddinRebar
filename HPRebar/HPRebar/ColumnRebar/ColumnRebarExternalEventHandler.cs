using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.ColumnRebar.Model;
using HPRebar.ColumnRebar.Service;
using HPRebar.ColumnRebar.ViewModel;
using Serilog;

namespace HPRebar.ColumnRebar;

/// <summary>
///     Moves build requests from the modeless window onto Revit's API thread. The window calls
///     <see cref="RunAsync"/>, which queues a request and raises the external event; Revit calls
///     <see cref="Execute"/> back on its own thread, where touching the document is legal.
///     The transaction boundary stays in the orchestrator, not here.
/// </summary>
public sealed class ColumnRebarExternalEventHandler : IExternalEventHandler, IColumnRebarRunner, IDisposable
{
    private readonly ColumnRebarOrchestrator _orchestrator;
    private readonly ConcurrentQueue<ColumnRebarRequest> _pending = new ConcurrentQueue<ColumnRebarRequest>();
    private readonly ExternalEvent _externalEvent;

    public ColumnRebarExternalEventHandler(ColumnRebarOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>What the last successful run put into the model.</summary>
    public OrchestratorResult? LastResult { get; private set; }

    public int PlannedCount(IReadOnlyList<ColumnRebarSpec> specs) => _orchestrator.PlannedCount(specs);

    public Task<int> RunAsync(IReadOnlyList<ColumnRebarSpec> specs, ViewNaming naming, IProgress<int> progress)
    {
        var request = new ColumnRebarRequest(specs, naming, progress);

        _pending.Enqueue(request);
        _externalEvent.Raise();

        return TotalOf(request);
    }

    /// <summary>Revit calls this on its API thread, once per <see cref="ExternalEvent.Raise"/>.</summary>
    public void Execute(UIApplication application)
    {
        while (_pending.TryDequeue(out var request))
        {
            try
            {
                var result = _orchestrator.Run(request.Specs, request.Naming, request.Progress);

                if (!result.IsOk)
                {
                    request.Completion.TrySetException(new InvalidOperationException(result.Validation.Message));
                    continue;
                }

                LastResult = result;

                // Reported here rather than from the window: this is the thread that knows the run finished,
                // and the window closes itself as soon as the request completes.
                RevitDialogs.Info("Column Rebar", Summary(result));

                request.Completion.TrySetResult(result);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Column Rebar could not build the reinforcement");
                request.Completion.TrySetException(exception);
            }
        }
    }

    public string GetName() => "HPRebar Column Rebar";

    public void Dispose() => _externalEvent.Dispose();

    private static async Task<int> TotalOf(ColumnRebarRequest request)
    {
        var result = await request.Completion.Task.ConfigureAwait(true);

        return result.Total;
    }

    private static string Summary(OrchestratorResult result) =>
        $"Created {result.Views.Total} view(s) and {result.Rebar.Total} rebar element(s): " +
        $"{result.Rebar.MainBars.Count} main bar(s), {result.Rebar.Stirrups.Count} tie(s), " +
        $"{result.Rebar.Ties.Count} cross-tie(s).";
}
