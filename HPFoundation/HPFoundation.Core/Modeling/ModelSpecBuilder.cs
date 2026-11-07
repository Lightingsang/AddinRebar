using HPFoundation.Core.Model;

namespace HPFoundation.Core.Modeling;

/// <summary>Turns a sheet's input into the analysis model the engineer would build by hand.</summary>
public static class ModelSpecBuilder
{
    private const double SameNodeToleranceM = 1e-6;

    public static ModelSpec Build(FoundationInput input, FlangeProfile flange)
    {
        var section = BuildSection(input.Section, flange);
        return new ModelSpec(
            input.SheetName,
            BuildNodes(input),
            section,
            ConcreteCatalog.ElasticModulusTPerM2(input.ConcreteGrade),
            RoundStiffness(input.Springs.InteriorStiffnessTPerM),
            RoundStiffness(input.Springs.EndStiffnessTPerM));
    }

    private static TeeSection BuildSection(BeamSection section, FlangeProfile flange)
    {
        var flangeDepthM = flange.EdgeThicknessM + flange.HaunchHeightM;
        if (Math.Abs(flangeDepthM - section.FlangeDepthM) > SameNodeToleranceM)
        {
            throw new ArgumentException(
                $"Edge + haunch = {flangeDepthM} m differs from the sheet's flange depth {section.FlangeDepthM} m.",
                nameof(flange));
        }

        return new TeeSection(
            section.FootingWidthM,
            section.BeamWidthM,
            section.BeamDepthM,
            section.BeamDepthM - flangeDepthM,
            flange.HaunchHeightM,
            flange.EdgeThicknessM);
    }

    private static List<ModelNode> BuildNodes(FoundationInput input)
    {
        var divisions = (int)Math.Round(input.FootingLengthM / input.Springs.InteriorSpacingM);
        var springNodes = Enumerable
            .Range(0, divisions + 1)
            .Select(index => new ModelNode(
                index * input.Springs.InteriorSpacingM,
                index == 0 || index == divisions ? SpringKind.End : SpringKind.Interior,
                null))
            .ToList();

        return input.Columns
            .Aggregate(springNodes, PlaceColumn)
            .OrderBy(node => node.XM)
            .ToList();
    }

    /// <summary>A column between two springs still needs its own node: it carries a load, not a piece of soil.</summary>
    private static List<ModelNode> PlaceColumn(List<ModelNode> nodes, ColumnLoad column)
    {
        var index = nodes.FindIndex(node => Math.Abs(node.XM - column.AxisPositionM) <= SameNodeToleranceM);
        if (index >= 0)
        {
            nodes[index] = nodes[index] with { Load = column };
        }
        else
        {
            nodes.Add(new ModelNode(column.AxisPositionM, SpringKind.None, column));
        }

        return nodes;
    }

    private static double RoundStiffness(double stiffnessTPerM) =>
        Math.Round(stiffnessTPerM, MidpointRounding.AwayFromZero);
}
