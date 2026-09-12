using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Model;
using HPRebar.FoundationRebar.Service;
using HPRebar.FoundationRebar.ViewModel;
using Serilog;

namespace HPRebar.FoundationRebar;

/// <summary>
///     Moves build requests from the modeless window onto Revit's API thread. The window calls
///     <see cref="RunAsync"/>, which queues a request and raises the external event; Revit calls
///     <see cref="Execute"/> back on its own thread, where opening a transaction is legal.
///     The transaction group lives in <see cref="FoundationRebarOrchestrator"/>, not here.
/// </summary>
public sealed class FoundationRebarExternalEventHandler
    : IExternalEventHandler, IFoundationRebarRunner, IDisposable
{
    private readonly FoundationRebarOrchestrator _orchestrator;

    private readonly ConcurrentQueue<FoundationRebarRequest> _pending =
        new ConcurrentQueue<FoundationRebarRequest>();

    private readonly ExternalEvent _externalEvent;

    public FoundationRebarExternalEventHandler(FoundationRebarOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>The mesh the last successful run put into the model.</summary>
    public FoundationMeshResult? LastResult { get; private set; }

    public Task RunAsync(FoundationSession session)
    {
        var request = new FoundationRebarRequest(session);

        _pending.Enqueue(request);
        _externalEvent.Raise();

        return request.Completion.Task;
    }

    /// <summary>Revit calls this on its API thread, once per <see cref="ExternalEvent.Raise"/>.</summary>
    public void Execute(UIApplication application)
    {
        while (_pending.TryDequeue(out var request))
        {
            try
            {
                var mesh = _orchestrator.Run(request.Session);

                LastResult = mesh;

                // Reported here rather than from the window: this is the thread that knows the run finished,
                // and the window closes itself as soon as the request completes.
                RevitDialogs.Info("Foundation Rebar", Summary(mesh, request.Session));

                request.Completion.TrySetResult(mesh);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Foundation Rebar could not build the reinforcement");
                request.Completion.TrySetException(exception);
            }
        }
    }

    public string GetName() => "HPRebar Foundation Rebar";

    public void Dispose() => _externalEvent.Dispose();

    private static string Summary(FoundationMeshResult mesh, FoundationSession session)
    {
        var statistics = mesh.Statistics;

        var lines = new List<string>
        {
            "Foundation reinforcement generated successfully!",
            $"- Total bars: {statistics.TotalBarCount}",
            $"  - Bottom Mat Dir X: {statistics.BottomBarCountX} bars",
            $"  - Bottom Mat Dir Y: {statistics.BottomBarCountY} bars"
        };

        if (session.Spec.IsTopMatEnabled)
        {
            lines.Add($"  - Top Mat Dir X: {statistics.TopBarCountX} bars");
            lines.Add($"  - Top Mat Dir Y: {statistics.TopBarCountY} bars");
        }
        else
        {
            lines.Add("  - Top Mat: Disabled");
        }

        lines.Add($"- Total length: {statistics.TotalLengthMm / 1000.0:F1} m");
        lines.Add($"- Estimated weight: {statistics.EstimatedWeightKg:F1} kg");

        return string.Join(Environment.NewLine, lines);
    }
}
