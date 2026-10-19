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
///     Queries available Structural Framing (beam) family symbols in Revit 2026.
/// </summary>
public sealed class InspectBeamTypesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectBeamTypesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "revit_beam_types.json"));

        _formatter.WriteBanner("Inspect Revit Beam Types", "Truy vấn các loại dầm bê tông cốt thép trong Revit 2026");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            _formatter.WriteSuccess($"Đã kết nối Named Pipe Revit: {pipeName}");

            string script = """
                var collector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsElementType();

                var beamTypes = new List<object>();
                foreach (FamilySymbol symbol in collector)
                {
                    double bMm = 0;
                    double hMm = 0;

                    var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) 
                          ?? symbol.LookupParameter("b") 
                          ?? symbol.LookupParameter("Width");
                    if (pB != null && pB.HasValue) bMm = Math.Round(pB.AsDouble() * 304.8, 1);

                    var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT) 
                          ?? symbol.LookupParameter("h") 
                          ?? symbol.LookupParameter("Height");
                    if (pH != null && pH.HasValue) hMm = Math.Round(pH.AsDouble() * 304.8, 1);

                    beamTypes.Add(new {
                        id = symbol.Id.Value,
                        familyName = symbol.FamilyName,
                        name = symbol.Name,
                        width = bMm,
                        height = hMm,
                        category = symbol.Category?.Name ?? ""
                    });
                }

                return new {
                    totalTypes = beamTypes.Count,
                    beamTypes = beamTypes
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Beam Types");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                return 1;
            }

            File.WriteAllText(outputPath, JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            _formatter.WriteSuccess($"Đã lưu danh sách loại dầm vào: {outputPath}");

            // Print summary
            using var docRes = JsonDocument.Parse(res.Value.Value.GetRawText());
            var root = docRes.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                using var inner = JsonDocument.Parse(root.GetString() ?? "{}");
                PrintBeams(inner.RootElement);
            }
            else
            {
                PrintBeams(root);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi kết nối: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void PrintBeams(JsonElement root)
    {
        var types = root.GetProperty("beamTypes").EnumerateArray().ToList();
        _formatter.WriteInfo($"Tìm thấy {types.Count} loại dầm (Structural Framing) trong template:");
        foreach (var t in types)
        {
            long id = t.GetProperty("id").GetInt64();
            string family = t.GetProperty("familyName").GetString() ?? "";
            string name = t.GetProperty("name").GetString() ?? "";
            double w = t.GetProperty("width").GetDouble();
            double h = t.GetProperty("height").GetDouble();
            Console.WriteLine($"  - [Id {id, 7}] Family: '{family}', Type: '{name}' (b={w}mm, h={h}mm)");
        }
    }
}
