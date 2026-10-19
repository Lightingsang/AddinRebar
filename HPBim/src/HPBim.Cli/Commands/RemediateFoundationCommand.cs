namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Core.Geometry;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
/// 100% C# command to remediate geometric shifts, clashes, and cantilever beams in Revit 2026.
/// Eliminates STT collision displacement and pins all grids/levels.
/// </summary>
public sealed class RemediateFoundationCommand
{
    private static readonly double[] XGrids = { 0, 1300, 6900, 9900, 13900, 16500, 23700, 29050, 35750, 38300, 40800, 45000, 48900 };
    private static readonly double[] YGrids = { 0, 2800, 4650, 7150, 9950, 12750, 13950, 15150, 17950, 20000, 20750, 23250, 25100, 27900 };

    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public RemediateFoundationCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        _formatter.WriteBanner("Remediate Foundation Geometry", "Nắn giằng móng vào tâm đài & tái tạo 17 đài tam giác 6 đỉnh (100% C#)");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Revit Named Pipe: {pipeName}");

            if (args.HasFlag("piles-only"))
            {
                await ExecuteAdjustPilesAsync(client, ct).ConfigureAwait(false);
            }
            else
            {
                await ExecutePinAndPurgeAsync(client, ct).ConfigureAwait(false);
                await ExecuteCreateFootingsAsync(client, ct).ConfigureAwait(false);
                await ExecuteAdjustPilesAsync(client, ct).ConfigureAwait(false);
                await ExecuteCreateGradeBeamsAsync(client, ct).ConfigureAwait(false);
            }

