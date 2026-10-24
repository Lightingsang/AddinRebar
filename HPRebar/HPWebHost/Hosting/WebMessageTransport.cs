using System.Diagnostics;
using HPWebHost.Protocol;
using Serilog;

namespace HPWebHost.Hosting;

/// <summary>
///     Carries protocol v1 over the WebView: page messages → parser → dispatcher → response, and Revit events → page.
///     Messages from any origin other than the toolkit's own page are ignored.
/// </summary>
public sealed class WebMessageTransport : IDisposable
{
    private readonly WebViewHost _host;
    private readonly CommandDispatcher _dispatcher;
    private readonly PageOrigins _origins;
    private readonly ILogger _logger;

    public WebMessageTransport(WebViewHost host, CommandDispatcher dispatcher, PageOrigins origins, ILogger logger)
    {
        _host = host;
        _dispatcher = dispatcher;
        _origins = origins;
        _logger = logger;
        _host.MessageReceived += OnMessageReceived;
    }

    /// <summary>While paused (pane hidden) events are dropped; the page refreshes itself when it is shown again.</summary>
    public bool IsPaused { get; set; }

    /// <summary>True when an event would reach the page: the pane is shown and its browser is up.</summary>
    public bool CanPublish => !IsPaused && _host.IsReady;

    public void Publish(string eventName, object payload)
    {
        if (!CanPublish)
        {
            return;
        }

        _host.PostJson(BridgeJson.Serialize(new EventEnvelope(eventName, payload)));
    }

    public void Dispose() => _host.MessageReceived -= OnMessageReceived;

    private async void OnMessageReceived(string sourceUrl, string message)
    {
        if (!_origins.IsTrusted(sourceUrl))
        {
            _logger.Warning("Ignored a message from untrusted page {Source}", sourceUrl);
            return;
        }

        try
        {
            var response = await HandleAsync(message);
            _host.PostJson(BridgeJson.Serialize(response));
        }
        catch (Exception exception)
        {
            // Serialising the response is the only step left that can fail here; the page times out its call.
            _logger.Error(exception, "Could not answer a page message");
        }
    }

    private async Task<ResponseEnvelope> HandleAsync(string message)
    {
        var parsed = RequestParser.Parse(message);
        if (!parsed.IsAccepted)
        {
            _logger.Warning("Rejected page message: {Reason}", parsed.Rejection!.Error!.Message);
            return parsed.Rejection;
        }

        var request = parsed.Request!;
        var stopwatch = Stopwatch.StartNew();
        var response = await _dispatcher.DispatchAsync(request);
        _logger.Debug("{Command} {Id} -> {Outcome} in {ElapsedMs} ms",
            request.Command, request.Id, response.Ok ? "ok" : response.Error!.Code, stopwatch.ElapsedMilliseconds);
        return response;
    }
}
