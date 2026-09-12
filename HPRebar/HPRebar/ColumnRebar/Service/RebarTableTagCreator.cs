using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;
using Serilog;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Writes the little table beside each cross-section listing what was put in that segment — bar count
///     and size, tie size and spacing, cross-ties. Built from detail lines and text notes rather than
///     rebar tags, which is what the original tool did and what keeps the layout predictable.
/// </summary>
public static class RebarTableTagCreator
{
    /// <summary>Row height, as a multiple of the text size.</summary>
    private const double RowHeightFactor = 12;

    /// <summary>Width of the label column, as a multiple of the row height.</summary>
    private const double LabelColumns = 5;

    /// <summary>Width of the value column, as a multiple of the row height.</summary>
    private const double ValueColumns = 5;

    public static int Create(
        Document document,
        ViewSection view,
        ColumnStack stack,
        int index,
        ColumnRebarSpec spec,
        AnnotationSettings settings)
    {
        if (settings.TextNoteType is null)
        {
            Log.Warning("The document has no text note type; the bar table was skipped");

            return 0;
        }

        try
        {
            // The table is laid out along the column's own axes, the same ones the section view was cut
            // along, so it stays beside the section instead of drifting off it when the column is rotated
            // in plan. A column square to the project gives the world axes back unchanged.
            var axes = ColumnPlanAxes.Of(stack.Faces[index], stack.Sections[index]);

            var rowHeight = RowHeight(document, settings);
            var origin = TableOrigin(view, stack, index, settings, axes);
            var rows = Rows(stack.Sections[index], spec);

            for (var row = 0; row < rows.Count; row++)
            {
                var top = origin - row * rowHeight * axes.North;

                WriteRow(document, view, settings, top, rowHeight, rows[row].Label, rows[row].Value, axes);
            }

            return rows.Count;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not write the bar table in {View}; it was skipped", view.Name);

            return 0;
        }
    }

    /// <summary>What the table says, one row per kind of bar actually present.</summary>
    private static IReadOnlyList<(string Label, string Value)> Rows(ColumnSection section, ColumnRebarSpec spec)
    {
        var culture = CultureInfo.InvariantCulture;
        var rows = new List<(string, string)>
        {
            ("Bar", $"{spec.Layout.BarCount}-{spec.MainBarType.Name}"),
            ("Stirrup", $"{spec.StirrupBarType.Name} @ {Spacing(spec.Stirrups).ToString("0", culture)}")
        };

        if (spec.Ties.AddH)
        {
            var count = spec.Ties.TypeH == 0 ? string.Empty : spec.Ties.NH + " ";

            rows.Add(("Add-Horizontal", $"{count}{spec.TieBarType.Name} @ {Spacing(spec.Stirrups).ToString("0", culture)}"));
        }

        if (spec.Ties.AddV)
        {
            var count = spec.Ties.TypeV == 0 ? string.Empty : spec.Ties.NV + " ";

            rows.Add(("Add-Vertical", $"{count}{spec.TieBarType.Name} @ {Spacing(spec.Stirrups).ToString("0", culture)}"));
        }

        return rows;
    }

    /// <summary>The spacing worth quoting: the even one, or the dense end spacing for a zoned layout.</summary>
    private static double Spacing(StirrupSpec spec) => spec.TypeDis == 0 ? spec.S : spec.S1;

    /// <summary>One row: a bordered two-cell box with its label and value.</summary>
    private static void WriteRow(
        Document document,
        ViewSection view,
        AnnotationSettings settings,
        XYZ topLeft,
        double rowHeight,
        string label,
        string value,
        (XYZ East, XYZ North) axes)
    {
        var labelWidth = LabelColumns * rowHeight;
        var valueWidth = ValueColumns * rowHeight;

        var topMid = topLeft + labelWidth * axes.East;
        var topRight = topMid + valueWidth * axes.East;
        var bottomLeft = topLeft - rowHeight * axes.North;
        var bottomMid = topMid - rowHeight * axes.North;
        var bottomRight = topRight - rowHeight * axes.North;

        foreach (var (from, to) in new[]
                 {
                     (topLeft, topRight),
                     (bottomLeft, bottomRight),
                     (topLeft, bottomLeft),
                     (topMid, bottomMid),
                     (topRight, bottomRight)
                 })
        {
            document.Create.NewDetailCurve(view, Line.CreateBound(from, to));
        }

        TextNote.Create(document, view.Id, topLeft, label, settings.TextNoteType!.Id);
        TextNote.Create(document, view.Id, topMid, value, settings.TextNoteType.Id);
    }

    /// <summary>Row height in feet, scaled so the table reads the same at any view scale.</summary>
    private static double RowHeight(Document document, AnnotationSettings settings)
    {
        var textSize = settings.TextNoteType?.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0;
        var height = textSize > 0 ? RowHeightFactor * textSize : RevitUnits.MmToFt(60);

        var scale = settings.SectionTemplate?.get_Parameter(BuiltInParameter.VIEW_SCALE)?.AsInteger() ?? 0;

        return scale > 0 ? height * (100.0 / scale) : height;
    }

    /// <summary>Top-left corner of the table: clear of the section's east face, at the view's cut height.</summary>
    private static XYZ TableOrigin(
        ViewSection view,
        ColumnStack stack,
        int index,
        AnnotationSettings settings,
        (XYZ East, XYZ North) axes)
    {
        var faces = stack.Faces[index];
        var section = stack.Sections[index];

        XYZ corner;

        if (section.Shape == SectionShape.Rectangle)
        {
            var point = ColumnSolidFaceReader.ProjectToPlane(faces.East!.Origin, faces.North!);
            corner = new XYZ(point.X, point.Y, view.Origin.Z);
        }
        else
        {
            var point = faces.LocationPoint!;
            corner = new XYZ(point.X, point.Y, view.Origin.Z)
                     + RevitUnits.MmToFt(section.D) * 0.5 * axes.North;
        }

        return corner + RevitUnits.MmToFt(settings.TableOffset) * axes.East;
    }
}
