using System.Collections.Generic;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.Tests.KataRebar.ColumnStack;

/// <summary>Kata.xlsm's sample column (sheet "Cot") and its storeys in model millimetres.</summary>
internal static class KataColumnTestSheet
{
    /// <summary>Sheet "Cot" of Kata.xlsm as shipped (C:\kata_pro), optionally with E/F/H changed on every storey.</summary>
    public static KataCellTable Sample(int e = 7, int f = 7, int h = 7, double? j = null)
    {
        var cells = new Dictionary<string, object?>
        {
            ["C1"] = 800.0, ["C2"] = 200.0, ["C3"] = "C1", ["C4"] = 1.0, ["C5"] = -5.5,
            ["F4"] = "30/100", ["F5"] = 15.0, ["H4"] = "d10a100/200", ["H5"] = "d8a400",
            ["G3"] = false, ["H3"] = false, ["I3"] = true, ["J3"] = 2.0, ["K3"] = true,
            ["A9"] = "PILECAPS", ["B9"] = 1.5,
        };
        string[] names = { "BASEMENT", "FLOOR 1", "FLOOR 2", "FLOOR 3", "FLOOR 4", "FLOOR 5" };
        double[] heights = { 3.0, 3.5, 3.9, 4.6, 3.3, 3.3 };
        object[] diameters = { 25.0, 25.0, "25/20", 20.0, 20.0, 20.0 };
        double[] beams = { 600, 600, 400, 400, 600, 400 };
        for (int i = 0; i < names.Length; i++)
        {
            int row = 10 + i;
            cells[$"A{row}"] = names[i];
            cells[$"B{row}"] = heights[i];
            cells[$"C{row}"] = 1000.0;
            cells[$"D{row}"] = 800.0;
            cells[$"E{row}"] = (double)e;
            cells[$"F{row}"] = f == 0 ? null : f;
            cells[$"G{row}"] = 7.0;
            cells[$"H{row}"] = h == 0 ? null : h;
            cells[$"I{row}"] = diameters[i];
            cells[$"K{row}"] = beams[i];
            cells[$"L{row}"] = "d10a100/200";
            cells[$"J{row}"] = j;
        }

        return new KataCellTable(cells);
    }

    /// <summary>The sample's five storeys (the sixth row is Kata's stub): cap -5500…-4000, then 3.0/3.5/3.9/4.6/3.3 m, 1000 × 800.</summary>
    public static IReadOnlyList<KataColumnStoreyGeometry> Storeys()
    {
        double[] heights = { 3000, 3500, 3900, 4600, 3300 };
        var storeys = new List<KataColumnStoreyGeometry>();
        double baseMm = -4000;
        foreach (double height in heights)
        {
            storeys.Add(new KataColumnStoreyGeometry(baseMm, baseMm + height, 0, 1000, 0, 800));
            baseMm += height;
        }

        return storeys;
    }
}
