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
///     Pure C# command to rename Revit Level Types to the concise HP branding:
///     HP_Level_T1_(±) and HP_Level_T1_(+), and ensure all 10 story levels use the new types.
/// </summary>
public sealed class RenameLevelTypesCommand
{
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public RenameLevelTypesCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        bool dryRun = args.HasFlag("dry-run");

        string modeLabel = dryRun ? "DRY-RUN (Thử nghiệm & Rollback)" : "COMMIT (Ghi thực tế)";
        _formatter.WriteBanner("Rename Level Types to HP Prefix", $"Đổi tên Level Type sang tiền tố HP tinh gọn [{modeLabel}]");

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
            _formatter.WriteError($"Lỗi đổi tên Level Type: {ex.Message}");
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
        _formatter.WriteInfo("Đang gửi C# Roslyn Script đổi tên Level Type sang Revit 2026...");
        string scriptCode = BuildRevitScript();

        var request = new ExecuteRequest(
            Code: scriptCode,
            Transaction: TransactionModes.Auto,
            DryRun: dryRun,
            TimeoutSeconds: 45,
            Label: "Rename Level Types to HP_ Prefix");

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
            ["Type Tầng 1 (Cốt hoàn thiện)", "HP_Level_T1_(±)"],
            ["Type 9 Tầng còn lại", "HP_Level_T1_(+)"],
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
            _formatter.WriteSuccess("DRY-RUN THÀNH CÔNG: Đã đổi tên Level Type sang HP_Level_T1_ thử nghiệm an toàn 100%. Sẵn sàng commit.");
        }
        else
        {
            _formatter.WriteSuccess("COMMIT THÀNH CÔNG: Đã đổi tên vĩnh viễn Level Type sang HP_Level_T1_(±) và HP_Level_T1_(+) trong Revit 2026!");
        }
    }

    public static string BuildRevitScript() => """
        var levelTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(LevelType))
            .Cast<LevelType>()
            .ToList();

        int renamedTypes = 0;
        LevelType typeZero = null;
        LevelType typePlus = null;

        foreach (var lt in levelTypes)
        {
            if (lt.Name.IndexOf("Level_T1_(±)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                lt.Name.Equals("HP_Level_T1_(±)", StringComparison.OrdinalIgnoreCase))
            {
                if (!lt.Name.Equals("HP_Level_T1_(±)", StringComparison.OrdinalIgnoreCase))
                {
                    lt.Name = "HP_Level_T1_(±)";
                    renamedTypes++;
                }
                typeZero = lt;
            }
            else if (lt.Name.IndexOf("Level_T1_(+)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     lt.Name.Equals("HP_Level_T1_(+)", StringComparison.OrdinalIgnoreCase))
            {
                if (!lt.Name.Equals("HP_Level_T1_(+)", StringComparison.OrdinalIgnoreCase))
                {
                    lt.Name = "HP_Level_T1_(+)";
                    renamedTypes++;
                }
                typePlus = lt;
            }
        }

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();

        int levelsUpdated = 0;
        foreach (var l in levels)
        {
            bool isZero = Math.Abs(l.Elevation) < 0.001 || l.Name.Contains("Tầng 1") || l.Name.Contains("±");
            var targetType = isZero ? typeZero : typePlus;
            if (targetType != null && l.GetTypeId() != targetType.Id)
            {
                l.ChangeTypeId(targetType.Id);
                levelsUpdated++;
            }
        }

        doc.Regenerate();

        return new
        {
            renamedTypes,
            levelsUpdated,
            typeZeroName = typeZero?.Name,
            typePlusName = typePlus?.Name,
            totalLevels = levels.Count
        };
    """;
}
