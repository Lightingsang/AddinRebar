using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public class KataDamSheetParserTests
{
    [Fact]
    public void Parse_EmptySheet_ReturnsDefaultSpecWithNoErrors()
    {
        var table = new KataCellTable();
        var spec = KataDamSheetParser.Parse(table);

        Assert.NotNull(spec);
        Assert.Equal("", spec.BeamName);
        Assert.Equal(1, spec.BeamCount);
        Assert.Empty(spec.Supports);
        Assert.Empty(spec.Spans);
    }

    [Fact]
    public void Parse_GoldenKataSample_ParsesAllComponentsAccurately()
    {
        var table = new KataCellTable();

        // 1. Header
        table.Set("B3", "B01");
        table.Set("B4", 1);
        table.Set("B5", 1100.0);
        table.Set("B6", 500.0);
        table.Set("B7", 150.0);
        table.Set("B8", "Trục 1");
        table.Set("B9", -100.0);
        table.Set("B10", "+3.300");

        // 2. Detailing Rules
        table.Set("G2", 40.0);
        table.Set("G3", 30.0);
        table.Set("H3", 0.20);
        table.Set("I3", "L từ tâm cột");
        table.Set("H5", 0.25);
        table.Set("I5", "L từ mép cột");
        table.Set("J9", "50/25");

        // 3. Global Stirrup & Side Bars
        table.Set("G6", 10.0);
        table.Set("G7", "a150");
        table.Set("G8", "a200");
        table.Set("G9", 150.0);
        table.Set("I8", 2);
        table.Set("G4", 12.0);
        table.Set("G5", 2);

        // 4. Main Continuous
        table.Set("B11", "6f25");
        table.Set("B12", "6f25");

        // 5. Column C: Support 0
        table.Set(10, 3, "Cột");
        table.Set(11, 3, 400.0);
        table.Set(13, 3, "6f25");
        table.Set(14, 3, "6f20");
        table.Set(22, 3, "1");
        table.Set(23, 3, -150.0);

        // 6. Column D: Span 0
        table.Set(10, 4, "Nhịp");
        table.Set(11, 4, 10400.0);
        table.Set(18, 4, "6f25"); // Layer 1
        table.Set(17, 4, "2f20"); // Layer 2
        table.Set(19, 4, "-50");  // Top drop
        table.Set(20, 4, "0f12"); // Side bar override
        table.Set(22, 4, "a100/200"); // Stirrup override

        // 7. Column E: Support 1
        table.Set(10, 5, "Cột");
        table.Set(11, 5, 400.0);
        table.Set(13, 5, "6f25");
        table.Set(14, 5, "6f20");
        table.Set(15, 5, "6f20;0"); // Layer 3 compound
        table.Set(19, 5, "350;0");  // Upper column
        table.Set(20, 5, 400.0);    // Crossing beam
        table.Set(22, 5, "2");

        // 8. Column F: Span 1
        table.Set(10, 6, "Nhịp");
        table.Set(11, 6, 6500.0);
        table.Set(18, 6, "2f20");
        table.Set(21, 6, "-100;5f20"); // Soffit drop + bar

        // 9. Column G: Support 2
        table.Set(10, 7, "Cột");
        table.Set(11, 7, 400.0);
        table.Set(13, 7, "2f20;2f16");
        table.Set(22, 7, "3");

        // Execute parsing
        var spec = KataDamSheetParser.Parse(table);

        // Assertions
        Assert.Equal("B01", spec.BeamName);
        Assert.Equal(1, spec.BeamCount);
        Assert.Equal(500.0, spec.Width);
        Assert.Equal(1100.0, spec.Height);
        Assert.Equal(150.0, spec.SlabThickness);
        Assert.Equal("Trục 1", spec.AxisGridName);
        Assert.Equal(-100.0, spec.AxisOffset);
        Assert.Equal("+3.300", spec.LevelElevation);

        Assert.Equal(40.0, spec.TensionLapMultiplier);
        Assert.Equal(30.0, spec.CompressionLapMultiplier);
        Assert.Equal(0.25, spec.TopCutoffRatioLayer1);
        Assert.Equal(0.20, spec.TopCutoffRatioLayer2);
        Assert.Equal(KataCutoffOrigin.FromColumnFace, spec.CutoffOriginLayer1);
        Assert.Equal(KataCutoffOrigin.FromColumnCenter, spec.CutoffOriginLayer2);
        Assert.Equal(50.0, spec.CoverMain);
        Assert.Equal(25.0, spec.CoverStirrup);

        // Continuous bars
        Assert.Equal(6, spec.TopContinuous.Count);
        Assert.Equal(25.0, spec.TopContinuous.Diameter);
        Assert.Equal(6, spec.BottomContinuous.Count);
        Assert.Equal(25.0, spec.BottomContinuous.Diameter);

        // Global stirrups & side bars
        Assert.Equal(10.0, spec.GlobalStirrup.Diameter);
        Assert.Equal(150.0, spec.GlobalStirrup.SupportSpacing);
        Assert.Equal(200.0, spec.GlobalStirrup.MidspanSpacing);
        Assert.Equal(2, spec.GlobalSideBars.Count); // 2 layers

        // Supports
        Assert.Equal(3, spec.Supports.Count);
        Assert.Equal(400.0, spec.Supports[0].ColumnWidth);
        Assert.Equal("1", spec.Supports[0].GridName);
        Assert.Equal(-150.0, spec.Supports[0].GridOffset);
        Assert.Single(spec.Supports[0].TopExtraLayer1);
        Assert.Equal(6, spec.Supports[0].TopExtraLayer1[0].Count);
        Assert.Equal(25.0, spec.Supports[0].TopExtraLayer1[0].Diameter);

        // Support 1 upper column & crossing beam
        Assert.Equal(350.0, spec.Supports[1].UpperColumnWidth);
        Assert.Equal(400.0, spec.Supports[1].CrossingBeamWidth);
        Assert.Single(spec.Supports[1].TopExtraLayer3); // 6f20 (0 filtered)

        // Support 2 compound layer 1: 2f20;2f16
        Assert.Equal(2, spec.Supports[2].TopExtraLayer1.Count);
        Assert.Equal(20.0, spec.Supports[2].TopExtraLayer1[0].Diameter);
        Assert.Equal(16.0, spec.Supports[2].TopExtraLayer1[1].Diameter);

        // Spans
        Assert.Equal(2, spec.Spans.Count);
        Assert.Equal(10400.0, spec.Spans[0].Length);
        Assert.Single(spec.Spans[0].BottomExtraLayer1);
        Assert.Equal(6, spec.Spans[0].BottomExtraLayer1[0].Count);
        Assert.Single(spec.Spans[0].BottomExtraLayer2);
        Assert.Equal(2, spec.Spans[0].BottomExtraLayer2[0].Count);
        Assert.Equal(-50.0, spec.Spans[0].TopDrop);
        Assert.NotNull(spec.Spans[0].StirrupOverride);
        Assert.Equal(100.0, spec.Spans[0].StirrupOverride!.SupportSpacing);
        Assert.Equal(200.0, spec.Spans[0].StirrupOverride!.MidspanSpacing);

        // Span 1 soffit drop
        Assert.Equal(6500.0, spec.Spans[1].Length);
        Assert.Equal(-100.0, spec.Spans[1].SoffitDrop);
        Assert.Single(spec.Spans[1].SoffitDropBars);
        Assert.Equal(5, spec.Spans[1].SoffitDropBars[0].Count);
        Assert.Equal(20.0, spec.Spans[1].SoffitDropBars[0].Diameter);

        // Total beam length: 3 columns of 400 + 10400 + 6500 = 1200 + 16900 = 18100 mm
        Assert.Equal(18100.0, spec.CalculateTotalLengthMm());
    }

    [Fact]
    public void Parse_CantileverAndBeamSupport_IdentifiesCorrectly()
    {
        var table = new KataCellTable();
        table.Set("B3", "ConsoleBeam");
        table.Set("B5", 600.0);
        table.Set("B6", 300.0);

        // Support 0: Cantilever tip (0 width)
        table.Set(10, 3, "Cột");
        table.Set(11, 3, "0");

        // Span 0: Cantilever span (1500 mm)
        table.Set(10, 4, "Nhịp");
        table.Set(11, 4, 1500.0);

        // Support 1: Bearing on another girder "300x500"
        table.Set(10, 5, "Cột");
        table.Set(11, 5, "300x500");

        // Span 1: Main interior span (5000 mm)
        table.Set(10, 6, "Nhịp");
        table.Set(11, 6, 5000.0);
        table.Set(22, 6, "a100/200/50"); // 3-zone asymmetric

        // Support 2: Column 400 mm
        table.Set(10, 7, "Cột");
        table.Set(11, 7, 400.0);

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(3, spec.Supports.Count);
        Assert.True(spec.Supports[0].IsCantilever);
        Assert.Equal(0.0, spec.Supports[0].ColumnWidth);

        Assert.False(spec.Supports[1].IsCantilever);
        Assert.Equal(300.0, spec.Supports[1].ColumnWidth);
        Assert.Equal("300x500", spec.Supports[1].SupportSection);

        Assert.Equal(2, spec.Spans.Count);
        Assert.Equal(1500.0, spec.Spans[0].Length);
        Assert.Equal(5000.0, spec.Spans[1].Length);
        Assert.NotNull(spec.Spans[1].StirrupOverride);
        Assert.Equal(100.0, spec.Spans[1].StirrupOverride!.SupportSpacing);
        Assert.Equal(200.0, spec.Spans[1].StirrupOverride!.MidspanSpacing);
        Assert.Equal(50.0, spec.Spans[1].StirrupOverride!.EndSupportSpacing);
    }

    [Fact]
    public void Parse_TopExtra4Layers_PopulatesAllLayersCorrectly()
    {
        var table = new KataCellTable();
        table.Set("B3", "DeepTransferBeam");
        table.Set(10, 3, "Cột");
        table.Set(11, 3, 600.0);
        table.Set(13, 3, "4f25"); // Layer 1
        table.Set(14, 3, "4f22"); // Layer 2
        table.Set(15, 3, "2f20"); // Layer 3
        table.Set(16, 3, "2f18"); // Layer 4

        var spec = KataDamSheetParser.Parse(table);

        Assert.Single(spec.Supports);
        var supp = spec.Supports[0];
        Assert.Equal(4, supp.AllTopExtraLayers.Count);
        Assert.Equal(25.0, supp.TopExtraLayer1[0].Diameter);
        Assert.Equal(22.0, supp.TopExtraLayer2[0].Diameter);
        Assert.Equal(20.0, supp.TopExtraLayer3[0].Diameter);
    }

    [Fact]
    public void Parse_CellJ7_IsKeptAsANoteOnly()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("J7", "a500");

        var spec = KataDamSheetParser.Parse(table);

        Assert.Contains(spec.DetailingNotes, n => n.Address == "J7" && n.Text == "a500");
    }

    [Fact]
    public void Parse_Row20_SideBarOverride_SupportsZeroCountAndSingleLayer()
    {
        var table = new KataCellTable();
        table.Set("B3", "BeamRow20");
        table.Set(10, 3, "Cột");
        table.Set(11, 3, 400.0);
        table.Set(10, 4, "Nhịp");
        table.Set(11, 4, 6000.0);
        table.Set(20, 4, "0f12");
        table.Set(10, 5, "Cột");
        table.Set(11, 5, 400.0);
        table.Set(10, 6, "Nhịp");
        table.Set(11, 6, 6000.0);
        table.Set(20, 6, "1f12");
        table.Set(10, 7, "Cột");
        table.Set(11, 7, 400.0);

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(2, spec.Spans.Count);
        Assert.Single(spec.Spans[0].SideBars);
        Assert.Equal(0, spec.Spans[0].SideBars[0].Count);
        Assert.Equal(12.0, spec.Spans[0].SideBars[0].Diameter);

        Assert.Single(spec.Spans[1].SideBars);
        Assert.Equal(1, spec.Spans[1].SideBars[0].Count);
        Assert.Equal(12.0, spec.Spans[1].SideBars[0].Diameter);
    }
}
