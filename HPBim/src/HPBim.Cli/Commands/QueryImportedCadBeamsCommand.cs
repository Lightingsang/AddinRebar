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
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Pure C# command to extract beam geometry directly from the linked CAD ImportInstance in Revit 2026.
/// </summary>
public sealed class QueryImportedCadBeamsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public QueryImportedCadBeamsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outPath = args.GetOption("output", Path.Combine("scratch", "revit_imported_cad_beams.json"));

        _formatter.WriteBanner("Query Imported CAD Beams", "Trích xuất thực thể dầm trực tiếp từ CAD Link Instance trong Revit 2026");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 10000,
            DefaultExecuteTimeoutSeconds = 120
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Revit Named Pipe: {pipeName}");

            string script = BuildQueryScript();
            var req = new ExecuteRequest(script, TransactionModes.None, false, 120, "Query Imported CAD Geometry");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);

            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                return 1;
            }

            var val = res.Value.Value;
            File.WriteAllText(outPath, JsonSerializer.Serialize(val, JsonOptions));
            _formatter.WriteSuccess($"Đã trích xuất và lưu dữ liệu: {outPath}");

            RenderSummary(val);
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi thực thi: {ex.Message}");
            return 99;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string BuildQueryScript() => """
        var inst = new FilteredElementCollector(doc)
            .OfClass(typeof(ImportInstance))
            .Cast<ImportInstance>()
            .FirstOrDefault(i => {
                var t = doc.GetElement(i.GetTypeId());
                return t != null && t.Name.IndexOf("MatBang_DaKieng", StringComparison.OrdinalIgnoreCase) >= 0;
            })
            ?? new FilteredElementCollector(doc)
                .OfClass(typeof(ImportInstance))
                .Cast<ImportInstance>()
                .FirstOrDefault(i => i.Id.Value == 9922744)
            ?? new FilteredElementCollector(doc)
                .OfClass(typeof(ImportInstance))
                .Cast<ImportInstance>()
                .LastOrDefault();

        if (inst == null)
        {
            return new { success = false, error = "Không tìm thấy ImportInstance nào trong Document." };
        }

        var opt = new Options { DetailLevel = ViewDetailLevel.Fine };
        var geomElem = inst.get_Geometry(opt);
        if (geomElem == null)
        {
            return new { success = false, error = "Không đọc được GeometryElement từ ImportInstance." };
        }

        var linesByLayer = new Dictionary<string, int>();
        var beamLines = new List<double[]>(); // [type: 0=horiz, 1=vert, 2=other, coord1, coord2, len]

        Action<XYZ, XYZ, string> addSegment = (p1, p2, layer) => {
            double x1 = Math.Round(p1.X * 304.8, 1);
            double y1 = Math.Round(p1.Y * 304.8, 1);
            double x2 = Math.Round(p2.X * 304.8, 1);
            double y2 = Math.Round(p2.Y * 304.8, 1);
            double len = Math.Round(p1.DistanceTo(p2) * 304.8, 1);

            if (len < 100.0) return;

            if (Math.Abs(y1 - y2) <= 5.0)
            {
                double ym = Math.Round((y1 + y2) / 2.0, 1);
                beamLines.Add(new[] { 0.0, ym, Math.Min(x1, x2), Math.Max(x1, x2), len });
            }
            else if (Math.Abs(x1 - x2) <= 5.0)
            {
                double xm = Math.Round((x1 + x2) / 2.0, 1);
                beamLines.Add(new[] { 1.0, xm, Math.Min(y1, y2), Math.Max(y1, y2), len });
            }
            else
            {
                beamLines.Add(new[] { 2.0, x1, y1, x2, y2, len });
            }
        };

        Action<Line> processLine = l => {
            var style = doc.GetElement(l.GraphicsStyleId) as GraphicsStyle;
            string layer = style?.GraphicsStyleCategory?.Name ?? "Unknown";
            linesByLayer[layer] = linesByLayer.TryGetValue(layer, out int cnt) ? cnt + 1 : 1;

            if (layer.IndexOf("DK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("Bao", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("DAM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("BAN BT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                addSegment(l.GetEndPoint(0), l.GetEndPoint(1), layer);
            }
        };

        Action<PolyLine> processPoly = pl => {
            var style = doc.GetElement(pl.GraphicsStyleId) as GraphicsStyle;
            string layer = style?.GraphicsStyleCategory?.Name ?? "Unknown";
            linesByLayer[layer] = linesByLayer.TryGetValue(layer, out int cnt) ? cnt + 1 : 1;

            if (layer.IndexOf("DK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("Bao", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("DAM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layer.IndexOf("BAN BT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var coords = pl.GetCoordinates();
                for (int i = 0; i < coords.Count - 1; i++)
                {
                    addSegment(coords[i], coords[i + 1], layer);
                }
            }
        };

        var childTypes = new Dictionary<string, int>();
        foreach (GeometryObject obj in geomElem)
        {
            if (obj is GeometryInstance gi)
            {
                var symGeom = gi.GetInstanceGeometry();
                if (symGeom == null) continue;

                foreach (GeometryObject child in symGeom)
                {
                    string ct = child.GetType().Name;
                    childTypes[ct] = childTypes.TryGetValue(ct, out int c) ? c + 1 : 1;

                    if (child is Line l) processLine(l);
                    else if (child is PolyLine pl) processPoly(pl);
                }
            }
        }

        return new {
            success = true,
            importInstanceId = inst.Id.Value,
            childTypes = childTypes,
            layerCounts = linesByLayer,
            totalBeamSegments = beamLines.Count,
            segments = beamLines
        };
    """;

    private void RenderSummary(JsonElement val)
    {
        bool success = val.TryGetProperty("success", out var sElem) && sElem.GetBoolean();
        if (!success)
        {
            string err = val.TryGetProperty("error", out var eElem) ? eElem.GetString() ?? "Lỗi" : "Lỗi";
            _formatter.WriteError($"Trích xuất thất bại: {err}");
            return;
        }

        long id = val.TryGetProperty("importInstanceId", out var iElem) ? iElem.GetInt64() : 0;
        int totalSegs = val.TryGetProperty("totalBeamSegments", out var tElem) ? tElem.GetInt32() : 0;
        var layers = val.GetProperty("layerCounts");

        string[][] rows =
        [
            ["ImportInstance ElementId", id.ToString()],
            ["Tổng số đoạn nét dầm trích xuất", $"{totalSegs} đoạn nét"],
            ["Các layer CAD nhận diện", string.Join(", ", layers.EnumerateObject().Select(p => $"{p.Name}: {p.Value.GetInt32()}"))]
        ];

        _formatter.RenderTable(["Thông số Trích xuất CAD từ Revit", "Giá trị thực tế"], rows);
    }
}
