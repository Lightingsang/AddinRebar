namespace HPRebar.Core.KataRebar.Models;

/// <summary>A sheet cell kept only so its content can be reported (e.g. input a later version will draw).</summary>
/// <param name="Address">A1 address of the cell, e.g. "D23".</param>
/// <param name="Text">Cell content as shown in Excel.</param>
/// <param name="Meaning">What the cell means in Kata, in the words shown to the user.</param>
/// <param name="Outcome">What HPRebar does with it; null = not drawn yet.</param>
public sealed record KataCellNote(string Address, string Text, string Meaning, string? Outcome = null);
