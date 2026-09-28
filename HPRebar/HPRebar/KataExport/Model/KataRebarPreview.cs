using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.Model;

/// <summary>
/// The sheet's bars planned on the beams Revit measured, handed from the API thread to the window. It holds
/// no Revit object, so the canvas can draw it and the generation re-plans the same sheet the same way.
/// </summary>
public sealed class KataRebarPreview
{
    public bool IsSuccess { get; init; }

    /// <summary>Why the beams could not be measured when <see cref="IsSuccess"/> is false.</summary>
    public string Message { get; init; } = "";

    public KataRebarPlan? Plan { get; init; }

    /// <summary>Remarks of the measurement (a support not found, a beam out of line…).</summary>
    public IReadOnlyList<string> MeasureWarnings { get; init; } = Array.Empty<string>();

    public static KataRebarPreview Failed(string message) => new() { IsSuccess = false, Message = message };
}
