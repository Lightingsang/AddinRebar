namespace HPBim.Cli.Commands;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Core.Qc;
using HPRebar.Mcp.Contracts;

/// <summary>
/// 100% C# command to execute the authentic 8-step QA/QC audit for Foundation Phase against live Revit 2026.
/// Adheres strictly to ĐIỀU 0: Zero heuristic guessing, mathematical verification only.
/// </summary>
public sealed class AuditFoundationCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public AuditFoundationCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        _formatter.WriteBanner("Authentic Foundation QA/QC Audit", "Kiểm toán độc lập 8 bước phân đoạn Móng (100% C#)");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Revit Named Pipe: {pipeName}");

            var (grids, levels) = await RevitFoundationQueryService.QueryGridsAndLevelsAsync(client, ct).ConfigureAwait(false);
            var piles = await RevitFoundationQueryService.QueryPilesAsync(client, ct).ConfigureAwait(false);
            var footings = await RevitFoundationQueryService.QueryFootingsAsync(client, ct).ConfigureAwait(false);
            var beams = await RevitFoundationQueryService.QueryBeamsAsync(client, ct).ConfigureAwait(false);

            _formatter.WriteInfo($"Đã đọc từ Revit: {grids.Count} trục, {levels.Count} cao trình, {piles.Count} cọc, {footings.Count} đài móng, {beams.Count} giằng móng.");

            var engine = new FoundationQcEngine();
            var report = engine.ExecuteFullAudit(grids, levels, piles, footings, beams);

            PrintAuditResults(report);
            await FoundationAuditDossierWriter.SaveAuditArtifactsAsync(report, grids, levels, piles, footings, beams, ct).ConfigureAwait(false);

            return report.AllPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kiểm toán: {ex.Message}");
            return 99;
        }
    }

    private void PrintAuditResults(FoundationAuditReport report)
    {
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("  KẾT QUẢ KIỂM TOÁN ĐỘC LẬP 8 BƯỚC PHÂN ĐOẠN MÓNG (C# NATIVE)");
        Console.WriteLine("================================================================================");
        foreach (var step in report.Steps)
        {
            string tag = step.Passed ? "[PASS]" : "[FAIL]";
            Console.WriteLine($"{tag} Bước {step.StepNumber}: {step.StepName}");
            Console.WriteLine($"       {step.Summary}");
            if (!step.Passed && step.Errors.Count > 0)
            {
                foreach (var err in step.Errors.Take(5))
                {
                    Console.WriteLine($"       - Lỗi: {err}");
                }
                if (step.Errors.Count > 5)
                {
                    Console.WriteLine($"       - ... và còn {step.Errors.Count - 5} lỗi khác.");
                }
            }
        }
        Console.WriteLine("================================================================================");
        string finalVerdict = report.AllPassed ? "ĐẠT NGHIỆM THU 100% (PASS)" : "CHƯA ĐẠT (CẦN KHẮC PHỤC HÌNH HỌC)";
        Console.WriteLine($"  KẾT LUẬN TOÀN DIỆN: {finalVerdict}");
        Console.WriteLine("================================================================================\n");
    }
}
