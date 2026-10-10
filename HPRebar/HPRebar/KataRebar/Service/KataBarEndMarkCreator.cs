using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Draws the cut marks of a run as detail lines on its long section, in the line style of Kata's bars
/// ("kata_thep chu", created red and weight 3 when the model lacks it), and deletes the marks of the previous run.
/// </summary>
internal static class KataBarEndMarkCreator
{
    /// <summary>Deletes the marks Kata Rebar drew for the run; call inside an open transaction.</summary>
    public static int DeletePrevious(Document doc, string runKey)
    {
        var ids = KataDraftingStorage.Find<CurveElement>(doc, runKey, KataDraftingStorage.MarkKind).Select(c => c.Id).ToList();
        if (ids.Count > 0) doc.Delete(ids);
        return ids.Count;
    }

    /// <summary>
    /// Draws one detail line per mark on <paramref name="view"/>, on its plane at local y <paramref name="planeY"/>;
    /// call inside an open transaction.
    /// </summary>
    public static int Draw(Document doc, RevitView view, string runKey, IReadOnlyList<KataBarEndMark> marks, PointMapper mapper, double planeY)
    {
        var style = KataLineStyles.Bar(doc);
        int drawn = 0;
        foreach (var mark in marks)
        {
            var (tipX, tipZ) = mark.Tip(view.Scale);
            var line = Line.CreateBound(mapper.ToXyz(new Point3(mark.X, planeY, mark.Z)), mapper.ToXyz(new Point3(tipX, planeY, tipZ)));
            var curve = doc.Create.NewDetailCurve(view, line);
            curve.LineStyle = style;
            KataDraftingStorage.Write(curve, runKey, KataDraftingStorage.MarkKind);
            drawn++;
        }

        return drawn;
    }
}
