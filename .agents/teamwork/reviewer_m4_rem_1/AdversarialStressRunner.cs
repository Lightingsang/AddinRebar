using System;
using System.Collections.Generic;
using HPKata.Core.DrawingLayout.Calculators;
using HPKata.Core.DrawingLayout.Models;
using HPKata.Core.QuantitySurveying.Calculators;
using HPKata.Core.QuantitySurveying.Models;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("--- ADVERSARIAL STRESS TEST SUITE ---");

        // Test 1: TitleBlock equal to full sheet size
        var sheetDef = SheetSizeDefinition.Create(SheetFormat.A4, false); // 210 x 297
        var hugeTb = new TitleBlockModel
        {
            Placement = TitleBlockPlacement.BottomRight,
            WidthMm = 200,
            HeightMm = 280
        };
        var normalBlock = new DrawingBlockItem("Block1", 50, 50);
        var res1 = SheetArrangerCalculator.ArrangeBlocks(new[] { normalBlock }, sheetDef, hugeTb);
        Console.WriteLine($"Huge TitleBlock: sheets={res1.Count}, placed={ (res1.Count > 0 ? res1[0].PlacedBlocks.Count : 0) }");

        // Test 2: QsTakeoffCalculator with extreme / degenerate inputs
        var degenerateCol = new QsElementModel
        {
            MemberType = QsMemberType.Column,
            WidthMm = double.NaN,
            HeightMm = -100,
            LengthMm = 0
        };
        double vol = QsTakeoffCalculator.CalculateGrossVolume(degenerateCol);
        Console.WriteLine($"Degenerate Column Volume: {vol}");

        // Test 3: QsTakeoffCalculator with huge numbers
        var hugeBeam = new QsElementModel
        {
            MemberType = QsMemberType.Beam,
            WidthMm = 1e6, // 1 km wide
            HeightMm = 1e6,
            LengthMm = 1e6
        };
        double hugeVol = QsTakeoffCalculator.CalculateGrossVolume(hugeBeam);
        Console.WriteLine($"Huge Beam Volume: {hugeVol} m3");

        // Test 4: Formwork calculator with self-intersecting ID
        var elemA = new QsElementModel
        {
            Id = "A",
            Mark = "A",
            MemberType = QsMemberType.Column,
            WidthMm = 400,
            HeightMm = 400,
            LengthMm = 3000,
            IntersectingElementIds = new List<string> { "A", "B" }
        };
        var elemB = new QsElementModel
        {
            Id = "B",
            Mark = "B",
            MemberType = QsMemberType.Beam,
            WidthMm = 200,
            HeightMm = 400,
            LengthMm = 4000,
            IntersectingElementIds = new List<string> { "A" }
        };
        var fwResults = QsFormworkCalculator.Evaluate(new[] { elemA, elemB });
        Console.WriteLine($"Column A Net Formwork: {fwResults["A"].NetAreaM2:F4} m2 (Gross: {fwResults["A"].GrossSideAreaM2:F4}, Ded: {fwResults["A"].DeductedAreaM2:F4})");
        Console.WriteLine($"Beam B Net Formwork: {fwResults["B"].NetAreaM2:F4} m2 (Gross: {fwResults["B"].GrossSideAreaM2:F4}, Ded: {fwResults["B"].DeductedAreaM2:F4})");

        Console.WriteLine("ALL ADVERSARIAL STRESS CASES COMPLETED SUCCESSFULLY!");
    }
}
