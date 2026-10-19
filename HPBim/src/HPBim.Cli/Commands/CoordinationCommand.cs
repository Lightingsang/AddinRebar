namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts;

/// <summary>
/// CLI command managing the BIM Coordination Ledger, 7-step lifecycle tracking,
/// FailFastGate audits, and Extensible Storage reconciliation.
/// </summary>
public sealed class CoordinationCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly BimCoordinationLedgerService _ledgerService;
    private readonly IReconcileService? _reconcileService;
    private readonly IRevitPipeClient? _pipeClient;
    private readonly IIntentJournal? _journal;

    public CoordinationCommand(
        ConsoleFormatter? formatter = null,
        BimCoordinationLedgerService? service = null,
        IReconcileService? reconcileService = null,
        IRevitPipeClient? pipeClient = null,
        IIntentJournal? journal = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _ledgerService = service ?? new BimCoordinationLedgerService();
        _reconcileService = reconcileService;
        _pipeClient = pipeClient;
        _journal = journal;
    }

    public Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string subCommand = args.UnmatchedArguments.Count > 0
            ? args.UnmatchedArguments[0].ToLowerInvariant()
            : args.GetOption("action", "status").ToLowerInvariant();
        string ledgerPath = ResolveLedgerPath(args);

        _formatter.WriteBanner("BIM Coordination Ledger", $"Quản trị Sổ cái & Vòng đời 7 bước [{subCommand}]");

        return subCommand switch
        {
            "init" => Task.FromResult(ExecuteInit(ledgerPath, args)),
            "status" => Task.FromResult(ExecuteStatus(ledgerPath)),
            "check" or "validate" => Task.FromResult(ExecuteCheck(ledgerPath, args)),
            "reconcile" => ExecuteReconcileAsync(ledgerPath, args, ct),
            _ => Task.FromResult(ExecuteHelp())
        };
    }

    private int ExecuteInit(string path, CliParseResult args)
    {
        string project = args.GetOption("project", "KC-HT-90-TLTT-16 (Hoi Truong Tan Dong Hiep)");
        _ledgerService.Initialize(project);
        _ledgerService.SaveToFile(path);
        _formatter.WriteSuccess($"Đã khởi tạo Sổ cái điều phối chuẩn tại: {path}");
        return 0;
    }

    private int ExecuteStatus(string path)
    {
        if (!File.Exists(path))
        {
            _formatter.WriteError($"Không tìm thấy file Sổ cái tại: {path}");
            return 1;
        }

        var ledger = _ledgerService.LoadFromFile(path);
        _formatter.WriteInfo($"Dự án: {ledger.Project}");
        _formatter.WriteInfo($"Phân đoạn hiện tại: {ledger.ActivePhase} ({ledger.PhaseStatus})");
        _formatter.WriteInfo($"Cổng chất lượng: {ledger.QualityGate}");
        _formatter.WriteInfo($"Nguyên Tắc Vàng (ĐIỀU 0): {ledger.PrimeDirective}");

        var s = ledger.Summary;
        _formatter.WriteInfo("\n--- TIẾN ĐỘ 7 BƯỚC VÒNG ĐỜI CẤU KIỆN ---");
        _formatter.WriteInfo($"  1. Đọc bản vẽ CAD (Step 1):        {s.Step1_ReadCad, 4} cấu kiện");
        _formatter.WriteInfo($"  2. Kiểm tra & Phân loại (Step 2):   {s.Step2_CheckAndClassify, 4} cấu kiện");
        _formatter.WriteInfo($"  3. Dựng mô hình Revit (Step 3):     {s.Step3_ModelRevit, 4} cấu kiện");
        _formatter.WriteInfo($"  4. Kiểm toán độc lập QA/QC (Step 4):{s.Step4_QaqcAudit, 4} cấu kiện");
        _formatter.WriteInfo($"  5. Định tuyến sửa lỗi (Step 5):    {s.Step5_RouteFix, 4} cấu kiện");
        _formatter.WriteInfo($"  6. Tái kiểm toán QA/QC (Step 6):   {s.Step6_ReAudit, 4} cấu kiện");
        _formatter.WriteInfo($"  7. Xác nhận đạt 100% (Step 7):      {s.Step7_ConfirmedPass, 4} cấu kiện");

        _formatter.WriteInfo("\n--- PHÂN HẠNG SẴN SÀNG KỸ THUẬT ---");
        _formatter.WriteInfo($"  Nhóm 1 (Đủ điều kiện dựng Revit):   {s.Group1_ReadyToModel, 4} cấu kiện");
        _formatter.WriteInfo($"  Nhóm 2 (Mâu thuẫn thông tin - RFI): {s.Group2_ConflictingInfo, 4} cấu kiện");
        _formatter.WriteInfo($"  Nhóm 3 (Thiếu thông tin - Tạm dừng):{s.Group3_MissingInfo, 4} cấu kiện");
        _formatter.WriteInfo($"  Tổng số cấu kiện theo dõi:          {s.TotalElements, 4} cấu kiện");

        return 0;
    }

    private int ExecuteCheck(string path, CliParseResult args)
    {
        if (!File.Exists(path))
        {
            _formatter.WriteError($"Không tìm thấy file Sổ cái tại: {path}");
            return 1;
        }

        var ledger = _ledgerService.LoadFromFile(path);
        string elementId = args.GetOption("id", "");
        var gate = new FailFastGate();

        if (string.IsNullOrWhiteSpace(elementId))
        {
            int pass = 0, fail = 0;
            foreach (var el in ledger.Elements)
            {
                var res = gate.Evaluate(el);
                if (res.IsPassed) pass++;
                else fail++;
            }
            _formatter.WriteSuccess($"Kiểm tra toàn bộ Sổ cái: {pass} ĐẠT, {fail} VI PHẠM Fail-Fast Gate.");
            return fail > 0 ? 1 : 0;
        }

        var element = ledger.Elements.FirstOrDefault(e => e.Id.Equals(elementId, StringComparison.OrdinalIgnoreCase));
        if (element == null)
        {
            _formatter.WriteError($"Không tìm thấy cấu kiện '{elementId}' trong Sổ cái.");
            return 1;
        }

        var result = gate.Evaluate(element);
        if (result.IsPassed)
        {
            _formatter.WriteSuccess($"Cấu kiện '{elementId}' ({element.Code}): ĐỦ CƠ SỞ KỸ THUẬT (PASS Fail-Fast Gate)");
            return 0;
        }

        _formatter.WriteError($"Cấu kiện '{elementId}' ({element.Code}): VI PHẠM TIÊU CHUẨN KỸ THUẬT (FAIL):");
        foreach (var v in result.Violations)
        {
            Console.WriteLine($"  - {v}");
        }
        return 1;
    }

    private async Task<int> ExecuteReconcileAsync(string ledgerPath, CliParseResult args, CancellationToken ct)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string? journalPath = args.GetOption("journal");
        string? opStr = args.GetOption("op");

        if (File.Exists(ledgerPath))
        {
            _ledgerService.LoadFromFile(ledgerPath);
        }
        else
        {
            InitializeDefaultLedger(ledgerPath);
        }

        using var localJournal = _journal == null ? new IntentJournal(journalPath) : null;
        var journalToUse = _journal ?? localJournal!;

        var pipeClient = _pipeClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 5000,
            DefaultExecuteTimeoutSeconds = 60
        });

        bool disposeClient = _pipeClient == null;
        try
        {
            var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService, ledgerFilePath: ledgerPath);
            return await RunReconciliationLoopAsync(service, opStr, ledgerPath, ct).ConfigureAwait(false);
        }
        finally
        {
            if (disposeClient)
            {
                await pipeClient.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<int> RunReconciliationLoopAsync(
        IReconcileService service,
        string? opStr,
        string ledgerPath,
        CancellationToken ct)
    {
        IReadOnlyList<ReconcileResult> results;
        if (Guid.TryParse(opStr, out var opId))
        {
            var single = await service.ReconcileIntentAsync(opId, ct).ConfigureAwait(false);
            results = new[] { single };
        }
        else
        {
            results = await service.ReconcileAllUnknownIntentsAsync(ct).ConfigureAwait(false);
        }

        return ProcessReconcileResults(results, ledgerPath);
    }

    private int ProcessReconcileResults(IReadOnlyList<ReconcileResult> results, string ledgerPath)
    {
        int committedCount = 0;
        int unknownCount = 0;

        foreach (var r in results)
        {
            switch (r.Status)
            {
                case ReconcileStatus.Committed:
                    foreach (var (keyStr, uniqueId) in r.RecoveredMappings)
                    {
                        if (CompositeSourceKey.TryParse(keyStr, out var sourceKey) && sourceKey != null)
                        {
                            _ledgerService.UpdateMapping(sourceKey, uniqueId, r.PlanHash, ElementExecutionStatus.Created);
                        }
                    }
                    _formatter.WriteSuccess($"[COMMITTED] Intent {r.OperationId}: Đã khôi phục {r.RecoveredMappings.Count} mappings từ Extensible Storage.");
                    committedCount++;
                    break;
                case ReconcileStatus.RolledBack:
                    _formatter.WriteInfo($"[ROLLED_BACK] Intent {r.OperationId}: Giao dịch không tồn tại trong Revit (đã rollback an toàn).");
                    break;
                case ReconcileStatus.StillUnknown:
                    _formatter.WriteError($"[UNKNOWN] Intent {r.OperationId}: Chưa xác định - {r.Message}");
                    unknownCount++;
                    break;
                case ReconcileStatus.NoOp:
                    _formatter.WriteInfo("Không có intent nào ở trạng thái chờ đối soát (All clear).");
                    break;
            }
        }

        if (committedCount > 0)
        {
            _ledgerService.SaveToFile(ledgerPath);
            _formatter.WriteSuccess($"Đã đồng bộ và lưu Sổ cái cập nhật tại: {ledgerPath}");
        }

        return unknownCount > 0 ? 1 : 0;
    }

    private void InitializeDefaultLedger(string ledgerPath)
    {
        _ledgerService.Initialize("HPBim Coordination Project");
        _ledgerService.SaveToFile(ledgerPath);
    }

    private int ExecuteHelp()
    {
        Console.WriteLine("""
        Sử dụng lệnh coord:
          hpbim coord status    [--ledger <path>]                          Xem bảng tóm tắt tiến độ 7 bước và 3 nhóm kỹ thuật
          hpbim coord init      [--project <name>] [--ledger <path>]       Khởi tạo file Sổ cái điều phối trung tâm mới
          hpbim coord check     [--id <elementId>] [--ledger <path>]       Kiểm tra Fail-Fast Gate cho cấu kiện
          hpbim coord reconcile [--pipe <name>] [--journal <path>] [--op <id>]  Tự động đối soát Extensible Storage và phục hồi Sổ cái
        """);
        return 0;
    }

    private static string ResolveLedgerPath(CliParseResult args)
    {
        string opt = args.GetOption("ledger", "");
        if (!string.IsNullOrWhiteSpace(opt)) return opt;

        if (File.Exists(Path.Combine("state", "bim_coordination_ledger.json")))
            return Path.Combine("state", "bim_coordination_ledger.json");

        if (File.Exists(Path.Combine("HPBim", "state", "bim_coordination_ledger.json")))
            return Path.Combine("HPBim", "state", "bim_coordination_ledger.json");

        return Path.Combine("state", "bim_coordination_ledger.json");
    }
}
