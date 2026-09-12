using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.ColumnRebar.Model;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     The settings the tool starts from before the user has touched anything: four corner bars, ties at
///     half the column width, no cross-ties, top dowels carrying on into the column above with a staggered
///     lap. The tool window replaces this once it exists.
/// </summary>
public static class DefaultRebarSpecBuilder
{
    public static IReadOnlyList<ColumnRebarSpec> Build(Document document, ColumnStack stack)
    {
        var barTypes = RebarTypeCatalog.BarTypes(document);
        var chosen = RebarTypeCatalog.DefaultBarType(barTypes);

        if (chosen is null) return new List<ColumnRebarSpec>();

        // Ties are normally thinner than the bars they restrain.
        var tieType = barTypes[0];
        var cover = RebarTypeCatalog.DefaultCoverMm(document);
        var specs = new List<ColumnRebarSpec>(stack.Sections.Count);

        foreach (var section in stack.Sections)
        {
            var layout = new BarLayoutSpec
            {
                Nx = section.Shape == SectionShape.Rectangle ? 2 : 0,
                Ny = section.Shape == SectionShape.Rectangle ? 2 : 0,
                Nd = section.Shape == SectionShape.Rectangle ? 0 : 4,
                BarDiameter = chosen.DiameterMm,
                StirrupDiameter = tieType.DiameterMm,
                Cover = cover
            };

            var splices = new List<SpliceSpec>(layout.BarCount);

            for (var barNumber = 1; barNumber <= layout.BarCount; barNumber++)
            {
                splices.Add(SpliceSpec.Default(barNumber, layout.BarDiameter, layout.SplitOverlap, layout.OverlapFactor));
            }

            var spacing = section.Shape == SectionShape.Rectangle ? section.B / 2 : section.D / 2;

            specs.Add(new ColumnRebarSpec
            {
                Layout = layout,
                Splices = splices,
                Stirrups = new StirrupSpec { TypeDis = 0, S = spacing, S1 = spacing / 2, S2 = spacing, IsTiesUp = false },
                Ties = new AdditionalTieSpec(),
                MainBarType = chosen,
                StirrupBarType = tieType,
                TieBarType = tieType,
                PartitionName = "Column Rebar"
            });
        }

        return specs;
    }
}
