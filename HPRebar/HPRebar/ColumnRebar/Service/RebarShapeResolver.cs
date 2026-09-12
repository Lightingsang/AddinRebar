using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Finds the rebar shape families the tool bends its ties from. The names are fixed: the original tool
///     offered a picker but forced the choice back to these, so the picker never did anything.
/// </summary>
public sealed class RebarShapeResolver
{
    /// <summary>Closed rectangular tie.</summary>
    public const string RectangularTie = "M_T1";

    /// <summary>Closed circular tie.</summary>
    public const string CircularTie = "M_T3";

    private readonly IReadOnlyDictionary<string, RebarShape> _shapes;

    private RebarShapeResolver(IReadOnlyDictionary<string, RebarShape> shapes) => _shapes = shapes;

    public static RebarShapeResolver Load(Document document)
    {
        var shapes = new FilteredElementCollector(document)
            .OfClass(typeof(RebarShape))
            .Cast<RebarShape>()
            .GroupBy(shape => shape.Name)
            .ToDictionary(group => group.Key, group => group.First());

        return new RebarShapeResolver(shapes);
    }

    /// <summary>Shape for the main perimeter tie of a section.</summary>
    public RebarShape? MainTie(SectionShape section) =>
        Find(section == SectionShape.Rectangle ? RectangularTie : CircularTie);

    /// <summary>
    ///     Shape for an intermediate cross-tie. The leg style follows the tie type the user picked;
    ///     an unknown type falls back to the plain double-hook shape, as the original tool did.
    /// </summary>
    public RebarShape? CrossTie(int tieType)
    {
        var name = tieType switch
        {
            1 => "M_T10B",
            2 => "M_T10",
            3 => "M_T10C",
            _ => "M_T10"
        };

        return Find(name) ?? Find("M_T10");
    }

    /// <summary>
    ///     Checks the shapes a stack needs are loaded, before any transaction is opened, so a missing family
    ///     is reported as a dialog rather than a rollback halfway through.
    /// </summary>
    public ValidationResult Require(SectionShape section, bool needsCrossTies)
    {
        if (MainTie(section) is null)
        {
            return ValidationResult.Fail(section == SectionShape.Rectangle ? 20 : 21);
        }

        return needsCrossTies && CrossTie(1) is null ? ValidationResult.Fail(22) : ValidationResult.Ok;
    }

    private RebarShape? Find(string name) => _shapes.TryGetValue(name, out var shape) ? shape : null;
}
