using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.JoinElements.Model;
using HPRebar.JoinElements.Service;
using Serilog;

namespace HPRebar.JoinElements;

public sealed class JoinElementRequest
{
    public JoinElementSettings Settings { get; }
    public IProgress<JoinProgressReport>? Progress { get; }
    public TaskCompletionSource<JoinElementResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public JoinElementRequest(JoinElementSettings settings, IProgress<JoinProgressReport>? progress = null)
    {
        Settings = settings;
        Progress = progress;
    }
}

/// <summary>
/// Dispatches Auto Join execution requests safely to Revit's API main thread via ExternalEvent.
/// </summary>
public sealed class JoinElementExternalEventHandler : IExternalEventHandler, IDisposable
{
    private readonly ConcurrentQueue<JoinElementRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;
    private readonly Document _document;
    private readonly IJoinElementService _service;

    public JoinElementExternalEventHandler(Document document, IJoinElementService service)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _externalEvent = ExternalEvent.Create(this);
    }

    public async Task<JoinElementResult> ExecuteJoinAsync(
        JoinElementSettings settings,
        IProgress<JoinProgressReport>? progress = null)
    {
        var req = new JoinElementRequest(settings, progress);
        _queue.Enqueue(req);
        _externalEvent.Raise();
        return await req.Completion.Task;
    }

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        while (_queue.TryDequeue(out var request))
        {
            try
            {
                if (uidoc is null)
                    throw new InvalidOperationException("Không có dự án Revit nào đang mở.");

                if (!uidoc.Document.Equals(_document))
                    throw new InvalidOperationException($"Mô hình hiện hành là '{uidoc.Document.Title}', không khớp với '{_document.Title}' của cửa sổ đang mở.");

                var result = _service.ExecuteJoin(_document, uidoc, request.Settings, request.Progress);
                request.Completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi xử lý ExternalEvent trong JoinElements");
                request.Completion.TrySetException(ex);
            }
        }
    }

    public string GetName() => "HPRebar.JoinElementExternalEventHandler";

    public void Dispose()
    {
        _externalEvent.Dispose();
    }
}
