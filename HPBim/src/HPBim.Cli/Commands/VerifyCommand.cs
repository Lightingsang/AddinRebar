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
using HPBim.Client.Pipeline;
using HPBim.Client.Verification;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts;

public sealed class VerifyCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitModelVerifier? _injectedVerifier;
    private readonly IMeasuredGeometricVerifier? _injectedGeometricVerifier;
    private readonly IBimCoordinationLedgerService? _injectedLedgerService;

    public VerifyCommand(
        ConsoleFormatter? formatter = null,
        IRevitModelVerifier? injectedVerifier = null,
        IMeasuredGeometricVerifier? injectedGeometricVerifier = null,
        IBimCoordinationLedgerService? injectedLedgerService = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedVerifier = injectedVerifier;
        _injectedGeometricVerifier = injectedGeometricVerifier;
        _injectedLedgerService = injectedLedgerService;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string? inputPath = args.GetOption("input");
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            _formatter.WriteError("Tập tin --input <path> không hợp lệ hoặc không tồn tại.");
            return 2;
        }

        string pipeName = args.GetOption("pipe", PipeNaming.For(2026));
        string ledgerPath = args.GetOption("ledger", "state/bim_coordination_ledger.json");
        _formatter.WriteBanner("Verify Revit Model", $"Đối soát 1:1 phần tử & hình học Revit so với schema: {inputPath}");
        var model = await BimModelSerializer.DeserializeFromFileAsync(inputPath, ct).ConfigureAwait(false);

        return await RunVerificationAsync(model, pipeName, ledgerPath, ct).ConfigureAwait(false);
    }

    private async Task<int> RunVerificationAsync(BimModel model, string pipeName, string ledgerPath, CancellationToken ct)
    {
        IRevitPipeClient? pipeClient = null;
        try
        {
            var (verifier, geomVerifier) = ResolveVerifiers(pipeName, out pipeClient);
            var smokeReport = await verifier.VerifyAsync(model, ct).ConfigureAwait(false);
            RenderSmokeReport(smokeReport);

            if (!smokeReport.IsMatch)
            {
                _formatter.WriteError($"PHÁT HIỆN SAI LỆCH SỐ LƯỢNG ({smokeReport.Discrepancies.Count} lỗi)");
                return 1;
            }

            if (geomVerifier != null)
            {
                var ledgerService = ResolveLedgerService(ledgerPath);
                var plan = ExecutionPlanBuilder.Build(model, targetDoc: null, options: new ExecutionOptions());
                plan = PopulateTargetUniqueIds(plan, ledgerService);
                var gr = await geomVerifier.VerifyPlanGeometryAsync(plan, cancellationToken: ct).ConfigureAwait(false);
                RenderGeometricReport(gr);
                if (!gr.OverallPassed)
                {
                    _formatter.WriteError($"NGHIỆM THU HÌNH HỌC THẤT BẠI: {gr.DefectCount} khuyết tật, {gr.InconclusiveCount} chưa rõ, {gr.MissingCount} thiếu.");
                    return 1;
                }
            }

            _formatter.WriteSuccess("ĐỐI SOÁT HOÀN HẢO: 100% số lượng cấu kiện và đo đạc hình học trong Revit khớp tuyệt đối.");
            return 0;
        }
        finally
        {
            if (pipeClient != null) await pipeClient.DisposeAsync().ConfigureAwait(false);
        }
    }

    private IBimCoordinationLedgerService ResolveLedgerService(string ledgerPath)
    {
        if (_injectedLedgerService != null) return _injectedLedgerService;
        var svc = new BimCoordinationLedgerService();
        if (File.Exists(ledgerPath))
        {
            svc.LoadFromFile(ledgerPath);
        }
        return svc;
    }

    private static BimExecutionPlan PopulateTargetUniqueIds(BimExecutionPlan plan, IBimCoordinationLedgerService ledgerService)
    {
        if (plan.Items.Count == 0 || ledgerService == null) return plan;
        var populated = new List<ElementPlanItem>(plan.Items.Count);
        foreach (var item in plan.Items)
        {
            if (string.IsNullOrEmpty(item.TargetUniqueId) && item.SourceKey != null &&
                TryLookupTarget(ledgerService, item.SourceKey, out string? uid) && !string.IsNullOrEmpty(uid))
            {
                populated.Add(item with { TargetUniqueId = uid });
            }
            else
            {
                populated.Add(item);
            }
        }
        return plan with { Items = populated };
    }

    private static bool TryLookupTarget(IBimCoordinationLedgerService ledgerService, CompositeSourceKey key, out string? uniqueId)
    {
        if (ledgerService is BimCoordinationLedgerService concrete)
        {
            return concrete.TryGetTarget(key, out uniqueId);
        }
        return ledgerService.Ledger.TryGetTarget(key, out uniqueId);
    }

    private (IRevitModelVerifier Verifier, IMeasuredGeometricVerifier? GeomVerifier) ResolveVerifiers(string pipeName, out IRevitPipeClient? pipeClient)
    {
        if (_injectedVerifier != null)
        {
            pipeClient = null;
            return (_injectedVerifier, _injectedGeometricVerifier);
        }

        pipeClient = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName });
        return (new RevitModelVerifier(pipeClient), _injectedGeometricVerifier ?? new MeasuredGeometricVerifier(pipeClient));
    }

    private void RenderSmokeReport(ModelVerificationReport r)
    {
        var rows = new List<string[]>
        {
            new[] { "Levels", r.ExpectedLevels.ToString(), r.ActualLevels.ToString(), (r.ActualLevels - r.ExpectedLevels).ToString() },
            new[] { "Grids", r.ExpectedGrids.ToString(), r.ActualGrids.ToString(), (r.ActualGrids - r.ExpectedGrids).ToString() },
            new[] { "Columns", r.ExpectedColumns.ToString(), r.ActualColumns.ToString(), (r.ActualColumns - r.ExpectedColumns).ToString() },
            new[] { "Walls", r.ExpectedWalls.ToString(), r.ActualWalls.ToString(), (r.ActualWalls - r.ExpectedWalls).ToString() },
            new[] { "Doors", r.ExpectedDoors.ToString(), r.ActualDoors.ToString(), (r.ActualDoors - r.ExpectedDoors).ToString() },
            new[] { "Windows", r.ExpectedWindows.ToString(), r.ActualWindows.ToString(), (r.ActualWindows - r.ExpectedWindows).ToString() },
            new[] { "Rooms", r.ExpectedRooms.ToString(), r.ActualRooms.ToString(), (r.ActualRooms - r.ExpectedRooms).ToString() },
            new[] { "Beams", r.ExpectedBeams.ToString(), r.ActualBeams.ToString(), (r.ActualBeams - r.ExpectedBeams).ToString() },
            new[] { "Slabs", r.ExpectedSlabs.ToString(), r.ActualSlabs.ToString(), (r.ActualSlabs - r.ExpectedSlabs).ToString() },
            new[] { "Pipes", r.ExpectedPipes.ToString(), r.ActualPipes.ToString(), (r.ActualPipes - r.ExpectedPipes).ToString() },
            new[] { "Rebars", r.ExpectedRebars.ToString(), r.ActualRebars.ToString(), (r.ActualRebars - r.ExpectedRebars).ToString() }
        };

        _formatter.RenderTable(["Hạng mục", "Thiết kế (Schema)", "Thực tế Revit", "Chênh lệch"], rows);
        if (!r.IsMatch)
        {
            foreach (var d in r.Discrepancies) _formatter.WriteWarning($" - {d}");
        }
    }

    private void RenderGeometricReport(GeometricAuditReport gr)
    {
        _formatter.WriteBanner("Kiểm toán Hình học Thực tế (Measured QA/QC)", gr.OverallPassed ? "100% ĐẠT TIÊU CHÍ HÌNH HỌC" : "CÓ SAI LỆCH HÌNH HỌC");
        var rows = new List<string[]>
        {
            new[] { "Tổng cấu kiện", gr.TotalPlannedElements.ToString() },
            new[] { "Đo đạc thành công (Verified)", gr.VerifiedCount.ToString() },
            new[] { "Khuyết tật hình học (Defect)", gr.DefectCount.ToString() },
            new[] { "Chưa đủ cơ sở đo (Inconclusive)", gr.InconclusiveCount.ToString() },
            new[] { "Cấu kiện thiếu trong RVT (Missing)", gr.MissingCount.ToString() }
        };
        _formatter.RenderTable(["Chỉ số Kiểm toán", "Số lượng"], rows);

        if (gr.SummaryDiscrepancies.Count > 0)
        {
            _formatter.WriteWarning("Danh sách sai lệch hình học:");
            foreach (var d in gr.SummaryDiscrepancies.Take(10)) _formatter.WriteWarning($" - {d}");
        }
    }
}
