using System;
using System.Collections.Generic;

namespace HPRebar.KataRebar.Model;

/// <summary>What one Kata Rebar run did to the model.</summary>
public sealed class KataRebarGenerationResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = "";

    /// <summary>Bars of the previous run on the same beams, deleted first.</summary>
    public int DeletedCount { get; init; }

    public int MainBarCount { get; init; }

    /// <summary>Additional top bars over the supports.</summary>
    public int ExtraTopBarCount { get; init; }

    /// <summary>Additional bottom bars in the spans.</summary>
    public int ExtraBottomBarCount { get; init; }

    /// <summary>Side bars ("cốt giá").</summary>
    public int SideBarCount { get; init; }

    /// <summary>Sets of flat bars: C ties of the side bars, inner U / C stirrups.</summary>
    public int BarSetCount { get; init; }

    public int StirrupSetCount { get; init; }

    /// <summary>Single stirrups drawn where a set could not be made.</summary>
    public int StirrupSingleBarCount { get; init; }

    /// <summary>Revit warnings cleared during the run (e.g. a bar reaching outside its host).</summary>
    public IReadOnlyList<string> RevitWarnings { get; init; } = Array.Empty<string>();

    public static KataRebarGenerationResult Failed(string message) => new() { IsSuccess = false, Message = message };
}
