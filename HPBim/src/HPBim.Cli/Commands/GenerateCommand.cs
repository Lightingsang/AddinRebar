namespace HPBim.Cli.Commands;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Validation;
using HPRebar.Mcp.Contracts;

public sealed class GenerateCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public GenerateCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string? inputPath = args.GetOption("input");
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            _formatter.WriteError("Tập tin --input <path> không hợp lệ hoặc không tồn tại.");
            return 2;
        }

        bool dryRun = args.HasFlag("dry-run");
        string pipeName = args.GetOption("pipe", PipeNaming.For(2026));

        _formatter.WriteBanner("Generate BIM Model", $"Dispatch 8-Stage Pipeline sang Revit 2026 (Pipe: {pipeName})");
        BimModel model;
        try
        {
            model = await BimModelSerializer.DeserializeValidatedFromFileAsync(inputPath, requireStrictDiscriminant: true, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (SchemaValidationException ex)
        {
            _formatter.WriteError("Pre-flight Schema Validation thất bại! Dữ liệu không đạt chuẩn BIM.");
            foreach (var err in ex.ValidationErrors)
            {
                _formatter.WriteError($"  - {err}");
            }
            return 1;
        }

        var qcReport = new QcEngine().Validate(model);
        if (!qcReport.IsValid)
        {
            _formatter.WriteError("Pre-flight QC thất bại! Không thể dispatch mô hình có lỗi quy chuẩn sang Revit.");
            return 1;
        }

        string ledgerPath = args.GetOption("ledger", Path.Combine("state", "bim_coordination_ledger.json"));
        return await RunPipelineAsync(model, dryRun, pipeName, ledgerPath, ct).ConfigureAwait(false);
    }

    private async Task<DocumentIdentitySnapshot?> ResolveTargetDocumentAsync(
        IRevitPipeClient client, bool dryRun, CancellationToken ct)
    {
        try
        {
            var query = new DocumentIdentityQuery(client);
            var snapshot = await query.QueryActiveDocumentAsync(dryRun, ct).ConfigureAwait(false);
            if (snapshot == null || snapshot.IsEmpty)
            {
                _formatter.WriteError("[Target Document] Không thể xác định tài liệu Revit đang hoạt động (snapshot rỗng).");
                return null;
            }

            _formatter.WriteInfo($"[Target Document] Đã phát hiện tài liệu Revit: '{snapshot.Title}' (VersionGUID: {snapshot.VersionGuid:D})");
            return snapshot;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"[Target Document] Lỗi khi truy vấn tài liệu Revit: {ex.Message}");
            return null;
        }
    }

    private async Task<int> RunPipelineAsync(BimModel model, bool dryRun, string pipeName, string ledgerPath, CancellationToken ct)
    {
        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName });
        using var journal = dryRun ? null : new IntentJournal();
        var ledgerService = ConfigureLedgerService(ledgerPath);

        try
        {
            var targetDoc = await ResolveTargetDocumentAsync(client, dryRun, ct).ConfigureAwait(false);
            if (targetDoc == null) return HandleTargetDocMissing(dryRun);

            var pipeline = new BimPipelineEngine(client)
            {
                IntentJournal = journal,
                LedgerService = ledgerService,
                LedgerFilePath = dryRun ? null : ledgerPath
            };
            pipeline.StageCompleted += stage =>
            {
                string status = stage.Success ? "OK" : "FAILED";
                _formatter.WriteInfo($"[Stage {stage.StageIndex}] {stage.StageName}: {status} ({stage.DurationMs}ms, +{stage.Changed.Added} elements)");
            };

            var options = new BimPipelineOptions
            {
                DryRun = dryRun,
                AtomicDryRun = dryRun,
                UseExecutionPlan = true,
                TargetDoc = targetDoc
            };
            var result = await pipeline.ExecuteAsync(model, options, ct).ConfigureAwait(false);

            RenderPipelineResult(result, dryRun);
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                _formatter.WriteError($"Chi tiết lỗi từ Revit: {result.ErrorMessage}");
            }
            bool isOk = dryRun ? (result.Success && result.RolledBack) : result.Success;
            return isOk ? 0 : 1;
        }
        finally
        {
            if (_injectedClient == null) await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private int HandleTargetDocMissing(bool dryRun)
    {
        RenderPipelineResult(new BimPipelineResult
        {
            Success = false,
            ErrorMessage = "Không thể xác định tài liệu Revit đang hoạt động trước khi thực thi."
        }, dryRun);
        return 1;
    }

    private BimCoordinationLedgerService ConfigureLedgerService(string ledgerPath)
    {
        var ledgerService = new BimCoordinationLedgerService();
        if (File.Exists(ledgerPath))
        {
            ledgerService.LoadFromFile(ledgerPath);
            _formatter.WriteInfo($"[Ledger] Đã nạp sổ cái điều phối từ: {ledgerPath} ({ledgerService.Mappings.Count} mappings)");
        }
        else
        {
            ledgerService.Initialize("HPBim Coordination Project");
            _formatter.WriteInfo($"[Ledger] Khởi tạo sổ cái điều phối mới tại: {ledgerPath}");
        }

        return ledgerService;
    }

    private void RenderPipelineResult(BimPipelineResult result, bool dryRun)
    {
        string[][] summaryRows =
        [
            ["Tổng thời gian", $"{result.TotalDurationMs} ms"],
            ["Tổng element tạo mới", result.TotalElementsAdded.ToString()],
            ["Tổng element chỉnh sửa", result.TotalElementsModified.ToString()],
            ["Trạng thái Rollback", result.RolledBack ? "CÓ (Transaction Rolled Back)" : "KHÔNG (Committed)"]
        ];
        _formatter.RenderTable(["Thông số Pipeline", "Kết quả"], summaryRows);

        if (dryRun)
        {
            if (result.Success && result.RolledBack)
            {
                _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Toàn bộ 8 stages đã được kiểm thử và rollback 100% an toàn.");
            }
            else if (!result.RolledBack)
            {
                _formatter.WriteError($"CẢNH BÁO NGUY HIỂM: Dry-run thất bại! Revit bridge KHÔNG xác nhận rollback: {result.ErrorMessage}");
            }
            else
            {
                _formatter.WriteError($"DRY-RUN THẤT BẠI: {result.ErrorMessage}");
            }
        }
        else if (result.Success)
        {
            _formatter.WriteSuccess("DỰNG HÌNH HOÀN TẤT: Mô hình BIM 3D bản địa đã được tạo thành công trong Revit 2026.");
        }
        else
        {
            _formatter.WriteError($"DỰNG HÌNH THẤT BẠI: {result.ErrorMessage}");
        }
    }
}
