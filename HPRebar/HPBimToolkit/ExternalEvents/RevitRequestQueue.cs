using System.Collections.Concurrent;
using Autodesk.Revit.UI;
using Serilog;

namespace HPBimToolkit.ExternalEvents;

/// <summary>
///     The add-in's one road onto Revit's API thread. Work queued from the WebView (or any modeless code) waits here
///     until Revit calls <see cref="Execute" />, which is the only place the API is legal outside a command or event.
/// </summary>
public sealed class RevitRequestQueue : IExternalEventHandler, IDisposable
{
    private readonly ConcurrentQueue<IRevitRequest> _pending = new();
    private readonly ExternalEvent _externalEvent;
    private readonly ILogger _logger;
    private bool _isDisposed;

    /// <remarks>Must be constructed in a Revit API context (OnStartup or a command): ExternalEvent.Create demands it.</remarks>
    public RevitRequestQueue(ILogger logger)
    {
        _logger = logger;
        _externalEvent = ExternalEvent.Create(this);
    }

    /// <summary>
    ///     Queues <paramref name="work" /> for Revit's API thread. The task completes when it ran, threw, was cancelled
    ///     before it started, or Revit refused the event; a cancelled request that already started still runs to the end.
    /// </summary>
    public Task<T> RunAsync<T>(Func<UIApplication, T> work, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var request = new RevitRequest<T>(work, cancellationToken, _logger);
        _pending.Enqueue(request);

        var outcome = _externalEvent.Raise();
        if (outcome is not (ExternalEventRequest.Accepted or ExternalEventRequest.Pending))
        {
            // Execute skips a request that is already completed, so it never runs after this refusal.
            request.Fail(new InvalidOperationException($"Revit refused the external event ({outcome})."));
        }

        return request.Task;
    }

    public void Execute(UIApplication app)
    {
        while (_pending.TryDequeue(out var request))
        {
            request.Run(app);
        }
    }

    public string GetName() => "HP BIM Toolkit";

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        while (_pending.TryDequeue(out var request))
        {
            request.Fail(new ObjectDisposedException(nameof(RevitRequestQueue)));
        }

        _externalEvent.Dispose();
    }

    private interface IRevitRequest
    {
        void Run(UIApplication app);

        void Fail(Exception exception);
    }

    private sealed class RevitRequest<T>(Func<UIApplication, T> work, CancellationToken cancellationToken, ILogger logger)
        : IRevitRequest
    {
        private readonly TaskCompletionSource<T> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<T> Task => _completion.Task;

        public void Run(UIApplication app)
        {
            if (_completion.Task.IsCompleted)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _completion.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                _completion.TrySetResult(work(app));
            }
            catch (Exception exception)
            {
                logger.Debug(exception, "Revit request failed");
                _completion.TrySetException(exception);
            }
        }

        public void Fail(Exception exception) => _completion.TrySetException(exception);
    }
}
