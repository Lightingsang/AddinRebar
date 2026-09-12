using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPRebar.BeamRebar.Model;
using HPRebar.BeamRebar.Service;
using HPRebar.BeamRebar.ViewModel;
using Serilog;

namespace HPRebar.BeamRebar;

/// <summary>
///     Moves build requests from the modeless window onto Revit's API thread. The window calls
///     <see cref="RunAsync"/>, which queues a request and raises the external event; Revit calls
///     <see cref="Execute"/> back on its own thread, where touching the document is legal.
///     The transaction boundary stays in <see cref="BeamRebarOrchestrator"/>, not here.
/// </summary>
public sealed class BeamRebarExternalEventHandler : IExternalEventHandler, IBeamRebarRunner, IDisposable
{
    private readonly BeamRebarOrchestrator _orchestrator;
    private readonly ConcurrentQueue<BeamRebarRequest> _pending = new ConcurrentQueue<BeamRebarRequest>();
    private readonly ExternalEvent _externalEvent;

    public BeamRebarExternalEventHandler(BeamRebarOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>What the last successful run put into the model.</summary>
    public BeamOrchestratorResult? LastResult { get; private set; }

    public int PlannedCount(BeamRebarSpec spec) => _orchestrator.PlannedCount(spec);

    public Task<int> RunAsync(BeamRebarSpec spec, IProgress<int> progress)
    {
        var request = new BeamRebarRequest(spec, progress);

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
                var result = _orchestrator.Run(request.Spec, request.Progress);

                if (!result.IsOk)
                {
                    request.Completion.TrySetException(new InvalidOperationException(result.Validation.Message));
                    continue;
                }

                LastResult = result;

                // Reported here rather than from the window: this is the thread that knows the run finished,
                // and the window closes itself as soon as the request completes.
                RevitDialogs.Info("Beam Rebar", Summary(result));

                request.Completion.TrySetResult(result);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Beam Rebar could not build the reinforcement");
                request.Completion.TrySetException(exception);
            }
        }
    }

    public string GetName() => "HPRebar Beam Rebar";

    public void Dispose() => _externalEvent.Dispose();

    private static async Task<int> TotalOf(BeamRebarRequest request)
    {
        var result = await request.Completion.Task.ConfigureAwait(true);

        return result.Total;
    }

    private static string Summary(BeamOrchestratorResult result) => string.Join(
        Environment.NewLine,
        $"Created {result.Views.Total} view(s) and {result.Rebar.Total} rebar element(s):",
        $"- {result.Rebar.Stirrups.Count} stirrup set(s)",
        $"- {result.Rebar.MainBars.Count} main bar(s)",
        $"- {result.Rebar.AdditionalBars.Count} additional bar(s)",
        $"- {result.Rebar.SideBars.Count} side bar(s)",
        $"- {result.Rebar.SpecialBars.Count} special hanging bar(s)");
}
