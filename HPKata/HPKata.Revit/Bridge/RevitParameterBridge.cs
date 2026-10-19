using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Units;

namespace HPKata.Revit.Bridge;

/// <summary>
/// Bridge mapping parameters between native Revit Rebar elements / DTOs and HPKata domain models.
/// Enforces single-bar cutting length mapping (never total length) and unit quarantine.
/// </summary>
public static class RevitParameterBridge
{
    private static readonly string[] ShapeParamNames = ["A", "B", "C", "D", "E", "F"];

    /// <summary>
    /// Converts a headless RevitRebarDto into an HPKata core RebarItem.
    /// </summary>
    public static RebarItem ToRebarItem(RevitRebarDto dto)
    {
        int dia = (int)Math.Round(dto.DiameterMm);
        var item = new RebarItem
        {
            ComponentMark = string.IsNullOrWhiteSpace(dto.Partition) ? "CK" : dto.Partition.Trim(),
            BarMark = string.IsNullOrWhiteSpace(dto.Mark) ? "1" : dto.Mark.Trim(),
            ShapeCode = RevitShapeAdapter.ResolveKataShape(dto.ShapeName, dto.ParametersMm),
            DiameterMm = dia,
            CountPerMember = Math.Max(1, dto.Quantity),
            MemberCount = 1,
            ParametersMm = dto.ParametersMm ?? [],
            CuttingLengthMm = dto.SingleBarLengthMm,
            Notes = dto.Comments ?? string.Empty,
            UnitMassKgPerM = RebarCalculator.GetUnitMass(dia)
        };
        return item;
    }

    /// <summary>
    /// Converts an HPKata core RebarItem into a headless RevitRebarDto.
    /// </summary>
    public static RevitRebarDto ToRebarDto(RebarItem item, long elementId = 0)
    {
        return new RevitRebarDto
        {
            ElementId = elementId,
            Partition = item.ComponentMark,
            Mark = item.BarMark,
            Quantity = item.CountPerMember,
            DiameterMm = item.DiameterMm,
            SingleBarLengthMm = item.CuttingLengthMm,
            ShapeName = RevitShapeAdapter.ResolveRevitShape(item.ShapeCode),
            ParametersMm = item.ParametersMm ?? [],
            Comments = item.Notes
        };
    }

    /// <summary>
    /// Extracts a headless RevitRebarDto from a native live Revit Rebar element.
    /// </summary>
    public static RevitRebarDto ExtractFromRevitRebar(Rebar rebar, Document doc)
    {
        string partition = rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString() ?? string.Empty;
        string mark = rebar.get_Parameter(BuiltInParameter.REBAR_NUMBER)?.AsString()
            ?? rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
        int qty = Math.Max(1, rebar.Quantity);
        double singleLengthFt = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)?.AsDouble()
            ?? (rebar.TotalLength / qty);
        double singleLengthMm = RevitUnitConverter.FeetToMm(singleLengthFt);
        double diameterMm = doc.GetElement(rebar.GetTypeId()) is RebarBarType barType
            ? RevitUnitConverter.FeetToMm(barType.BarNominalDiameter) : 0.0;
        bool isFreeform = rebar.IsRebarFreeForm();
        var (shapeName, parametersMm) = ResolveShapeAndParameters(rebar, doc, isFreeform);
        string comments = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty;

        return new RevitRebarDto
        {
            ElementId = rebar.Id.Value,
            UniqueId = rebar.UniqueId,
            Partition = partition,
            Mark = mark,
            Quantity = qty,
            DiameterMm = diameterMm,
            SingleBarLengthMm = singleLengthMm,
            ShapeName = shapeName,
            ParametersMm = parametersMm,
            Comments = comments,
            IsFreeform = isFreeform
        };
    }

    private static (string shapeName, double[] parametersMm) ResolveShapeAndParameters(
        Rebar rebar,
        Document doc,
        bool isFreeform)
    {
        if (!isFreeform && doc.GetElement(rebar.GetShapeId()) is RebarShape shape)
        {
            return (shape.Name, ExtractShapeParametersMm(rebar));
        }

        var curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
        var analyzed = RevitCurveAnalyzer.AnalyzeCurves(curves);
        return (analyzed.ShapeCode, analyzed.ParametersMm);
    }

    private static double[] ExtractShapeParametersMm(Rebar rebar)
    {
        var list = new List<double>();
        foreach (var name in ShapeParamNames)
        {
            var p = rebar.LookupParameter(name);
            if (p != null && p.HasValue)
            {
                list.Add(RevitUnitConverter.FeetToMm(p.AsDouble()));
            }
        }
        return list.ToArray();
    }
}
