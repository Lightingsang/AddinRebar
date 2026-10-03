using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;
using Serilog;
// The feature's own View namespace shadows Autodesk.Revit.DB.View inside HPRebar.BeamRebar,
// so the Revit type is aliased wherever it is named directly.
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Settings for automated drawing generation, dimensioning, and tagging.
/// </summary>
public sealed class BeamAnnotationSettings
{
    public RevitView? DetailTemplate { get; set; }
    public RevitView? SectionTemplate { get; set; }
    public DimensionType? DimensionType { get; set; }
    public ElementType? TextNoteType { get; set; }

    public double DimensionOffsetH { get; set; }
    public double DimensionOffsetV { get; set; }
    public double TableOffset { get; set; }
    public double ViewMargin { get; set; }

    public string DetailViewName { get; set; } = BeamViewOptions.Default.DetailViewName;
    public string SectionPrefix { get; set; } = BeamViewOptions.Default.SectionPrefix;
    public int SectionsPerSpan { get; set; } = BeamViewOptions.Default.SectionsPerSpan;
    public int ElevationScale { get; set; } = BeamViewOptions.Default.ElevationScale;

    // Ids, not elements, survive the modeless window: the user may delete, undo or edit these
    // before Run, which invalidates a held wrapper while the element itself may still exist.
    private ElementId? _templateId;
    private ElementId? _dimensionTypeId;
    private ElementId? _textNoteTypeId;

    /// <summary>
    ///     A copy for one run: the document types found when the tool opened, fetched again from the document
    ///     (one deleted meanwhile is left out), and the names and counts of the Views tab.
    /// </summary>
    public BeamAnnotationSettings ForRun(Document document, BeamViewOptions views)
    {
        var template = Resolve<RevitView>(document, _templateId, "view template");
        return new BeamAnnotationSettings
        {
            DetailTemplate = template,
            SectionTemplate = template,
            DimensionType = Resolve<DimensionType>(document, _dimensionTypeId, "dimension type"),
            TextNoteType = Resolve<ElementType>(document, _textNoteTypeId, "text note type"),
            DimensionOffsetH = DimensionOffsetH,
            DimensionOffsetV = DimensionOffsetV,
            TableOffset = TableOffset,
            ViewMargin = ViewMargin,
            DetailViewName = views.DetailViewName,
            SectionPrefix = views.SectionPrefix,
            SectionsPerSpan = views.SectionsPerSpan,
            ElevationScale = views.ElevationScale
        };
    }

    public static BeamAnnotationSettings Load(Document document, BeamContinuousStack stack)
    {
        double maxSizeMm = stack.MaxHeight;
        if (maxSizeMm <= 0.0) maxSizeMm = 600.0;

        var templates = new FilteredElementCollector(document)
            .OfClass(typeof(RevitView))
            .WhereElementIsNotElementType()
            .Cast<RevitView>()
            .Where(v => v.IsTemplate)
            .ToList();

        // Every view the run creates is a ViewSection; Revit applies section, detail and elevation templates
        // to it, and a template of another view type would make the ViewTemplateId setter throw.
        var sectionTemplates = templates
            .Where(v => v.ViewType is ViewType.Section or ViewType.Detail or ViewType.Elevation)
            .ToList();

        var structural = sectionTemplates.FirstOrDefault(IsStructural) ?? sectionTemplates.FirstOrDefault();

        var dimensionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .ToList();

        var textTypes = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfClass(typeof(TextNoteType))
            .Cast<ElementType>()
            .ToList();

        var dimensionType = dimensionTypes.FirstOrDefault(d => d.FamilyName == "Linear Dimension Style")
                            ?? dimensionTypes.FirstOrDefault();

        return new BeamAnnotationSettings
        {
            _templateId = structural?.Id,
            _dimensionTypeId = dimensionType?.Id,
            _textNoteTypeId = textTypes.FirstOrDefault()?.Id,
            DimensionOffsetH = maxSizeMm * 0.5,
            DimensionOffsetV = maxSizeMm * 0.5,
            TableOffset = maxSizeMm * 0.5,
            ViewMargin = maxSizeMm * 0.8
        };
    }

    public string SectionViewName(int spanIndex, int cutIndex) => 
        $"{DetailViewName} - Span {spanIndex} - {SectionPrefix} {cutIndex}";

    // The discipline is compared by value: its display string is localized.
    private static bool IsStructural(RevitView template) =>
        template.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE)?.AsInteger() == (int)ViewDiscipline.Structural;

    private static T? Resolve<T>(Document document, ElementId? id, string role) where T : Element
    {
        if (id is null)
        {
            return null;
        }

        if (document.GetElement(id) is T { IsValidObject: true } element)
        {
            return element;
        }

        Log.Warning("The {Role} chosen when Beam Rebar opened is no longer in the document; the run continues without it.", role);
        return null;
    }
}
