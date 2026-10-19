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
///     Queries available Structural Column family symbols in Revit 2026.
/// </summary>
public sealed class InspectColumnTypesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectColumnTypesCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        string outputPath = args.GetOption("output", Path.Combine("scratch", "revit_column_types.json"));

        _formatter.WriteBanner("Inspect Revit Column Types", "Truy vấn các loại cột kết cấu trong Revit 2026");

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
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .WhereElementIsElementType();

                var colTypes = new List<object>();
                foreach (FamilySymbol symbol in collector)
                {
                    double bMm = 0;
                    double hMm = 0;

                    var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH) 
                          ?? symbol.LookupParameter("b") 
                          ?? symbol.LookupParameter("Width")
                          ?? symbol.LookupParameter("B");

                    var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT) 
                          ?? symbol.LookupParameter("h") 
                          ?? symbol.LookupParameter("Height")
                          ?? symbol.LookupParameter("H");

                    if (pB != null && pB.HasValue)
                        bMm = Math.Round(pB.AsDouble() * 304.8, 1);
                    if (pH != null && pH.HasValue)
                        hMm = Math.Round(pH.AsDouble() * 304.8, 1);

                    colTypes.Add(new {
                        id = symbol.Id.Value,
                        familyName = symbol.FamilyName,
                        name = symbol.Name,
                        width = bMm,
                        height = hMm
                    });
                }

                var targetSymbol = doc.GetElement(new ElementId(9299509)) as FamilySymbol;
                var paramList = new List<object>();
                if (targetSymbol != null)
                {
                    foreach (Parameter p in targetSymbol.Parameters)
                    {
                        if (p.StorageType == StorageType.Double && p.HasValue)
                        {
                            paramList.Add(new {
                                name = p.Definition.Name,
                                valMm = Math.Round(p.AsDouble() * 304.8, 1),
                                isReadOnly = p.IsReadOnly
                            });
                        }
                    }
                }

                var instanceCount = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .WhereElementIsNotElementType()
                    .GetElementCount();

                return new {
                    existingInstanceCount = instanceCount,
                    totalTypes = colTypes.Count,
                    targetParams = paramList,
                    types = colTypes
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.None, false, 60, "Inspect Column Types");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                return 1;
            }

            string json = JsonSerializer.Serialize(res.Value.Value, JsonOptions);
            string dir = Path.GetDirectoryName(outputPath) ?? "scratch";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(outputPath, json);

            _formatter.WriteSuccess($"Đã lưu danh sách loại cột: {outputPath}");
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
}
