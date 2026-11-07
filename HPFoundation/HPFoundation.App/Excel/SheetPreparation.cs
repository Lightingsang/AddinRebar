using HPFoundation.Core.Model;
using HPFoundation.Core.Sheets;

namespace HPFoundation.App.Excel;

/// <summary>One moment cell of the sheet before and after the convention change.</summary>
public sealed record MomentChange(string Cell, double? OldValueTm, double? NewValueTm);

/// <summary>What preparing one sheet produced. Input may be shown for checking, but use it only when IsReady.</summary>
public sealed record PreparedSheet(
    string SheetName,
    FoundationInput? Input,
    IReadOnlyList<string> MissingAnchors,
    IReadOnlyList<SheetIssue> Issues,
    IReadOnlyList<MomentChange> MomentChanges)
{
    public bool IsReady => Input is not null && MissingAnchors.Count == 0 && Issues.Count == 0;
}

/// <summary>
///     Applies the ETABS moment convention to the sheet, lets Excel recalculate and reads the inputs.
///     The order matters: the base moments feed eccentricity, soil pressure, settlement and so the spring stiffness.
/// </summary>
public static class SheetPreparation
{
    public static PreparedSheet Prepare(ExcelSession excel, string sheetName)
    {
        var before = excel.ReadGrid(sheetName);
        var located = SheetLayoutLocator.Locate(before);
        if (located.Layout is not { } layout)
        {
            return new PreparedSheet(sheetName, null, located.MissingAnchors, [], []);
        }

        RewriteMoments(excel, sheetName, before, layout);
        excel.CalculateAll();
        var after = excel.ReadGrid(sheetName);

        var built = FoundationInputBuilder.Build(after, layout);
        var inputIssues = built.Input is { } input ? InputValidator.Validate(input) : [];
        var issues = InputValidator.FailedCells(after).Concat(built.Issues).Concat(inputIssues).ToList();
        var changes = new[] { layout.LongDirectionMoment, layout.ShortDirectionMoment }
            .Select(cell => new MomentChange(cell.ToString(), before.CellAt(cell)?.Number, after.CellAt(cell)?.Number))
            .ToList();
        return new PreparedSheet(sheetName, built.Input, [], issues, changes);
    }

    private static void RewriteMoments(ExcelSession excel, string sheetName, SheetGrid grid, SheetLayout layout)
    {
        var longFormula = grid.CellAt(layout.LongDirectionMoment)!.Formula!;
        var shortFormula = grid.CellAt(layout.ShortDirectionMoment)!.Formula!;
        excel.WriteFormula(sheetName, layout.LongDirectionMoment, MomentFormulaRewriter.RewriteLongDirection(longFormula));
        excel.WriteFormula(sheetName, layout.ShortDirectionMoment, MomentFormulaRewriter.RewriteShortDirection(shortFormula));
    }
}
