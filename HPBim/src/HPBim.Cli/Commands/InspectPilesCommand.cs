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
///     Queries AutoCAD block 'Rec-TT' and Revit structural foundation families
///     to diagnose pile type discrepancies.
/// </summary>
public sealed class InspectPilesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectPilesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        _formatter.WriteBanner("Inspect Piles (CAD vs Revit)", "Đối soát chi tiết cọc giữa AutoCAD 2026 và Revit 2026");

        var cadResult = await InspectCadBlockAsync(ct).ConfigureAwait(false);
        var revitResult = await InspectRevitPilesAsync(ct).ConfigureAwait(false);

        var combined = new
        {
            cad = cadResult,
            revit = revitResult
        };

        string outPath = Path.Combine("scratch", "piles_cad_vs_revit_inspection.json");
        File.WriteAllText(outPath, JsonSerializer.Serialize(combined, JsonOptions));
        _formatter.WriteSuccess($"Đã lưu kết quả đối soát vào: {outPath}");

        return 0;
    }

    private async Task<object?> InspectCadBlockAsync(CancellationToken ct)
    {
        string pipe = PipeNaming.For(PipeNaming.AutocadHost, 2026);
        _formatter.WriteInfo($"Đang kết nối AutoCAD qua pipe: {pipe}...");
        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipe,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 30
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            string script = """
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has("Rec-TT"))
                {
                    return new { found = false, message = "Block Rec-TT khong ton tai trong AutoCAD" };
                }

                var btr = (BlockTableRecord)tr.GetObject(bt["Rec-TT"], OpenMode.ForRead);
                var ents = new List<object>();
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (ObjectId id in btr)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string entType = ent.GetType().Name;
                    var ext = ent.GeometricExtents;
                    minX = Math.Min(minX, ext.MinPoint.X);
                    minY = Math.Min(minY, ext.MinPoint.Y);
                    maxX = Math.Max(maxX, ext.MaxPoint.X);
                    maxY = Math.Max(maxY, ext.MaxPoint.Y);

                    ents.Add(new {
                        type = entType,
                        layer = ent.Layer,
                        color = ent.ColorIndex,
                        bounds = new {
                            minX = Math.Round(ext.MinPoint.X, 1),
                            minY = Math.Round(ext.MinPoint.Y, 1),
                            maxX = Math.Round(ext.MaxPoint.X, 1),
                            maxY = Math.Round(ext.MaxPoint.Y, 1)
                        }
                    });
                }

                return new {
                    found = true,
                    blockName = "Rec-TT",
                    entityCount = ents.Count,
                    overallSize = new {
                        width = Math.Round(maxX - minX, 1),
                        height = Math.Round(maxY - minY, 1),
                        minX = Math.Round(minX, 1),
                        minY = Math.Round(minY, 1),
                        maxX = Math.Round(maxX, 1),
                        maxY = Math.Round(maxY, 1)
                    },
                    entities = ents
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 30, "Inspect Block Rec-TT");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi AutoCAD: {res.Message}");
                return null;
            }

            _formatter.WriteSuccess("Đã đọc thành công Block 'Rec-TT' từ AutoCAD 2026!");
            return res.Value.Value;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối AutoCAD: {ex.Message}");
            return null;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<object?> InspectRevitPilesAsync(CancellationToken ct)
    {
        string pipe = PipeNaming.For(PipeNaming.RevitHost, 2026);
        _formatter.WriteInfo($"Đang kết nối Revit qua pipe: {pipe}...");
        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipe,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 30
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            string script = """
                var symbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .Cast<FamilySymbol>()
                    .Select(s => new {
                        id = s.Id.ToString(),
                        familyName = s.FamilyName,
                        symbolName = s.Name
                    })
                    .OrderBy(s => s.familyName)
                    .ThenBy(s => s.symbolName)
                    .ToList();

                var placedFoundations = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .Cast<FamilyInstance>()
                    .Select(fi => {
                        var bb = fi.get_BoundingBox(null);
                        var loc = (LocationPoint)fi.Location;
                        double offsetFt = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                        var pL = fi.LookupParameter("L") ?? fi.LookupParameter("Length") ?? fi.LookupParameter("Chiều dài");
                        double lenMm = pL != null ? pL.AsDouble() * 304.8 : 0.0;

                        return new {
                            id = fi.Id.ToString(),
                            name = fi.Name,
                            symbolName = fi.Symbol.Name,
                            familyName = fi.Symbol.FamilyName,
                            level = doc.GetElement(fi.LevelId)?.Name ?? "",
                            locZMm = Math.Round(loc != null ? loc.Point.Z * 304.8 : 0.0, 1),
                            offsetMm = Math.Round(offsetFt * 304.8, 1),
                            lengthMm = Math.Round(lenMm, 1),
                            minZMm = Math.Round(bb != null ? bb.Min.Z * 304.8 : 0.0, 1),
                            maxZMm = Math.Round(bb != null ? bb.Max.Z * 304.8 : 0.0, 1)
                        };
                    })
                    .ToList();

                return new {
                    totalSymbols = symbols.Count,
                    symbols = symbols,
                    totalPlaced = placedFoundations.Count,
                    samplePiles = placedFoundations.Take(5).ToList(),
                    placedSummary = placedFoundations
                        .GroupBy(p => $"{p.familyName}: {p.symbolName}")
                        .Select(g => new { type = g.Key, count = g.Count() })
                        .ToList()
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 30, "Inspect Revit Foundations");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                return null;
            }

            _formatter.WriteSuccess("Đã đọc thành công Foundation Symbols & Instances từ Revit 2026!");
            return res.Value.Value;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối Revit: {ex.Message}");
            return null;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
