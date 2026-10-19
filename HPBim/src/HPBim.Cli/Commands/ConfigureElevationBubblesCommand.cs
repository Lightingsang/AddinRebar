namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to configure grid bubbles on elevation and section views:
///     show bubble at the bottom (lower Z) and hide bubble at the top (higher Z).
/// </summary>
public sealed class ConfigureElevationBubblesCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public ConfigureElevationBubblesCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Configure Elevation Bubbles", $"Cấu hình Bong bóng Lưới Trục Mặt đứng (Chỉ hiện dưới, ẩn trên) [{modeLabel}]");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Revit Named Pipe: {pipeName}");

            var result = await DispatchRevitScriptAsync(client, dryRun, ct).ConfigureAwait(false);
            RenderExecutionResult(result, dryRun);
            return result.IsError ? 1 : 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi cấu hình bong bóng mặt đứng: {ex.Message}");
            return 1;
        }
        finally
        {
            if (_injectedClient == null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<ExecuteResult> DispatchRevitScriptAsync(
        IRevitPipeClient client, bool dryRun, CancellationToken ct)
    {
        _formatter.WriteInfo("Đang gửi C# Roslyn Script cấu hình hiển thị bong bóng mặt đứng sang Revit 2026...");
        string scriptCode = BuildRevitScript();

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 45,
            Label: "Configure Grid Elevation Bubbles (Bottom Only)");

        return await client.ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    private void RenderExecutionResult(ExecuteResult result, bool dryRun)
    {
        if (result.IsError)
        {
            string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                ? string.Join("; ", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                : "Không có diagnostics";
            _formatter.WriteError($"Revit trả về lỗi: {result.Message} -> {diags}");
            return;
        }

        string[][] summaryRows =
        [
            ["Thời gian thực thi", $"{result.DurationMs} ms"],
            ["Quy tắc hiển thị Mặt đứng/Mặt cắt", "CHỈ HIỆN PHÍA DƯỚI (Chân trục), ẨN PHÍA TRÊN (Đỉnh trục)"],
            ["Quy tắc hiển thị Mặt bằng", "GIỮ NGUYÊN 2 ĐẦU (Không thay đổi)"],
            ["Phần tử sửa đổi (Modified)", result.Changed.Modified.ToString()],
            ["Trạng thái Rollback", result.RolledBack ? "CÓ (Transaction Rolled Back sạch sẽ)" : "KHÔNG (Committed vào Revit)"]
        ];
        _formatter.RenderTable(["Chỉ số", "Kết quả thực tế"], summaryRows);

        if (result.Value.HasValue)
        {
            _formatter.WriteInfo($"Revit Data: {result.Value.Value.GetRawText()}");
        }

        if (dryRun)
        {
            _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã cấu hình bong bóng phía dưới thử nghiệm an toàn 100%. Sẵn sàng commit.");
        }
        else
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Đã cấu hình vĩnh viễn bong bóng lưới trục chỉ hiện phía dưới trên các mặt đứng trong Revit 2026!");
        }
    }

    public static string BuildRevitScript() => """
        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToList();

        var elevationViews = new FilteredElementCollector(doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(v => !v.IsTemplate && (v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section))
            .ToList();

        int shownBottom = 0;
        int hiddenTop = 0;

        foreach (var grid in grids)
        {
            foreach (var v in elevationViews)
            {
                try
                {
                    DatumEnds bottomEnd = DatumEnds.End0;
                    DatumEnds topEnd = DatumEnds.End1;

                    var curves = grid.GetCurvesInView(DatumExtentType.ViewSpecific, v);
                    if (curves != null && curves.Count > 0)
                    {
                        var curve = curves[0];
                        if (curve.GetEndPoint(0).Z > curve.GetEndPoint(1).Z)
                        {
                            bottomEnd = DatumEnds.End1;
                            topEnd = DatumEnds.End0;
                        }
                    }

                    grid.ShowBubbleInView(bottomEnd, v);
                    shownBottom++;

                    grid.HideBubbleInView(topEnd, v);
                    hiddenTop++;
                }
                catch (Exception)
                {
                    // View may not intersect grid or support bubbles for this datum
                }
            }
        }

        doc.Regenerate();

        return new
        {
            gridCount = grids.Count,
            viewsProcessed = elevationViews.Count,
            shownBottom,
            hiddenTop
        };
    """;
}
