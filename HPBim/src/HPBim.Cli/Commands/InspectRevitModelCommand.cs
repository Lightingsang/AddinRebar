namespace HPBim.Cli.Commands;

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to deeply inspect Grids, Levels, and Views in Revit 2026
///     to detect all user interventions, adjustments, elbows, extents, and bubble states.
/// </summary>
public sealed class InspectRevitModelCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IRevitPipeClient? _injectedClient;

    public InspectRevitModelCommand(ConsoleFormatter? formatter = null, IRevitPipeClient? injectedClient = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _injectedClient = injectedClient;
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "revit_detailed_inspection.json"));

        _formatter.WriteBanner("Deep Inspect Revit Model", $"Quét toàn diện can thiệp của người dùng trên Lưới trục & Cao độ");

        var client = _injectedClient ?? new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");

            var result = await DispatchScriptAsync(client, ct).ConfigureAwait(false);
            if (result.IsError || !result.Value.HasValue)
            {
                string diags = result.Diagnostics != null && result.Diagnostics.Count > 0
                    ? string.Join("; ", result.Diagnostics.Select(d => $"[L{d.Line}:C{d.Column}] {d.Id}: {d.Message}"))
                    : "Không có diagnostics";
                _formatter.WriteError($"Lỗi truy vấn: {result.Message} -> {diags}");
                return 1;
            }

            string json = JsonSerializer.Serialize(result.Value.Value, JsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã lưu phân tích chi tiết tại: {outputPath}");

            RenderDetailedSummary(result.Value.Value);
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
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

    private async Task<ExecuteResult> DispatchScriptAsync(IRevitPipeClient client, CancellationToken ct)
    {
        string script = """
            var grids = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .OrderBy(g => g.Name)
                .ToList();

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section))
                .OrderBy(v => v.Name)
                .ToList();

            var gridList = grids.Select(g => {
                var c = g.Curve;
                var p0 = c.GetEndPoint(0);
                var p1 = c.GetEndPoint(1);
                var gt = doc.GetElement(g.GetTypeId()) as ElementType;

                var viewSettings = views.Select(v => {
                    bool b0 = false;
                    bool b1 = false;
                    string extentType = "Model";
                    try {
                        b0 = g.IsBubbleVisibleInView(DatumEnds.End0, v);
                        b1 = g.IsBubbleVisibleInView(DatumEnds.End1, v);
                        extentType = g.GetDatumExtentTypeInView(DatumEnds.End0, v).ToString();
                    } catch (Exception) { /* view does not intersect grid */ }
                    return new { viewName = v.Name, viewType = v.ViewType.ToString(), b0, b1, extentType };
                }).ToList();

                return new {
                    id = g.Id.ToString(),
                    name = g.Name,
                    typeName = gt?.Name ?? "",
                    pinned = g.Pinned,
                    start = new { x = Math.Round(p0.X * 304.8, 1), y = Math.Round(p0.Y * 304.8, 1), z = Math.Round(p0.Z * 304.8, 1) },
                    end = new { x = Math.Round(p1.X * 304.8, 1), y = Math.Round(p1.Y * 304.8, 1), z = Math.Round(p1.Z * 304.8, 1) },
                    views = viewSettings
                };
            }).ToList();

            var levelList = levels.Select(l => {
                var lt = doc.GetElement(l.GetTypeId()) as ElementType;
                var viewSettings = views.Where(v => v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section).Select(v => {
                    bool b0 = false;
                    bool b1 = false;
                    string extentType = "Model";
                    try {
                        b0 = l.IsBubbleVisibleInView(DatumEnds.End0, v);
                        b1 = l.IsBubbleVisibleInView(DatumEnds.End1, v);
                        extentType = l.GetDatumExtentTypeInView(DatumEnds.End0, v).ToString();
                    } catch (Exception) { /* view does not intersect level */ }
                    return new { viewName = v.Name, b0, b1, extentType };
                }).ToList();

                return new {
                    id = l.Id.ToString(),
                    name = l.Name,
                    typeName = lt?.Name ?? "",
                    pinned = l.Pinned,
                    elevationMm = Math.Round(l.Elevation * 304.8, 1),
                    views = viewSettings
                };
            }).ToList();

            var allLevelTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(LevelType))
                .Cast<LevelType>()
                .Select(lt => {
                    var pSymbol = lt.get_Parameter(BuiltInParameter.LEVEL_HEAD_TAG);
                    var symId = pSymbol?.AsElementId();
                    var symbolElem = (symId != null && symId != ElementId.InvalidElementId) ? doc.GetElement(symId) as FamilySymbol : null;
                    return new {
                        id = lt.Id.ToString(),
                        name = lt.Name,
                        symbolName = symbolElem?.Name ?? "",
                        familyName = symbolElem?.FamilyName ?? ""
                    };
                })
                .OrderBy(lt => lt.name)
                .ToList();

            var allGridTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(GridType))
                .Cast<GridType>()
                .Select(gt => {
                    var pSymbol = gt.get_Parameter(BuiltInParameter.GRID_HEAD_TAG);
                    var symId = pSymbol?.AsElementId();
                    var symbolElem = (symId != null && symId != ElementId.InvalidElementId) ? doc.GetElement(symId) as FamilySymbol : null;
                    return new {
                        id = gt.Id.ToString(),
                        name = gt.Name,
                        symbolName = symbolElem?.Name ?? "",
                        familyName = symbolElem?.FamilyName ?? ""
                    };
                })
                .OrderBy(gt => gt.name)
                .ToList();

            return new {
                totalGrids = gridList.Count,
                totalLevels = levelList.Count,
                totalViews = views.Count,
                viewNames = views.Select(v => $"{v.Name} ({v.ViewType})").ToList(),
                allLevelTypes,
                allGridTypes,
                grids = gridList,
                levels = levelList
            };
        """;

        var request = new ExecuteRequest(
            Code: script,
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 45,
            Label: "Deep Inspect Revit Model");

        return await client.ExecuteAsync(request, ct).ConfigureAwait(false);
    }

    private void RenderDetailedSummary(JsonElement root)
    {
        _formatter.WriteBanner("Detailed Analysis of User Interventions", "Phân tích can thiệp của người dùng trên Revit Model");

        if (root.TryGetProperty("allLevelTypes", out var allTypesElem))
        {
            var typeRows = new List<string[]>();
            foreach (var t in allTypesElem.EnumerateArray())
            {
                string id = t.GetProperty("id").GetString() ?? "";
                string name = t.GetProperty("name").GetString() ?? "";
                string sym = t.GetProperty("symbolName").GetString() ?? "";
                string fam = t.GetProperty("familyName").GetString() ?? "";
                typeRows.Add([id, name, fam, sym]);
            }
            _formatter.WriteInfo("DANH SÁCH LEVEL TYPES & SYMBOL ĐANG DÙNG:");
            _formatter.RenderTable(["Id", "Tên Level Type", "Family Ký hiệu", "Tên Symbol Ký hiệu"], typeRows.ToArray());
        }

        if (root.TryGetProperty("allGridTypes", out var allGridTypesElem))
        {
            var gridTypeRows = new List<string[]>();
            foreach (var t in allGridTypesElem.EnumerateArray())
            {
                string id = t.GetProperty("id").GetString() ?? "";
                string name = t.GetProperty("name").GetString() ?? "";
                string sym = t.GetProperty("symbolName").GetString() ?? "";
                string fam = t.GetProperty("familyName").GetString() ?? "";
                gridTypeRows.Add([id, name, fam, sym]);
            }
            _formatter.WriteInfo("DANH SÁCH GRID TYPES & SYMBOL ĐANG DÙNG:");
            _formatter.RenderTable(["Id", "Tên Grid Type", "Family Ký hiệu", "Tên Symbol Ký hiệu"], gridTypeRows.ToArray());
        }

        if (root.TryGetProperty("levels", out var levelsElem))
        {
            var rows = new List<string[]>();
            foreach (var l in levelsElem.EnumerateArray())
            {
                string name = l.GetProperty("name").GetString() ?? "";
                double elev = l.GetProperty("elevationMm").GetDouble();
                string typeName = l.GetProperty("typeName").GetString() ?? "";
                bool pinned = l.GetProperty("pinned").GetBoolean();
                rows.Add([name, $"{elev:F0} mm", typeName, pinned ? "PINNED" : "Unpinned"]);
            }
            _formatter.RenderTable(["Tên Level", "Cao độ (Z)", "Loại Family Type", "Trạng thái Ghim"], rows.ToArray());
        }

        if (root.TryGetProperty("grids", out var gridsElem))
        {
            var rowsX = new List<string[]>();
            var rowsY = new List<string[]>();
            foreach (var g in gridsElem.EnumerateArray())
            {
                string name = g.GetProperty("name").GetString() ?? "";
                var start = g.GetProperty("start");
                var end = g.GetProperty("end");
                double x0 = start.GetProperty("x").GetDouble();
                double y0 = start.GetProperty("y").GetDouble();
                double x1 = end.GetProperty("x").GetDouble();
                double y1 = end.GetProperty("y").GetDouble();
                bool pinned = g.GetProperty("pinned").GetBoolean();

                if (Math.Abs(x0 - x1) < 1.0)
                {
                    rowsX.Add([$"Trục {name}", $"X = {x0:F0} mm", $"Y: {y0:F0} -> {y1:F0} mm", pinned ? "PINNED" : "Unpinned"]);
                }
                else
                {
                    rowsY.Add([$"Trục {name}", $"Y = {y0:F0} mm", $"X: {x0:F0} -> {x1:F0} mm", pinned ? "PINNED" : "Unpinned"]);
                }
            }

            _formatter.WriteInfo("HỆ TRỤC DỌC (PHƯƠNG X):");
            _formatter.RenderTable(["Tên Trục", "Tọa độ X", "Phạm vi Y", "Ghim"], rowsX.ToArray());

            _formatter.WriteInfo("HỆ TRỤC NGANG (PHƯƠNG Y):");
            _formatter.RenderTable(["Tên Trục", "Tọa độ Y", "Phạm vi X", "Ghim"], rowsY.ToArray());
        }
    }
}
