using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// Project rebar type option with diameter for display in UI.
/// </summary>
public sealed record RebarTypeOption
{
    public string Name { get; init; } = "";
    public double DiameterMm { get; init; }
    public RebarDeformationType DeformationType { get; init; } = RebarDeformationType.Deformed;
    public ElementId Id { get; init; } = ElementId.InvalidElementId;
    public RebarBarType BarType { get; init; } = null!;

    public override string ToString() => $"{Name} (Ø{DiameterMm:0.#} mm)";
}

/// <summary>
/// Project hook type option for display and matching.
/// </summary>
public sealed record RebarHookOption
{
    public string Name { get; init; } = "";
    public double AngleDegrees { get; init; }
    public RebarStyle Style { get; init; }
    public ElementId Id { get; init; } = ElementId.InvalidElementId;
    public RebarHookType HookType { get; init; } = null!;
}

/// <summary>
/// Row item for mapping a specific bar diameter/notation from Kata to a Revit RebarBarType,
/// supporting interactive override via ComboBox in the UI.
/// </summary>
public sealed partial class KataBarTypeMappingItem : ObservableObject
{
    public string Role { get; init; } = "";
    public string Notation { get; init; } = "";
    public double DiameterMm { get; init; }
    public bool IsLongitudinal { get; init; }

    [ObservableProperty]
    private RebarTypeOption? _selectedType;

    public IReadOnlyList<RebarTypeOption> AvailableTypes { get; init; } = Array.Empty<RebarTypeOption>();
}
