using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.Model;

/// <summary>
/// Snapshot of one beam run read from Revit. Everything the export window changes afterwards (the Name and
/// Count parameters, Normal/Reverse) is resolved from this snapshot without touching the Revit API.
/// </summary>
public sealed class KataExportSession
{
    public const string DefaultNameParameter = "STR_ElementName";
    public const string DefaultCountParameter = "STR_ElementCount";

    public required IReadOnlyList<ElementId> BeamIds { get; init; }

    public required IReadOnlyList<KataBeamPiece> Pieces { get; init; }

    public required IReadOnlyList<KataSupport> Supports { get; init; }

    public required IReadOnlyList<KataGridCrossing> Grids { get; init; }

    public required double HeightMm { get; init; }

    public required double WidthMm { get; init; }

    public required double LevelElevationMm { get; init; }

    public double? SlabThicknessMm { get; init; }

    public string? AxisGridName { get; init; }

    public double? AxisOffsetMm { get; init; }

    /// <summary>Instance parameters of the first element of the run, by name.</summary>
    public required IReadOnlyDictionary<string, object?> FirstBeamParameters { get; init; }

    /// <summary>Problems found while reading Revit (the sheet builder adds its own).</summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    public IReadOnlyList<string> ParameterNames =>
        FirstBeamParameters.Keys.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();

    public string? DefaultName => FirstBeamParameters.ContainsKey(DefaultNameParameter) ? DefaultNameParameter : null;

    public string? DefaultCount => FirstBeamParameters.ContainsKey(DefaultCountParameter) ? DefaultCountParameter : null;

    public KataRunInput ToInput(string? nameParameter, string? countParameter) => new(
        Pieces,
        Supports,
        Grids,
        new KataHeader(
            Lookup(nameParameter),
            Lookup(countParameter),
            HeightMm,
            WidthMm,
            LevelElevationMm,
            SlabThicknessMm,
            AxisGridName,
            AxisOffsetMm));

    private object? Lookup(string? name) =>
        name is not null && FirstBeamParameters.TryGetValue(name, out var value) ? value : null;
}
