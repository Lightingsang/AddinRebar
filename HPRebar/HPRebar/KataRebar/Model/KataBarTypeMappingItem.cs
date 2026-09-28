using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// One diameter of the plan and the RebarBarType it is drawn with; the type can be changed in the window.
/// </summary>
public sealed partial class KataBarTypeMappingItem : ObservableObject
{
    public string Role { get; init; } = "";

    public string Notation { get; init; } = "";

    public double DiameterMm { get; init; }

    [ObservableProperty]
    private RebarTypeOption? _selectedType;

    public IReadOnlyList<RebarTypeOption> AvailableTypes { get; init; } = Array.Empty<RebarTypeOption>();
}
