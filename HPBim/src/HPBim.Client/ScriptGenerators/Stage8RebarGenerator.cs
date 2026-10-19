namespace HPBim.Client.ScriptGenerators;

using System.Linq;
using HPBim.Client.TypeResolver;
using HPBim.Core.Models;

/// <summary>
///     Generates Revit script for Stage 8: Creating native 3D structural Reinforcement (Rebar).
/// </summary>
public sealed class Stage8RebarGenerator : IStageScriptGenerator
{
    public int StageIndex => 8;
    public string StageName => "Rebars";

    public StageExecutionPayload Generate(BimModel model, StageGenerationOptions? options = null)
    {
        var rebars = model.Rebars.Select(r => new
        {
            id = r.Id,
            hostId = r.HostElementId,
            mark = r.BarMark,
            diameterMm = r.DiameterMm,
            grade = r.SteelGrade,
            shape = r.ShapeType.ToString(),
            lengthMm = r.LengthMm,
            qty = r.Quantity,
            spacingMm = r.SpacingMm,
            coverMm = r.CoverMm
        }).ToList();

        return new StageExecutionPayload
        {
            StageIndex = StageIndex,
            StageName = StageName,
            ScriptCode = GetScriptCode(),
            Args = new { rebars },
            Label = $"Stage 8: Generate {rebars.Count} Rebar groups"
        };
    }

    public static string GetScriptCode() => $$"""
        {{DynamicTypeScriptHelper.GetCommonHelpersScript()}}

        var rebarBarTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(Autodesk.Revit.DB.Structure.RebarBarType))
            .Cast<Autodesk.Revit.DB.Structure.RebarBarType>()
            .ToList();

        var hostCandidates = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .Where(e => e.Category != null && (
                e.Category.BuiltInCategory == BuiltInCategory.OST_StructuralColumns ||
                e.Category.BuiltInCategory == BuiltInCategory.OST_StructuralFraming))
            .ToList();

        var hostsByMark = new Dictionary<string, Element>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in hostCandidates)
        {
            string mark = h.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
            if (!string.IsNullOrEmpty(mark)) hostsByMark[mark] = h;
        }

        var rebarItems = args.List("rebars");
        int createdCount = 0;
        foreach (var rb in rebarItems)
        {
            if (rebarBarTypes.Count == 0 || hostCandidates.Count == 0) break;

            double diamMm = rb.Double("diameterMm", 16);
            double diamFt = Ft(diamMm);
            var barType = rebarBarTypes.OrderBy(b => Math.Abs(b.BarModelDiameter - diamFt)).FirstOrDefault() ?? rebarBarTypes[0];

            string hostId = rb.Str("hostId", string.Empty);
            Element host = null;
            if (!string.IsNullOrEmpty(hostId)) hostsByMark.TryGetValue(hostId, out host);
            host ??= hostCandidates.FirstOrDefault();
            if (host == null) continue;

            double lenFt = Math.Max(Ft(100), Ft(rb.Double("lengthMm", 3000)));
            var bbox = host.get_BoundingBox(null);
            var origin = bbox != null ? bbox.Min : XYZ.Zero;

            var p0 = new XYZ(origin.X + Ft(30), origin.Y + Ft(30), origin.Z);
            var p1 = new XYZ(p0.X, p0.Y, p0.Z + lenFt);
            if (p0.DistanceTo(p1) < 0.01) continue;

            var curves = new List<Curve> { Line.CreateBound(p0, p1) };
            var normal = XYZ.BasisX;

            try
            {
                var rebar = Autodesk.Revit.DB.Structure.Rebar.CreateFromCurves(
                    doc,
                    Autodesk.Revit.DB.Structure.RebarStyle.Standard,
                    barType,
                    null,
                    null,
                    host,
                    normal,
                    curves,
                    Autodesk.Revit.DB.Structure.RebarHookOrientation.Left,
                    Autodesk.Revit.DB.Structure.RebarHookOrientation.Left,
                    true,
                    true);

                if (rebar != null)
                {
                    int qty = rb.Int("qty", 1);
                    if (qty > 1)
                    {
                        double spacingFt = Ft(rb.Double("spacingMm", 150));
                        rebar.GetShapeDrivenAccessor()?.SetLayoutAsNumberWithSpacing(qty, spacingFt, true, true, true);
                    }
                    createdCount++;
                }
            }
            catch (Exception)
            {
                // Fallback gracefully if host lacks valid geometry cover or boundaries.
            }
        }

        return new { stage = 8, totalRebars = rebarItems.Count, createdRebars = createdCount };
        """;
}
