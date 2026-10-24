using HPBimToolkit.Core.Application;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Domain.Dashboard;
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

    private readonly SingleFlightRun<StatisticsScope> _statistics = new("Statistics for the other scope are still being counted.");

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(CommandNames.GetDocumentInfo, (_, token) =>
            queue.RunAsync<object?>(DocumentInfoReader.Read, token));

        dispatcher.Register<StatisticsRequest>(CommandNames.GetStatistics, ReadStatisticsAsync, StatisticsTimeout);

        dispatcher.Register(CommandNames.GetSelection, (_, token) =>
            queue.RunAsync<object?>(SelectionReader.Read, token));

        dispatcher.Register<ShowElementsRequest>(CommandNames.ShowElements, (request, token) =>
        {
            // Validated here, before the queue: a bad request never waits for Revit's API thread.
            var source = ShowElementsRules.SourceOf(request);
            return queue.RunAsync<object?>(app => ElementNavigator.Show(app, request, source), token);
        });

        dispatcher.Register<OpenToolRequest>(CommandNames.OpenTool, (request, token) =>
            queue.RunAsync<object?>(app => ToolLauncher.Open(app, request), token));
    }

    private Task<object?> ReadStatisticsAsync(StatisticsRequest request, CancellationToken token) =>
        _statistics.Run(request.Scope, token, () => queue.RunAsync<object?>(app =>
        {
            var statistics = ModelStatisticsReader.Read(app, request.Scope, token);
            if (!token.IsCancellationRequested)
            {
                // The page receives these numbers, so changes made before them are no longer news.
                changes.Reset();
            }

            return statistics;
        }, token));
}
