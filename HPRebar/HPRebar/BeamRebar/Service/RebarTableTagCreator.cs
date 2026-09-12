using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Draws tabular reinforcement schedule blocks beside beam cross-section views
/// and places native rebar tags on elevation views.
/// </summary>
public static class RebarTableTagCreator
{
    private const double RowHeightFactor = 12.0;
    private const double LabelColumns = 6.0;
    private const double ValueColumns = 6.0;

    public static int Create(
        Document document,
        ViewSection view,
        BeamSpan span,
        int spanIndex,
        int cutIndex,
        BeamRebarSpec spec,
        BeamAnnotationSettings settings)
    {
        if (settings.TextNoteType is null)
        {
            Log.Warning("The document has no text note type; bar table was skipped.");
            return 0;
        }

        try
        {
            double rowHeight = CalculateRowHeight(document, settings);
            XYZ origin = CalculateTableOrigin(view, span, settings);
            var rows = BuildRows(span, cutIndex, spec);

            XYZ rightDir = view.RightDirection;
            XYZ upDir = view.UpDirection;

            for (int r = 0; r < rows.Count; r++)
            {
                XYZ rowTopLeft = origin - (r * rowHeight) * upDir;
                WriteRow(document, view, settings, rowTopLeft, rowHeight, rows[r].Label, rows[r].Value, rightDir, upDir);
            }

            return rows.Count;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not write rebar table in {View}; skipped.", view.Name);
            return 0;
        }
    }

    public static void TagRebarOnElevation(
        Document document,
        ViewSection elevationView,
        Rebar rebar,
        XYZ headPosition)
    {
        try
        {
            IndependentTag.Create(
                document,
                elevationView.Id,
                new Reference(rebar),
                addLeader: true,
                TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal,
                headPosition);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not place rebar tag on rebar {RebarId}; skipping.", rebar.Id);
        }
    }

    private static IReadOnlyList<(string Label, string Value)> BuildRows(
        BeamSpan span, 
        int cutIndex, 
        BeamRebarSpec spec)
    {
        var list = new List<(string, string)>();
        list.Add(("Section", $"{span.Width:0}x{span.Height:0}"));
        list.Add(("Main Top", $"{spec.MainBars.TopCount}-d{spec.MainBars.TopDiameter:0}"));
        list.Add(("Main Bot", $"{spec.MainBars.BottomCount}-d{spec.MainBars.BottomDiameter:0}"));

        if (cutIndex == 0 && spec.AdditionalBars.SupportTopBars.Count > 0)
        {
            var topConfig = spec.AdditionalBars.SupportTopBars[0];
            list.Add(("Add Top", $"{topConfig.Layer1Count}-d{topConfig.Layer1Diameter:0}"));
        }
        else if (cutIndex == 1 && spec.AdditionalBars.SpanBottomBars.Count > 0)
        {
            var botConfig = spec.AdditionalBars.SpanBottomBars[0];
            list.Add(("Add Bot", $"{botConfig.Layer1Count}-d{botConfig.Layer1Diameter:0}"));
        }

        if (span.Height >= spec.SideBars.DepthThreshold && spec.SideBars.AutoSkinBars)
        {
            list.Add(("Side Bars", $"2x-d{spec.SideBars.Diameter:0}"));
        }

        double spacing = cutIndex == 1 ? spec.Stirrups.SpacingSparse : spec.Stirrups.SpacingDense;
        list.Add(("Stirrup", $"d{spec.Stirrups.Diameter:0} @ {spacing:0}"));

        return list;
    }

    private static void WriteRow(
        Document document,
        ViewSection view,
        BeamAnnotationSettings settings,
        XYZ topLeft,
        double rowHeight,
        string label,
        string value,
        XYZ rightDir,
        XYZ upDir)
    {
        double labelWidth = LabelColumns * rowHeight;
        double valueWidth = ValueColumns * rowHeight;

        XYZ topMid = topLeft + labelWidth * rightDir;
        XYZ topRight = topMid + valueWidth * rightDir;
        XYZ bottomLeft = topLeft - rowHeight * upDir;
        XYZ bottomMid = topMid - rowHeight * upDir;
        XYZ bottomRight = topRight - rowHeight * upDir;

        // Draw 5 bounding line segments for the 2-column box
        var lines = new[]
        {
            Line.CreateBound(topLeft, topRight),
            Line.CreateBound(bottomLeft, bottomRight),
            Line.CreateBound(topLeft, bottomLeft),
            Line.CreateBound(topMid, bottomMid),
            Line.CreateBound(topRight, bottomRight)
        };

        foreach (var line in lines)
        {
            document.Create.NewDetailCurve(view, line);
        }

        TextNote.Create(document, view.Id, topLeft, label, settings.TextNoteType!.Id);
        TextNote.Create(document, view.Id, topMid, value, settings.TextNoteType.Id);
    }

    private static double CalculateRowHeight(Document document, BeamAnnotationSettings settings)
    {
        double textSize = settings.TextNoteType?.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0.0;
        double baseHeight = textSize > 0 ? RowHeightFactor * textSize : RevitUnits.MmToFt(60.0);

        int scale = settings.SectionTemplate?.get_Parameter(BuiltInParameter.VIEW_SCALE)?.AsInteger() ?? 0;
        return scale > 0 ? baseHeight * (100.0 / scale) : baseHeight;
    }

    private static XYZ CalculateTableOrigin(ViewSection view, BeamSpan span, BeamAnnotationSettings settings)
    {
        double widthFt = RevitUnits.MmToFt(span.Width);
        double heightFt = RevitUnits.MmToFt(span.Height);
        double offsetFt = RevitUnits.MmToFt(settings.TableOffset);

        // Position clear of right face of section, at top elevation of section
        return view.Origin 
            + (widthFt * 0.5 + offsetFt) * view.RightDirection 
            + (heightFt * 0.5) * view.UpDirection;
    }
}
