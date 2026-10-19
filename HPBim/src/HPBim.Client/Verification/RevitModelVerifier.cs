namespace HPBim.Client.Verification;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Models;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Queries active Revit Document element counts via read-only script and verifies against expected BIM model.
/// </summary>
public sealed class RevitModelVerifier : IRevitModelVerifier
{
    private readonly IRevitPipeClient _pipeClient;

    public RevitModelVerifier(IRevitPipeClient pipeClient)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
    }

    public async Task<ModelVerificationReport> VerifyAsync(BimModel expectedModel, CancellationToken cancellationToken = default)
    {
        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var request = new ExecuteRequest(
            Code: GetVerificationQueryScript(),
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 60,
            Label: "Verify BIM Model Element Counts");

        var result = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsError || result.Value == null)
        {
            throw new RevitPipeException($"Model verification script failed: {result.Message}");
        }

        return CompareCounts(expectedModel, result.Value.Value);
    }

    public static string GetVerificationQueryScript() => """
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).GetElementCount();
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).GetElementCount();
        var columns = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns).GetElementCount();
        var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).GetElementCount();
        var doors = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Doors).GetElementCount();
        var windows = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_Windows).GetElementCount();
        var rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).OfCategory(BuiltInCategory.OST_Rooms).GetElementCount();
        var beams = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).GetElementCount();
        var slabs = new FilteredElementCollector(doc).OfClass(typeof(Floor)).GetElementCount();
        var pipes = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.Pipe)).GetElementCount();
        var rebars = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Structure.Rebar)).GetElementCount();

        return new
        {
            levels,
            grids,
            columns,
            walls,
            doors,
            windows,
            rooms,
            beams,
            slabs,
            pipes,
            rebars
        };
        """;

    private readonly record struct ElementCounts(
        int Levels, int Grids, int Columns, int Walls,
        int Doors, int Windows, int Rooms, int Beams,
        int Slabs, int Pipes, int Rebars);

    private static ModelVerificationReport CompareCounts(BimModel exp, JsonElement actual)
    {
        var expCounts = GetExpectedCounts(exp);
        var actCounts = GetActualCounts(actual);
        var discrepancies = CheckAllDiscrepancies(expCounts, actCounts);
        return CreateReport(expCounts, actCounts, discrepancies);
    }

    private static ElementCounts GetExpectedCounts(BimModel exp)
    {
        int expDoors = exp.Walls?.SelectMany(w => w.Openings ?? Array.Empty<OpeningDef>()).Count(o => o.Kind == OpeningKind.Door) ?? 0;
        int expWindows = exp.Walls?.SelectMany(w => w.Openings ?? Array.Empty<OpeningDef>()).Count(o => o.Kind == OpeningKind.Window) ?? 0;
        return new ElementCounts(
            exp.Levels?.Count ?? 0,
            exp.Grids?.Count ?? 0,
            exp.Columns?.Count ?? 0,
            exp.Walls?.Count ?? 0,
            expDoors,
            expWindows,
            exp.Rooms?.Count ?? 0,
            exp.Beams?.Count ?? 0,
            exp.Slabs?.Count ?? 0,
            exp.Pipes?.Count ?? 0,
            exp.Rebars?.Count ?? 0);
    }

    private static ElementCounts GetActualCounts(JsonElement el) => new(
        ReadInt(el, "levels"), ReadInt(el, "grids"), ReadInt(el, "columns"),
        ReadInt(el, "walls"), ReadInt(el, "doors"), ReadInt(el, "windows"),
        ReadInt(el, "rooms"), ReadInt(el, "beams"), ReadInt(el, "slabs"),
        ReadInt(el, "pipes"), ReadInt(el, "rebars"));

    private static List<string> CheckAllDiscrepancies(ElementCounts exp, ElementCounts act)
    {
        var items = new (string Category, int Exp, int Act)[]
        {
            ("Levels", exp.Levels, act.Levels),
            ("Grids", exp.Grids, act.Grids),
            ("Columns", exp.Columns, act.Columns),
            ("Walls", exp.Walls, act.Walls),
            ("Doors", exp.Doors, act.Doors),
            ("Windows", exp.Windows, act.Windows),
            ("Rooms", exp.Rooms, act.Rooms),
            ("Beams", exp.Beams, act.Beams),
            ("Slabs", exp.Slabs, act.Slabs),
            ("Pipes", exp.Pipes, act.Pipes),
            ("Rebars", exp.Rebars, act.Rebars)
        };

        var disc = new List<string>();
        foreach (var (cat, e, a) in items)
        {
            if (e != a) disc.Add($"{cat}: expected {e}, found {a} in Revit (diff: {a - e})");
        }
        return disc;
    }

    private static ModelVerificationReport CreateReport(ElementCounts e, ElementCounts a, List<string> disc) => new()
    {
        IsMatch = disc.Count == 0,
        ExpectedLevels = e.Levels, ActualLevels = a.Levels,
        ExpectedGrids = e.Grids, ActualGrids = a.Grids,
        ExpectedColumns = e.Columns, ActualColumns = a.Columns,
        ExpectedWalls = e.Walls, ActualWalls = a.Walls,
        ExpectedDoors = e.Doors, ActualDoors = a.Doors,
        ExpectedWindows = e.Windows, ActualWindows = a.Windows,
        ExpectedRooms = e.Rooms, ActualRooms = a.Rooms,
        ExpectedBeams = e.Beams, ActualBeams = a.Beams,
        ExpectedSlabs = e.Slabs, ActualSlabs = a.Slabs,
        ExpectedPipes = e.Pipes, ActualPipes = a.Pipes,
        ExpectedRebars = e.Rebars, ActualRebars = a.Rebars,
        Discrepancies = disc
    };

    private static int ReadInt(JsonElement el, string propertyName)
    {
        if (el.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out int val))
        {
            return val;
        }
        return 0;
    }
}