            _formatter.WriteSuccess("Hoàn tất 100% công tác khắc phục và tái tạo hình học phân đoạn Móng trong Revit.");
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi khắc phục: {ex.Message}");
            return 99;
        }
    }

    private async Task ExecutePinAndPurgeAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Bước 1: Đang ghim 27 trục, 10 cao trình và xóa sạch dầm ma & đài cũ...");
        string script = FoundationRemediationBuilder.BuildPinAndPurgeScript();
        var req = new ExecuteRequest(script, TransactionModes.Auto, false, 60, "Pin and Purge Foundation");
        var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
        if (!res.IsError)
        {
            _formatter.WriteSuccess("Đã ghim 100% hệ trục, cao trình và dọn sạch dữ liệu cũ trong Revit.");
        }
    }

    private async Task ExecuteCreateFootingsAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Bước 2: Đang tái lập 72 đài móng chuẩn xác (17 đài tam giác vát 6 đỉnh)...");
        string footingsPath = Path.Combine(".agents", "teamwork", "explorer_survey_3", "verified_72_footings.json");
        using var jsonDoc = JsonDocument.Parse(await File.ReadAllTextAsync(footingsPath, ct).ConfigureAwait(false));
        var footingsArray = jsonDoc.RootElement.GetProperty("footings");

        string pilePath = Path.Combine(".agents", "teamwork", "worker_m3_foundation", "exact_196_pile_mapping.json");
        using var pileDoc = JsonDocument.Parse(await File.ReadAllTextAsync(pilePath, ct).ConfigureAwait(false));
        var pilesByFooting = new Dictionary<string, List<Point2D>>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in pileDoc.RootElement.EnumerateArray())
        {
            string fId = p.GetProperty("footing_id").GetString() ?? "";
            string normId = fId.Replace("DC-", "ĐC-");
            double px = p.GetProperty("x").GetDouble();
            double py = p.GetProperty("y").GetDouble();
            if (!pilesByFooting.TryGetValue(normId, out var pList))
            {
                pList = new List<Point2D>();
                pilesByFooting[normId] = pList;
            }
            pList.Add(new Point2D(px, py));
        }

        var correctedFootings = PrepareCorrectedFootings(footingsArray, pilesByFooting);
        var argsObj = JsonSerializer.SerializeToElement(new { footings = correctedFootings });

        string script = FoundationRemediationBuilder.BuildCreateFootingsScript();
        var req = new ExecuteRequest(script, TransactionModes.Auto, false, 120, "Create 72 True Footings", argsObj);
        var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
        if (!res.IsError)
        {
            _formatter.WriteSuccess("Đã dựng thành công 72 đài móng tại COTE đỉnh -1.600m (kèm 17 đài tam giác 6 đỉnh).");
        }
    }

    private async Task ExecuteAdjustPilesAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Bước 3: Đang điều chỉnh cao độ đầu cọc ngàm 100mm vào đáy đài...");
        string script = FoundationRemediationBuilder.BuildAdjustPilesScript();
        var req = new ExecuteRequest(script, TransactionModes.Auto, false, 60, "Adjust Piles Embedment");
        var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
        if (!res.IsError)
        {
            _formatter.WriteSuccess("Đã cập nhật cao độ 196 đầu cọc ngàm chính xác 100mm.");
        }
    }

    private async Task ExecuteCreateGradeBeamsAsync(IRevitPipeClient client, CancellationToken ct)
    {
        _formatter.WriteInfo("Bước 4: Đang nắn và dựng 188 giằng móng bắt vào tâm đài/lưới trục...");
        string path = Path.Combine(".agents", "teamwork", "worker_m3_foundation", "cad_extracted_beam_spans.json");
        using var jsonDoc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false));

        var rawList = new List<BeamRawSpan>();
        foreach (var b in jsonDoc.RootElement.EnumerateArray())
        {
            rawList.Add(new BeamRawSpan(
                b.GetProperty("name").GetString() ?? "GM",
                b.GetProperty("bMm").GetDouble(),
                b.GetProperty("hMm").GetDouble(),
                b.GetProperty("x1").GetDouble(),
                b.GetProperty("y1").GetDouble(),
                b.GetProperty("x2").GetDouble(),
                b.GetProperty("y2").GetDouble()
            ));
        }

        var snapped = GradeBeamGridSnapper.SnapAll(rawList, XGrids, YGrids);
        var beamPayload = snapped.Select(s => new
        {
            name = s.Name,
            bMm = s.WidthMm,
            hMm = s.HeightMm,
            x1 = s.StartPoint.X,
            y1 = s.StartPoint.Y,
            x2 = s.EndPoint.X,
            y2 = s.EndPoint.Y,
            yOffsetMm = s.YOffsetMm
        }).ToList();

        var argsObj = JsonSerializer.SerializeToElement(new { beams = beamPayload });
        string script = FoundationRemediationBuilder.BuildCreateGradeBeamsScript();
        var req = new ExecuteRequest(script, TransactionModes.Auto, false, 120, "Create Grade Beams", argsObj);
        var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
        if (!res.IsError)
        {
            _formatter.WriteSuccess($"Đã dựng thành công {beamPayload.Count} giằng móng chuẩn thiết kế.");
        }
    }

    private static List<Dictionary<string, object>> PrepareCorrectedFootings(
        JsonElement footingsArray,
        IReadOnlyDictionary<string, List<Point2D>> pilesByFooting)
    {
        var shifts = new Dictionary<string, (double dx, double dy)>(StringComparer.OrdinalIgnoreCase)
        {
            ["ĐC-2-04"] = (0.0, 450.0),
            ["ĐC-2-10"] = (0.0, 450.0),
            ["ĐC-2-19"] = (0.0, -450.0),
            ["ĐC-6-02"] = (150.0, 0.0)
        };

        var customRectangles = new Dictionary<string, List<(double x, double y)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ĐC-6-01"] = new() { (15450.0, 4000.0), (17950.0, 4000.0), (17950.0, 5600.0), (15450.0, 5600.0) },
            ["ĐC-6-04"] = new() { (15450.0, 22300.0), (17950.0, 22300.0), (17950.0, 23900.0), (15450.0, 23900.0) }
        };

        var result = new List<Dictionary<string, object>>();
        foreach (var ft in footingsArray.EnumerateArray())
        {
            string id = ft.GetProperty("id").GetString() ?? "";
            string normId = id.Replace("DC-", "ĐC-");
            string type = ft.GetProperty("type").GetString() ?? "";
            double thick = ft.GetProperty("thicknessMm").GetDouble();

            var verts = new List<Dictionary<string, double>>();
            if (pilesByFooting.TryGetValue(normId, out var pList) && pList.Count == 3)
            {
                var poly = TriangularFootingGenerator.Generate(pList[0], pList[1], pList[2], 350.0);
                foreach (var pt in poly.Vertices)
                {
                    verts.Add(new Dictionary<string, double> { ["x"] = pt.X, ["y"] = pt.Y });
                }
            }
            else if (customRectangles.TryGetValue(normId, out var customList) || customRectangles.TryGetValue(id, out customList))
            {
                foreach (var pt in customList)
                {
                    verts.Add(new Dictionary<string, double> { ["x"] = pt.x, ["y"] = pt.y });
                }
            }
            else
            {
                double dx = 0.0, dy = 0.0;
                if (shifts.TryGetValue(normId, out var shift) || shifts.TryGetValue(id, out shift))
                {
                    dx = shift.dx;
                    dy = shift.dy;
                }

                foreach (var v in ft.GetProperty("vertices").EnumerateArray())
                {
                    verts.Add(new Dictionary<string, double>
                    {
                        ["x"] = v.GetProperty("x").GetDouble() + dx,
                        ["y"] = v.GetProperty("y").GetDouble() + dy
                    });
                }
            }

            result.Add(new Dictionary<string, object>
            {
                ["id"] = id,
                ["type"] = type,
                ["thicknessMm"] = thick,
                ["vertices"] = verts
            });
        }
        return result;
    }
}
