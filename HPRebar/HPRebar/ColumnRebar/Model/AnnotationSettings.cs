using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.ColumnRebar.Models;
// The feature's own View namespace shadows Autodesk.Revit.DB.View inside HPRebar.ColumnRebar,
// so the Revit type is aliased wherever it is named directly.
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     The document types the annotation pass draws with, plus the offsets that position what it draws.
///     Everything here is looked up once when the tool opens; a type the document does not have stays null
///     and the pass that needs it is skipped rather than failing the whole run.
/// </summary>
public sealed class AnnotationSettings
{
    /// <summary>View template applied to the two elevation views.</summary>
    public RevitView? DetailTemplate { get; set; }

    /// <summary>View template applied to each cross-section view.</summary>
    public RevitView? SectionTemplate { get; set; }

    public DimensionType? DimensionType { get; set; }

    public ElementType? TextNoteType { get; set; }

    /// <summary>How far the elevation dimensions stand off the column. Millimetres.</summary>
    public double DimensionOffsetH { get; set; }

    /// <summary>How far the section dimensions stand off the column. Millimetres.</summary>
    public double DimensionOffsetV { get; set; }

    /// <summary>How far the bar table sits to the side of the section. Millimetres.</summary>
    public double TableOffset { get; set; }

    /// <summary>Padding around the crop box of the elevation views. Millimetres.</summary>
    public double ViewMargin { get; set; }

    public string DetailViewName { get; set; } = ViewNaming.Default.DetailViewName;

    /// <summary>Appended to each cross-section view name.</summary>
    public string SectionSuffix { get; set; } = ViewNaming.Default.SectionSuffix;

    /// <summary>
    ///     Reads what the document offers and derives the offsets from the column size, matching the
    ///     defaults the original tool started from.
    /// </summary>
    public static AnnotationSettings Load(Document document, ColumnStack stack)
    {
        var first = stack.Sections[0];
        var size = first.Shape == SectionShape.Rectangle ? System.Math.Max(first.B, first.H) : first.D;

        var templates = new FilteredElementCollector(document)
            .OfClass(typeof(RevitView))
            .WhereElementIsNotElementType()
            .Cast<RevitView>()
            .Where(view => view.IsTemplate)
            .ToList();

        // A structural template if the document has one, otherwise whatever template exists.
        var structural = templates.FirstOrDefault(view =>
            view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE)?.AsValueString() == "Structural")
            ?? templates.FirstOrDefault();

        var dimensionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .ToList();

        return new AnnotationSettings
        {
            DetailTemplate = structural,
            SectionTemplate = structural,
            DimensionType = dimensionTypes.FirstOrDefault(type => type.FamilyName == "Linear Dimension Style")
                            ?? dimensionTypes.FirstOrDefault(),
            TextNoteType = new FilteredElementCollector(document)
                .WhereElementIsElementType()
                .OfClass(typeof(TextNoteType))
                .Cast<ElementType>()
                .FirstOrDefault(),
            DimensionOffsetH = size,
            DimensionOffsetV = size,
            TableOffset = size / 2,
            ViewMargin = size
        };
    }

    /// <summary>Names for the two elevation views.</summary>
    public (string X, string Y) DetailViewNames() => (DetailViewName + "X", DetailViewName + "Y");

    /// <summary>Name for the cross-section view of one column segment.</summary>
    public string SectionViewName(int columnNumber) => $"{DetailViewName} {columnNumber} {SectionSuffix}";
}
