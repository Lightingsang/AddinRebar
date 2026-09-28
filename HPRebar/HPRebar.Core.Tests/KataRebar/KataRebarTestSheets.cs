using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Sheet 'Dam' contents shared by the Kata Rebar tests.</summary>
internal static class KataRebarTestSheets
{
    /// <summary>
    /// One 300×600 beam, 6000 mm clear between two 400 mm columns, as KataExport writes it and a user fills
    /// in B11/B12/G2/G3/G6/G7/G8/J9: the Revit live check uses the same numbers.
    /// </summary>
    public static KataCellTable SingleSpan(string cover = "43/25")
    {
        var table = new KataCellTable();
        table.Set("B3", "DT1");
        table.Set("B4", 1);
        table.Set("B5", 600.0);
        table.Set("B6", 300.0);
        table.Set("B10", "+3.600");
        table.Set("B11", "3f20");
        table.Set("B12", "4f20");
        table.Set("G2", 40.0);
        table.Set("G3", 30.0);
        table.Set("G6", 8.0);
        table.Set("G7", "a100");
        table.Set("G8", "a200");
        table.Set("J9", cover);
        table.Set("C10", "Cột");
        table.Set("D10", "Nhịp");
        table.Set("E10", "Cột ");
        table.Set("C11", 400.0);
        table.Set("D11", 6000.0);
        table.Set("E11", 400.0);
        return table;
    }

    public static KataMeasuredBeam MeasuredSingleSpan(double left = 400.0, double span = 6000.0, double right = 400.0) =>
        new(300.0, 600.0, new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, left),
            new(KataMeasuredSupportKind.None, span),
            new(KataMeasuredSupportKind.Column, right)
        }, 1);
}
