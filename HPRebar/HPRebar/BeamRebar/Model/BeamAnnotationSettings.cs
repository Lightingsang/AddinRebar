using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;
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

    /// <summary>A copy for one run: the document types found when the tool opened, the names and counts of the Views tab.</summary>
    public BeamAnnotationSettings ForRun(BeamViewOptions views) => new()
    {
        DetailTemplate = DetailTemplate,
        SectionTemplate = SectionTemplate,
        DimensionType = DimensionType,
        TextNoteType = TextNoteType,
        DimensionOffsetH = DimensionOffsetH,
        DimensionOffsetV = DimensionOffsetV,
        TableOffset = TableOffset,
        ViewMargin = ViewMargin,
        DetailViewName = views.DetailViewName,
        SectionPrefix = views.SectionPrefix,
        SectionsPerSpan = views.SectionsPerSpan,
        ElevationScale = views.ElevationScale
    };

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

        var structural = templates.FirstOrDefault(v => 
            v.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE)?.AsValueString() == "Structural")
            ?? templates.FirstOrDefault();

        var dimensionTypes = new FilteredElementCollector(document)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .ToList();

        var textTypes = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfClass(typeof(TextNoteType))
            .Cast<ElementType>()
            .ToList();

        return new BeamAnnotationSettings
        {
            DetailTemplate = structural,
            SectionTemplate = structural,
            DimensionType = dimensionTypes.FirstOrDefault(d => d.FamilyName == "Linear Dimension Style") 
                            ?? dimensionTypes.FirstOrDefault(),
            TextNoteType = textTypes.FirstOrDefault(),
            DimensionOffsetH = maxSizeMm * 0.5,
            DimensionOffsetV = maxSizeMm * 0.5,
            TableOffset = maxSizeMm * 0.5,
            ViewMargin = maxSizeMm * 0.8
        };
    }

    public string SectionViewName(int spanIndex, int cutIndex) => 
        $"{DetailViewName} - Span {spanIndex} - {SectionPrefix} {cutIndex}";
}
