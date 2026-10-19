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
///     Queries available FloorTypes and Foundation Slab types in Revit 2026.
/// </summary>
public sealed class InspectFoundationSlabsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectFoundationSlabsCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        _formatter.WriteBanner("Inspect Revit Floor & Foundation Types", "Truy vấn FloorType & FoundationSlab trong Revit 2026");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 30
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối thành công tới Named Pipe: {pipeName}");

            string script = """
                var floorTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .Select(ft => {
                        var p = ft.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
                        double thick = p != null ? p.AsDouble() * 304.8 : 0.0;
                        return new {
                            id = ft.Id.ToString(),
                            name = ft.Name,
                            category = ft.Category != null ? ft.Category.Name : "",
                            catId = ft.Category != null ? ft.Category.Id.ToString() : "",
                            thicknessMm = Math.Round(thick, 1)
                        };
                    })
                    .OrderBy(ft => ft.name)
                    .ToList();

                var foundationTypes = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .WhereElementIsElementType()
                    .Select(et => new {
                        id = et.Id.ToString(),
                        name = et.Name,
                        family = et is FamilySymbol fs ? fs.FamilyName : et.GetType().Name,
                        category = et.Category != null ? et.Category.Name : ""
                    })
                    .OrderBy(et => et.name)
                    .ToList();

                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Select(l => new {
                        id = l.Id.ToString(),
                        name = l.Name,
                        elevMm = Math.Round(l.Elevation * 304.8, 1)
                    })
                    .OrderBy(l => l.elevMm)
                    .ToList();

                return new {
                    totalFloorTypes = floorTypes.Count,
                    floorTypes = floorTypes,
                    totalFoundationTypes = foundationTypes.Count,
                    foundationTypes = foundationTypes,
                    levels = levels
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 30, "Inspect Floor & Foundation Types");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                if (res.Diagnostics != null)
                {
                    foreach (var d in res.Diagnostics)
                    {
                        _formatter.WriteError($"  [{d.Id}] L{d.Line}:C{d.Column} - {d.Message}");
                    }
                }
                return 1;
            }

            string outPath = Path.Combine("scratch", "revit_floor_types.json");
            File.WriteAllText(outPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã lưu danh sách Floor & Foundation Types vào: {outPath}");

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối Revit: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
