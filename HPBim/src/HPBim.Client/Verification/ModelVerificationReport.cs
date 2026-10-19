namespace HPBim.Client.Verification;

using System;
using System.Collections.Generic;

/// <summary>
///     Comprehensive verification report comparing the expected BIM model against elements in Revit.
/// </summary>
public sealed record ModelVerificationReport
{
    public bool IsMatch { get; init; }
    public int ExpectedLevels { get; init; }
    public int ActualLevels { get; init; }
    public int ExpectedGrids { get; init; }
    public int ActualGrids { get; init; }
    public int ExpectedColumns { get; init; }
    public int ActualColumns { get; init; }
    public int ExpectedWalls { get; init; }
    public int ActualWalls { get; init; }
    public int ExpectedDoors { get; init; }
    public int ActualDoors { get; init; }
    public int ExpectedWindows { get; init; }
    public int ActualWindows { get; init; }
    public int ExpectedRooms { get; init; }
    public int ActualRooms { get; init; }
    public int ExpectedBeams { get; init; }
    public int ActualBeams { get; init; }
    public int ExpectedSlabs { get; init; }
    public int ActualSlabs { get; init; }
    public int ExpectedPipes { get; init; }
    public int ActualPipes { get; init; }
    public int ExpectedRebars { get; init; }
    public int ActualRebars { get; init; }
    public IReadOnlyList<string> Discrepancies { get; init; } = Array.Empty<string>();
}
