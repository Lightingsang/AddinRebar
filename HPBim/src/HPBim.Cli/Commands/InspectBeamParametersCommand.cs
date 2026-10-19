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
///     Queries detailed parameter structure of a Structural Framing instance in Revit.
/// </summary>
public sealed class InspectBeamParametersCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;

    public InspectBeamParametersCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));

        _formatter.WriteBanner("Inspect Beam Parameters", "Kiểm tra chi tiết tham số định vị và offset của Dầm trong Revit");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 8000,
            DefaultExecuteTimeoutSeconds = 60
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);

            string script = """
                var beam = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .FirstOrDefault();

                if (beam == null)
                {
                    return new { error = "Không tìm thấy dầm nào trong mô hình." };
                }

                var paramList = new List<object>();
                foreach (Parameter p in beam.Parameters)
                {
                    string defName = p.Definition != null ? p.Definition.Name : "";
                    string valStr = "";
                    try
                    {
                        valStr = p.AsValueString() ?? p.AsString() ?? p.AsInteger().ToString();
                    }
                    catch
                    {
                        valStr = "n/a";
                    }

                    int bId = -1;
                    try
                    {
                        var bip = (BuiltInParameter)((InternalDefinition)p.Definition).BuiltInParameter;
                        bId = (int)bip;
                    }
                    catch
                    {
                    }

                    if (defName.IndexOf("Justif", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Offset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Y ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Z ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Geometric", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("Reference", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int intVal = p.StorageType == StorageType.Integer ? p.AsInteger() : -999;
                        paramList.Add(new {
                            name = defName,
                            bipId = bId,
                            storageType = p.StorageType.ToString(),
                            value = valStr,
                            intValue = intVal,
                            readOnly = p.IsReadOnly
                        });
                    }
                }

                var level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => Math.Abs(l.Elevation * 304.8 - (-100.0)) < 1.0 || l.Name == "Tầng 1");

                var sym = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => s.Name == "B_200x400");

                if (sym != null && level != null)
                {
                    if (!sym.IsActive) sym.Activate();
                    // Draw a beam from (0, 0) to (5000, 0) in mm
                    var p1 = new XYZ(0, 0, level.Elevation);
                    var p2 = new XYZ(5000.0 / 304.8, 0, level.Elevation);
                    var line = Line.CreateBound(p1, p2);
                    var testBeam = doc.Create.NewFamilyInstance(line, sym, level, Autodesk.Revit.DB.Structure.StructuralType.Beam);

                    testBeam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)?.Set(0); // Top
                    testBeam.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)?.Set(0); // Left
                    testBeam.get_Parameter(BuiltInParameter.Y_OFFSET_VALUE)?.Set(100.0 / 304.8); // +100mm offset

                    doc.Regenerate();
                    var bb = testBeam.get_BoundingBox(null);
                    double minX = bb.Min.X * 304.8;
                    double maxX = bb.Max.X * 304.8;
                    double minY = bb.Min.Y * 304.8;
                    double maxY = bb.Max.Y * 304.8;
                    double minZ = bb.Min.Z * 304.8;
                    double maxZ = bb.Max.Z * 304.8;

                    return new {
                        testBeamCreated = true,
                        yJustification = testBeam.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)?.AsInteger(),
                        yJustificationStr = testBeam.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)?.AsValueString(),
                        yOffsetMm = Math.Round(testBeam.get_Parameter(BuiltInParameter.Y_OFFSET_VALUE)?.AsDouble() * 304.8 ?? 0, 1),
                        zJustificationStr = testBeam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)?.AsValueString(),
                        bbox = new { minX, maxX, minY, maxY, minZ, maxZ }
                    };
                }

                return new {
                    beamId = beam.Id.Value,
                    symbolName = beam.Symbol?.Name ?? "",
                    paramsFound = paramList
                };
            """;

            var req = new ExecuteRequest(script, TransactionModes.Auto, true, 60, "Inspect Beam Params");
            var res = await client.ExecuteAsync(req, ct).ConfigureAwait(false);
            if (res.IsError || !res.Value.HasValue)
            {
                _formatter.WriteError($"Lỗi Revit: {res.Message}");
                return 1;
            }

            Console.WriteLine(JsonSerializer.Serialize(res.Value.Value, JsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
