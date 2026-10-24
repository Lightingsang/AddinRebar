using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Protocol;
using HPBimToolkit.Core.Statistics;
using HPBimToolkit.ExternalEvents;
using HPBimToolkit.Services;

namespace HPBimToolkit.Modules.Dashboard;

/// <summary>The Dashboard's commands. Every Revit read or selection change runs on the API thread through the queue.</summary>
public sealed class DashboardModule(RevitRequestQueue queue, ModelChangeTally changes) : IToolkitModule
{
    /// <summary>
    ///     A count walks every element on Revit's UI thread; on a large model that can take far longer than a click.
    ///     The page waits a little longer than this (frontend STATISTICS_TIMEOUT_MS) so the host's TIMEOUT arrives first.
    /// </summary>
    public static readonly TimeSpan StatisticsTimeout = TimeSpan.FromSeconds(120);

    private (Task<object?> Task, StatisticsScope Scope, CancellationToken Token)? _statisticsRun;

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(CommandNames.GetDocumentInfo, (_, token) =>
            queue.RunAsync<object?>(DocumentInfoReader.Read, token));

        dispatcher.Register<StatisticsRequest>(CommandNames.GetStatistics, ReadStatisticsAsync, StatisticsTimeout);

        dispatcher.Register(CommandNames.GetSelection, (_, token) =>
            queue.RunAsync<object?>(SelectionReader.Read, token));

        dispatcher.Register<ShowElementsRequest>(CommandNames.ShowElements, (request, token) =>
            queue.RunAsync<object?>(app => ElementNavigator.Show(app, request), token));

        dispatcher.Register<OpenToolRequest>(CommandNames.OpenTool, (request, token) =>
            queue.RunAsync<object?>(app => ToolLauncher.Open(app, request), token));
    }

    /// <summary>
    ///     One count at a time. A second request for the same scope joins the run in progress; a request for another scope
    ///     answers BUSY until it ends. A run whose own request already timed out is abandoned (Revit will skip it), so a
    ///     retry after a modal dialog queues a fresh run instead of inheriting the cancellation.
    /// </summary>
    private Task<object?> ReadStatisticsAsync(StatisticsRequest request, CancellationToken token)
    {
        if (_statisticsRun is { Task.IsCompleted: false, Token.IsCancellationRequested: false } running)
        {
            return running.Scope == request.Scope
                ? running.Task
                : throw new BridgeException(BridgeErrorCodes.Busy, "Statistics for the other scope are still being counted.");
        }

        var run = queue.RunAsync<object?>(app =>
        {
            var statistics = ModelStatisticsReader.Read(app, request.Scope, token);
            if (!token.IsCancellationRequested)
            {
                // The page receives these numbers, so changes made before them are no longer news.
                changes.Reset();
            }

            return statistics;
        }, token);
        _statisticsRun = (run, request.Scope, token);
        return run;
    }
}
