using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Parses sheet 'Dam' of Kata.xlsm into a strongly-typed <see cref="KataBeamRebarSpec"/>
/// using an <see cref="IKataDamCellAccessor"/>.
/// Pure logic layer with 0 dependencies on Excel or Autodesk.Revit.*.
/// </summary>
public static class KataDamSheetParser
{
    private const int FirstDataColumn = 3;  // Column C
    private const int MaxDataColumn = 78;   // Column BZ

    /// <summary>
    /// Parses the entire sheet grid into a <see cref="KataBeamRebarSpec"/>.
    /// </summary>
    /// <param name="accessor">The cell accessor abstraction providing access to the sheet grid.</param>
    /// <returns>A fully populated <see cref="KataBeamRebarSpec"/>.</returns>
    public static KataBeamRebarSpec Parse(IKataDamCellAccessor accessor)
    {
        if (accessor is null)
            throw new ArgumentNullException(nameof(accessor));

        // 1. Header (B3:B10)
        string beamName = accessor.GetText(3, 2) ?? "";
        int beamCount = accessor.GetInt(4, 2) ?? 1;
        if (beamCount < 1) beamCount = 1;
        double height = accessor.GetDouble(5, 2) ?? 0.0;
        double width = accessor.GetDouble(6, 2) ?? 0.0;
        double slabThickness = accessor.GetDouble(7, 2) ?? 0.0;
        string axisGridName = accessor.GetText(8, 2) ?? "";
        double axisOffset = accessor.GetDouble(9, 2) ?? 0.0;
        string levelElevation = accessor.GetText(10, 2) ?? "";

        // 2. Detailing Rules (G1:G9, H3, H5, I3, I5, J9)
        string g1Text = accessor.GetText("G1")?.Trim() ?? "";
        double curtailedExtension = KataBarNotationParser.ParsePositive(g1Text);
        double tensionLap = accessor.GetDouble("G2") ?? 40.0;
        double compLap = accessor.GetDouble("G3") ?? 30.0;
        double topCutoffL2 = accessor.GetDouble("H3") ?? 0.20;
        double topCutoffL1 = accessor.GetDouble("H5") ?? 0.25;

        string originL1Text = accessor.GetText("I5") ?? "";
        var originL1 = originL1Text.IndexOf("tâm", StringComparison.OrdinalIgnoreCase) >= 0
            ? KataCutoffOrigin.FromColumnCenter
            : KataCutoffOrigin.FromColumnFace;

        string originL2Text = accessor.GetText("I3") ?? "";
        var originL2 = originL2Text.IndexOf("tâm", StringComparison.OrdinalIgnoreCase) >= 0
            ? KataCutoffOrigin.FromColumnCenter
            : KataCutoffOrigin.FromColumnFace;

        var (coverMain, coverStirrup) = KataBarNotationParser.ParseCover(accessor.GetText("J9"));

        // 3. Global Stirrup (G6:G9, I8, J7, Rows 25-27)
        double stirrupDia = accessor.GetDouble("G6") ?? 10.0;
        string? g7Text = accessor.GetText("G7");
        string? g8Text = accessor.GetText("G8");
        var (sDense, _, _) = KataBarNotationParser.ParseStirrupSpacing(g7Text, 150.0, 150.0);
        var (_, sMid, _) = KataBarNotationParser.ParseStirrupSpacing(g8Text, 200.0, 200.0);
        var (sCantilever, _, _) = KataBarNotationParser.ParseStirrupSpacing(accessor.GetText("G9"), 150.0, 150.0);
        // Option group "Khoảng cách đai gia cường": its linked cell I8 is 1 for "Giống đai ngoài" and 2 for
        // "Bố trí đều với" J7; the C ties follow it.
        string j7Text = accessor.GetText("J7")?.Trim() ?? "";
        var tieMode = accessor.GetInt("I8") == 1 ? KataTieSpacingMode.LikeHoops : KataTieSpacingMode.Uniform;

        var notes = new List<KataCellNote>();
        var branches = new[] { KataStirrupBranchSpec.Outer };
        var globalStirrup = new KataStirrupSpec
        {
            Diameter = stirrupDia,
            SupportSpacing = sDense,
            MidspanSpacing = sMid,
            CantileverSpacing = sCantilever,
            TieSpacing = ParseTieSpacing(j7Text),
            TieSpacingText = j7Text,
            TieSpacingMode = tieMode,
            Branches = branches
        };

        // 4. Continuous Main Bars (B11, B12)
        var topBars = KataBarNotationParser.ParseBarList(accessor.GetText(11, 2));
        var botBars = KataBarNotationParser.ParseBarList(accessor.GetText(12, 2));
        var topContinuous = topBars.Count > 0 ? topBars[0] : KataBarItem.Empty;
        var botContinuous = botBars.Count > 0 ? botBars[0] : KataBarItem.Empty;

        // 5. Global Side Bars (G4, G5)
        double sideDia = accessor.GetDouble("G4") ?? 0.0;
        // G5 counts the layers; a negative count keeps the layers and drops their C ties.
        int g5 = accessor.GetInt("G5") ?? 0;
        int sideLayers = Math.Abs(g5);
        var globalSideBars = new List<KataBarItem>();
        if (sideDia > 0.0 && sideLayers > 0)
        {
            for (int l = 1; l <= sideLayers; l++)
            {
                // Each layer has 2 bars (one on each face)
                globalSideBars.Add(new KataBarItem(2, sideDia, l, 0.0, $"2f{sideDia:0}"));
            }
        }

        // 6. Supports and Spans (Columns C..BZ)
        var supports = new List<KataSupportRebarSpec>();
        var spans = new List<KataSpanRebarSpec>();

        int supportIdx = 0;
        int spanIdx = 0;

        // Kata's own save/load macros walk row 11 from C and stop at its first empty cell; the "Cột"/"Nhịp"
        // captions of row 10 run across the whole template, so they cannot end the list.
        for (int col = FirstDataColumn; col <= MaxDataColumn; col++)
        {
            string? headerTag = accessor.GetText(10, col)?.Trim();
            if (string.IsNullOrEmpty(accessor.GetText(11, col)?.Trim()))
                break;

            // Determine if column is Support or Span:
            // In Kata, odd column index (C=3, E=5, G=7...) is Support; even (D=4, F=6...) is Span.
            // If headerTag is explicitly "Cột" or "Nhịp", respect that.
            bool isSupport = (col % 2 != 0);
            if (headerTag is not null)
            {
                if (headerTag.IndexOf("cột", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    headerTag.IndexOf("cot", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isSupport = true;
                }
                else if (headerTag.IndexOf("nhịp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         headerTag.IndexOf("nhip", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isSupport = false;
                }
            }

            if (isSupport)
            {
                var supp = ParseSupport(accessor, col, supportIdx++, notes);
                supports.Add(supp);
            }
            else
            {
                var span = ParseSpan(accessor, col, spanIdx++, globalStirrup, notes);
                // Row 21 of a span is its soffit step from B5's soffit, the top staying level: depth = B5 − step.
                if (height > 0.0) span = span with { Depth = height - span.SoffitDrop };
                spans.Add(span);
            }
        }

        return new KataBeamRebarSpec
        {
            BeamName = beamName,
            BeamCount = beamCount,
            Width = width,
            Height = height,
            SlabThickness = slabThickness,
            AxisGridName = axisGridName,
            AxisOffset = axisOffset,
            LevelElevation = levelElevation,
            TensionLapMultiplier = tensionLap,
            CompressionLapMultiplier = compLap,
            CurtailedExtension = curtailedExtension,
            CurtailedExtensionText = g1Text,
            TopCutoffRatioLayer1 = topCutoffL1,
            TopCutoffRatioLayer2 = topCutoffL2,
            CutoffOriginLayer1 = originL1,
            CutoffOriginLayer2 = originL2,
            CoverMain = coverMain,
            CoverStirrup = coverStirrup,
            TopContinuous = topContinuous,
            BottomContinuous = botContinuous,
            TopMainItems = topBars,
            BottomMainItems = botBars,
            DetailingNotes = notes,
            GlobalStirrup = globalStirrup,
            GlobalSideBars = globalSideBars,
            SideBarTies = g5 >= 0,
            Supports = supports,
            Spans = spans
        };
    }

    private static KataSupportRebarSpec ParseSupport(IKataDamCellAccessor accessor, int col, int supportIndex, List<KataCellNote> notes)
    {
        string? row11 = accessor.GetText(11, col);
        var (width, beamDepth) = KataBarNotationParser.ParseSupportDimension(row11);
        string supportSection = (row11 != null && (row11.Contains("x") || row11.Contains("*") || row11.Contains("/")))
            ? row11
            : "";

        var topL1 = KataBarNotationParser.ParseBarList(accessor.GetText(13, col), defaultLayer: 1);
        var topL2 = KataBarNotationParser.ParseBarList(accessor.GetText(14, col), defaultLayer: 2);
        var topL3 = KataBarNotationParser.ParseBarList(accessor.GetText(15, col), defaultLayer: 3);
        var topL4 = KataBarNotationParser.ParseBarList(accessor.GetText(16, col), defaultLayer: 4);
        var sides = new[]
        {
            KataBarNotationParser.ParseSides(accessor.GetText(13, col), 1),
            KataBarNotationParser.ParseSides(accessor.GetText(14, col), 2),
            KataBarNotationParser.ParseSides(accessor.GetText(15, col), 3),
            KataBarNotationParser.ParseSides(accessor.GetText(16, col), 4)
        };

        var (upperW, upperOffset) = KataBarNotationParser.ParsePair(accessor.GetText(19, col));
        double crossingW = accessor.GetDouble(20, col) ?? 0.0;
        double crossingOffset = accessor.GetDouble(21, col) ?? 0.0;
        string gridName = accessor.GetText(22, col) ?? "";
        double gridOffset = accessor.GetDouble(23, col) ?? 0.0;
        Note(accessor, notes, 24, col, "đai chống xoắn / đai gia cường tại gối");

        return new KataSupportRebarSpec
        {
            SupportIndex = supportIndex,
            SheetColumn = col,
            ColumnWidth = width,
            SupportSection = supportSection,
            BeamDepth = supportSection.Length > 0 ? beamDepth : 0.0,
            GridName = gridName,
            GridOffset = gridOffset,
            UpperColumnWidth = upperW,
            UpperColumnOffset = upperOffset,
            CrossingBeamWidth = crossingW,
            CrossingBeamOffset = crossingOffset,
            TopExtraLayer1 = topL1,
            TopExtraLayer2 = topL2,
            TopExtraLayer3 = topL3,
            TopExtraLayer4 = topL4,
            TopExtraSides = sides
        };
    }

    private static KataSpanRebarSpec ParseSpan(
        IKataDamCellAccessor accessor,
        int col,
        int spanIndex,
        KataStirrupSpec globalStirrup,
        List<KataCellNote> notes)
    {
        double length = accessor.GetDouble(11, col) ?? 0.0;

        // In Kata: Row 18 is Bottom Extra Layer 1, Row 17 is Bottom Extra Layer 2!
        string botText1 = accessor.GetText(18, col)?.Trim() ?? "";
        string botText2 = accessor.GetText(17, col)?.Trim() ?? "";
        var botL1 = KataBarNotationParser.ParseBarList(botText1, defaultLayer: 1);
        var botL2 = KataBarNotationParser.ParseBarList(botText2, defaultLayer: 2);

        var (topDrop, topDropBars) = KataBarNotationParser.ParseOffsetAndBars(accessor.GetText(19, col));
        var sideBars = KataBarNotationParser.ParseBarList(accessor.GetText(20, col), allowZeroCount: true);
        var (soffitDrop, soffitDropBars) = KataBarNotationParser.ParseOffsetAndBars(accessor.GetText(21, col));

        string? stirrupOverrideText = accessor.GetText(22, col);
        KataStirrupSpec? stirrupOverride = null;
        if (!string.IsNullOrWhiteSpace(stirrupOverrideText))
        {
            var (sDense, sMid, sEnd) = KataBarNotationParser.ParseStirrupSpacing(
                stirrupOverrideText,
                globalStirrup.SupportSpacing,
                globalStirrup.MidspanSpacing);

            stirrupOverride = globalStirrup with
            {
                SupportSpacing = sDense,
                MidspanSpacing = sMid,
                EndSupportSpacing = sEnd
            };
        }

        Note(accessor, notes, 23, col, "đai gia cường của nhịp");
        var innerStirrups = KataStirrupSectionParser.ParsePair(accessor, col - 1);

        return new KataSpanRebarSpec
        {
            SpanIndex = spanIndex,
            SheetColumn = col,
            Length = length,
            BottomExtraLayer1 = botL1,
            BottomExtraLayer2 = botL2,
            BottomExtraLayer1Text = botText1,
            BottomExtraLayer2Text = botText2,
            SideBars = sideBars,
            StirrupOverride = stirrupOverride,
            InnerStirrups = innerStirrups,
            TopDrop = topDrop,
            SoffitDrop = soffitDrop,
            TopDropBars = topDropBars,
            SoffitDropBars = soffitDropBars
        };
    }

    /// <summary>Cell J7: one spacing, "a500" or "500"; anything else (empty, "a100/200") is null.</summary>
    internal static double? ParseTieSpacing(string text)
    {
        string clean = text.Trim().TrimStart('a', 'A', '@').Trim();
        return double.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)
            && v > 0.0 && !double.IsInfinity(v)
            ? v
            : null;
    }

    internal static void Note(IKataDamCellAccessor accessor, List<KataCellNote> notes, int row, int col, string meaning)
    {
        string? text = accessor.GetText(row, col)?.Trim();
        if (!string.IsNullOrEmpty(text))
            notes.Add(new KataCellNote(KataDamCellAccessorExtensions.ToAddress(row, col), text!, meaning));
    }
}
