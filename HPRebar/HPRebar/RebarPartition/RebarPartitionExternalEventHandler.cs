using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.RebarPartition.Model;
using HPRebar.RebarPartition.Service;
using Serilog;

namespace HPRebar.RebarPartition;

public enum RebarPartitionRequestKind
{
    Preview,
    Apply
}

public sealed class RebarPartitionRequest
{
    public RebarPartitionRequestKind Kind { get; }
    public RebarPartitionSettings? Settings { get; set; }
    public IReadOnlyList<RebarPartitionPreviewItem>? ItemsToApply { get; set; }
    public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public RebarPartitionRequest(RebarPartitionRequestKind kind) => Kind = kind;
}

/// <summary>
/// Dispatches UI thread requests to Revit's API main thread safely via ExternalEvent.
/// </summary>
public sealed class RebarPartitionExternalEventHandler : IExternalEventHandler, IDisposable
{
    private readonly ConcurrentQueue<RebarPartitionRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;
    private readonly Document _document;
    private readonly IRebarPartitionService _service;

    public RebarPartitionExternalEventHandler(Document document, IRebarPartitionService service)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _externalEvent = ExternalEvent.Create(this);
    }

    public async Task<IReadOnlyList<RebarPartitionPreviewItem>> BuildPreviewAsync(RebarPartitionSettings settings)
    {
        var req = new RebarPartitionRequest(RebarPartitionRequestKind.Preview) { Settings = settings };
        var res = await EnqueueAsync(req);
        return res as IReadOnlyList<RebarPartitionPreviewItem> ?? Array.Empty<RebarPartitionPreviewItem>();
    }

    public async Task<RebarPartitionResult> ApplyAsync(IReadOnlyList<RebarPartitionPreviewItem> items, RebarPartitionSettings settings)
    {
        var req = new RebarPartitionRequest(RebarPartitionRequestKind.Apply)
        {
            ItemsToApply = items,
            Settings = settings
        };
        var res = await EnqueueAsync(req);
        return res as RebarPartitionResult ?? RebarPartitionResult.Failed("Revit không trả về kết quả.");
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

                object? result = request.Kind switch
                {
                    RebarPartitionRequestKind.Preview => _service.BuildPreview(_document, uidoc, request.Settings!),
                    RebarPartitionRequestKind.Apply => _service.ApplyPartitions(_document, request.ItemsToApply!, request.Settings),
                    _ => throw new NotSupportedException($"Yêu cầu {request.Kind} không được hỗ trợ.")
                };

                request.Completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi xử lý ExternalEvent trong RebarPartition");
                request.Completion.TrySetException(ex);
            }
        }
    }

    public string GetName() => "HPRebar.RebarPartitionExternalEventHandler";

    public void Dispose()
    {
        _externalEvent.Dispose();
    }

    private Task<object?> EnqueueAsync(RebarPartitionRequest request)
    {
        _queue.Enqueue(request);
        _externalEvent.Raise();
        return request.Completion.Task;
    }
}
