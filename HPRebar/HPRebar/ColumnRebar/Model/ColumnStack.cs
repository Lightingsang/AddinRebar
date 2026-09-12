using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Autodesk.Revit.DB;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     A validated run of stacked columns, ready to reinforce. <see cref="Sections"/> and
///     <see cref="Faces"/> are index-aligned and ordered bottom to top.
/// </summary>
public sealed record ColumnStack
{
    public ColumnSectionStyle Style { get; init; }

    /// <summary>Pure numbers, millimetres, measured from <see cref="DatumFace"/>.</summary>
    public IReadOnlyList<ColumnSection> Sections { get; init; } = new List<ColumnSection>();

    public IReadOnlyList<ColumnFaces> Faces { get; init; } = new List<ColumnFaces>();

    /// <summary>
    ///     The face every vertical position is measured from: the top of whatever supports the bottom
    ///     column, falling back to the bottom column's own base.
    /// </summary>
    public PlanarFace DatumFace { get; init; } = null!;

    /// <summary>South face of the bottom column — the Y datum. Null on a circular stack.</summary>
    public PlanarFace? SouthDatum { get; init; }

    /// <summary>West face of the bottom column — the X datum. Null on a circular stack.</summary>
    public PlanarFace? WestDatum { get; init; }

    /// <summary>Insertion point of the bottom column — the plan datum for a circular stack.</summary>
    public XYZ? PointDatum { get; init; }

    /// <summary>Faces the dimension pass will hang witness lines off, bottom to top.</summary>
    public IReadOnlyList<PlanarFace> DimensionFaces { get; init; } = new List<PlanarFace>();

    /// <summary>Human-readable dump of the numbers, for the pre-UI smoke test.</summary>
    public string Summary()
    {
        var culture = CultureInfo.InvariantCulture;
        var text = new StringBuilder();

        text.AppendLine($"{Style} stack, {Sections.Count} segment(s). All values in mm.");

        foreach (var section in Sections)
        {
            var plan = section.Shape == SectionShape.Rectangle
                ? $"b={section.B.ToString("0.#", culture)} h={section.H.ToString("0.#", culture)}"
                : $"D={section.D.ToString("0.#", culture)}";

            text.AppendLine();
            text.AppendLine($"[{section.Index + 1}] {plan}");
            text.AppendLine($"    hc={section.Hc.ToString("0.#", culture)}  hb={section.Hb.ToString("0.#", culture)}  zb={section.Zb.ToString("0.#", culture)}");
            text.AppendLine($"    bottom={section.BottomPosition.ToString("0.#", culture)}  top={section.TopPosition.ToString("0.#", culture)}");

            if (section.Shape == SectionShape.Rectangle)
            {
                text.AppendLine($"    west={section.WestPosition.ToString("0.#", culture)}  east={section.EastPosition.ToString("0.#", culture)}");
                text.AppendLine($"    south={section.SouthPosition.ToString("0.#", culture)}  north={section.NorthPosition.ToString("0.#", culture)}");
            }
            else
            {
                text.AppendLine($"    centre=({section.CenterX.ToString("0.#", culture)}, {section.CenterY.ToString("0.#", culture)})");
            }
        }

        return text.ToString();
    }
}
