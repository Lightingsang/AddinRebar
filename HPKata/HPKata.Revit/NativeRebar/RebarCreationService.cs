using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPKata.Revit.NativeRebar;

/// <summary>
/// Service managing the creation of native 3D structural rebar elements in Revit 2026.
/// Employs modern Revit 2026 BarTerminationsData API.
/// </summary>
public static class RebarCreationService
{
    /// <summary>
    /// Creates a native standard rebar element from centerline curves and assigns partition, mark, and comments.
    /// </summary>
    public static Rebar CreateRebar(
        Document doc,
        Element host,
        RebarBarType barType,
        XYZ normal,
        IList<Curve> curves,
        string partition = "CK",
        string mark = "1",
        string comments = "HPKata",
        RebarStyle style = RebarStyle.Standard)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (host == null) throw new ArgumentNullException(nameof(host));
        if (barType == null) throw new ArgumentNullException(nameof(barType));
        if (curves == null || curves.Count == 0) throw new ArgumentException("Curves cannot be empty.", nameof(curves));

        var terminations = new BarTerminationsData(doc);
        var rebar = Rebar.CreateFromCurves(
            doc,
            style,
            barType,
            host,
            normal,
            curves,
            terminations,
            true,
            true);

        if (rebar == null)
        {
            throw new InvalidOperationException($"Revit could not create rebar of type '{barType.Name}'.");
        }

        AssignParameters(rebar, partition, mark, comments);
        return rebar;
    }

    private static void AssignParameters(Rebar rebar, string partition, string mark, string comments)
    {
        if (!string.IsNullOrWhiteSpace(partition))
        {
            rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.Set(partition);
        }

        if (!string.IsNullOrWhiteSpace(mark))
        {
            var pMark = rebar.get_Parameter(BuiltInParameter.REBAR_NUMBER)
                        ?? rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            pMark?.Set(mark);
        }

        if (!string.IsNullOrWhiteSpace(comments))
        {
            rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(comments);
        }
    }
}
