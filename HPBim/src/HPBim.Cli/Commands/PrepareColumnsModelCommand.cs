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

/// <summary>
///     Pure C# command to prepare the Column Model from extracted CAD data.
/// </summary>
public sealed class PrepareColumnsModelCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public PrepareColumnsModelCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string inputPath = args.GetOption("input", Path.Combine("scratch", "cad_columns_extracted.json"));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "columns_story1_model.json"));

        _formatter.WriteBanner("Prepare Columns Model", "Chuẩn hóa dữ liệu 72 Cột Tầng 1 sang mô hình BIM C#");

        if (!File.Exists(inputPath))
        {
            _formatter.WriteError($"Không tìm thấy file trích xuất: {inputPath}");
            return 1;
        }

        using var inDoc = JsonDocument.Parse(await File.ReadAllTextAsync(inputPath, ct).ConfigureAwait(false));
        if (!inDoc.RootElement.TryGetProperty("definitionPolylines", out var polys))
        {
            _formatter.WriteError("Không tìm thấy property 'definitionPolylines' trong JSON.");
            return 1;
        }

        // CAD to Revit offsets relative to Grid 1-A
        const double dX = 43976.4;
        const double dY = 379.9;

        var columns = new List<object>();
        var typeGroups = new Dictionary<string, (double w, double h, int count)>();

        int index = 0;
        foreach (var p in polys.EnumerateArray())
        {
            index++;
            double w = p.GetProperty("width").GetDouble();
            double h = p.GetProperty("height").GetDouble();
            double cx = p.GetProperty("centerX").GetDouble();
            double cy = p.GetProperty("centerY").GetDouble();

            double revitX = Math.Round(cx + dX, 1);
            double revitY = Math.Round(cy + dY, 1);
            string typeName = $"C_{w:0}x{h:0}";

            if (typeGroups.TryGetValue(typeName, out var group))
                typeGroups[typeName] = (w, h, group.count + 1);
            else
                typeGroups[typeName] = (w, h, 1);

            columns.Add(new
            {
                index = index,
                typeName = typeName,
                widthMm = w,
                heightMm = h,
                xMm = revitX,
                yMm = revitY,
                coCot = new
                {
                    mark = $"CC_{index:D2}",
                    baseLevel = "Đỉnh Đài Móng",
                    topLevel = "Tầng 1 (±0.000)",
                    baseOffsetMm = 0.0,
                    topOffsetMm = 0.0,
                    comments = "Cổ Cột Móng"
                },
                cotTang1 = new
                {
                    mark = $"C1_{index:D2}",
                    baseLevel = "Tầng 1 (±0.000)",
                    topLevel = "Tầng 2",
                    baseOffsetMm = 0.0,
                    topOffsetMm = 0.0,
                    comments = "Cột Tầng 1"
                }
            });
        }

        var typesNeeded = typeGroups.Select(kv => new
        {
            name = kv.Key,
            widthMm = kv.Value.w,
            heightMm = kv.Value.h,
            count = kv.Value.count
        }).OrderBy(t => t.widthMm).ThenBy(t => t.heightMm).ToList();

        var model = new
        {
            totalColumns = columns.Count,
            totalSegments = columns.Count * 2,
            typesNeeded = typesNeeded,
            columns = columns
        };

        string json = JsonSerializer.Serialize(model, JsonOptions);
        string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);

        _formatter.WriteSuccess($"Đã chuẩn hóa {columns.Count} vị trí cột ({columns.Count * 2} phân đoạn) vào: {outputPath}");
        foreach (var t in typesNeeded)
        {
            _formatter.WriteInfo($"  - Type {t.name}: {t.count} cột");
        }

        return 0;
    }
}
