using HPRebar.Core.KataExport.ColumnStack;
using HPRebar.Core.KataRebar.ColumnStack;

namespace HPRebar.KataExport.View.Web.ColumnExport;

/// <summary>Commands the Kata Column Rebar page sends (protocol v1 of HPWebHost), mirrored in the page's columnContracts.ts.</summary>
public static class KataColumnWebCommands
{
    /// <summary>The measured stack, the plan read from sheet Cot and the Excel state (<see cref="KataColumnPageState" />).</summary>
    public const string Load = "column.load";

    /// <summary>Looks at Excel again (<see cref="KataColumnWorkbook" />): the user activated the Kata workbook meanwhile.</summary>
    public const string Probe = "column.probe";

    /// <summary>Writes the stack into sheet Cot of the probed workbook (<see cref="KataColumnWriteResponse" />).</summary>
    public const string Write = "column.write";

    /// <summary>Step 2: creates the bars in Revit (<see cref="KataColumnGenerateResponse" />).</summary>
    public const string Generate = "column.generate";

    /// <summary>Step 2: reads sheet Cot again and plans anew (<see cref="KataColumnPageState" />).</summary>
    public const string Reload = "column.reload";

    /// <summary>Closes the window.</summary>
    public const string Close = "column.close";
}

public static class KataColumnWebEvents
{
    /// <summary>Revit's theme changed while the window is open (<see cref="KataColumnThemeEvent" />).</summary>
    public const string Theme = "column.theme";
}

/// <param name="Theme">"dark" or "light", as Revit shows.</param>
/// <param name="Mode">The step the page opens on: "export" (Revit → sheet Cot) or "rebar" (sheet Cot → Revit rebar).</param>
/// <param name="Preview">The stack to write (step 1).</param>
/// <param name="Rebar">The rebar plan read from sheet Cot (step 2; null while Excel cannot be read).</param>
public sealed record KataColumnPageState(string Theme, string Mode, KataColumnPreview? Preview, KataColumnRebarSummary? Rebar, KataColumnWorkbook Workbook);

public sealed record KataColumnGenerateResponse(bool Success, int Created, int Deleted, IReadOnlyList<string> Warnings, string Error);

/// <param name="Ready">The active workbook has sheet Cot; <paramref name="Error" /> says why not otherwise.</param>
public sealed record KataColumnWorkbook(bool Ready, string Name, string Error);

/// <param name="Name">C3, as the user may have corrected it on the page.</param>
/// <param name="Count">C4: how many identical columns.</param>
public sealed record KataColumnWriteRequest(string Name, int Count);

/// <param name="RowsWithoutBars">Storeys still without a bar count (E): Kata's VC refuses them until typed.</param>
public sealed record KataColumnWriteResponse(
    bool Success,
    int StoreysWritten,
    string Error,
    IReadOnlyList<string> RowsWithoutBars,
    KataColumnWorkbook Workbook);

public sealed record KataColumnThemeEvent(string Theme);
