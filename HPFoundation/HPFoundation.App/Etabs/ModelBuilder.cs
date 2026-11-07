using ETABSv1;
using HPFoundation.Core.Modeling;
using static HPFoundation.App.Etabs.EtabsCall;

namespace HPFoundation.App.Etabs;

/// <summary>Writes a <see cref="ModelSpec"/> into a blank ETABS model, the way the engineer builds it by hand.</summary>
internal static class ModelBuilder
{
    public const string ComboName = "TH-MONG";
    public const string LoadPatternName = "P-CHANCOT";

    private const string MaterialName = "CONC";
    private const string SectionName = "DMB1";
    private const string InteriorSpringName = "Z1";
    private const string EndSpringName = "Z2";
    private const string GlobalAxes = "Global";
    private const double PoissonRatio = 0.2;
    private const double HorizontalStiffnessTPerM = 1e6;
    private const double BeamYM = 4;
    private const double BeamZM = 3;
    private const double StoryHeightM = 3;
    private const double SectionAreaToleranceM2 = 1e-6;
    private const int DefaultColor = -1;
    private const int UserSpring = 1;
    private const int LinearAdditive = 0;

    public static IReadOnlyList<string> Build(cSapModel model, ModelSpec spec)
    {
        Check(model.InitializeNewModel(eUnits.Ton_m_C), "InitializeNewModel");
        Check(model.File.NewGridOnly(1, StoryHeightM, StoryHeightM, 2, 2, 1, 1), "File.NewGridOnly");

        DefineConcrete(model, spec);
        DefineSection(model, spec.Section);
        DefineSprings(model, spec);
        DefineLoadCase(model);

        var pointNames = AddNodes(model, spec);
        AddBeam(model, pointNames);
        return pointNames;
    }

    private static void DefineConcrete(cSapModel model, ModelSpec spec)
    {
        Check(model.PropMaterial.SetMaterial(MaterialName, eMatType.Concrete, DefaultColor, "", ""), "PropMaterial.SetMaterial");
        Check(model.PropMaterial.SetMPIsotropic(MaterialName, spec.ConcreteModulusTPerM2, PoissonRatio, 0, 0), "PropMaterial.SetMPIsotropic");
    }

    private static void DefineSection(cSapModel model, TeeSection section)
    {
        double[] widths = [section.BeamWidthM, section.FootingWidthM, section.BeamWidthM, 0];
        double[] depths = [section.BeamDepthM, section.StemHeightM, 0, 0, section.HaunchHeightM, section.EdgeThicknessM];
        Check(model.PropFrame.SetPrecastI(SectionName, MaterialName, ref widths, ref depths, DefaultColor, "", ""), "PropFrame.SetPrecastI");
        RequireSectionArea(model, section);
    }

    /// <summary>The meaning of the precast depths was inferred from one section, so ETABS must confirm the area.</summary>
    private static void RequireSectionArea(cSapModel model, TeeSection section)
    {
        double area = 0, shear2 = 0, shear3 = 0, torsion = 0, i22 = 0, i33 = 0, s22 = 0, s33 = 0, z22 = 0, z33 = 0, r22 = 0, r33 = 0;
        Check(
            model.PropFrame.GetSectProps(SectionName, ref area, ref shear2, ref shear3, ref torsion, ref i22, ref i33, ref s22, ref s33, ref z22, ref z33, ref r22, ref r33),
            "PropFrame.GetSectProps");
        if (Math.Abs(area - section.AreaM2) > SectionAreaToleranceM2)
        {
            throw new InvalidOperationException($"ETABS section area {area} m2 differs from the expected {section.AreaM2} m2.");
        }
    }

    private static void DefineSprings(cSapModel model, ModelSpec spec)
    {
        DefineSpring(model, InteriorSpringName, spec.InteriorStiffnessTPerM);
        DefineSpring(model, EndSpringName, spec.EndStiffnessTPerM);
    }

    private static void DefineSpring(cSapModel model, string name, double verticalStiffnessTPerM)
    {
        double[] stiffness = [HorizontalStiffnessTPerM, HorizontalStiffnessTPerM, verticalStiffnessTPerM, 0, 0, 0];
        Check(
            model.PropPointSpring.SetPointSpringProp(name, UserSpring, ref stiffness, GlobalAxes, "", "", 0, DefaultColor, "", ""),
            "PropPointSpring.SetPointSpringProp");
    }

    private static void DefineLoadCase(cSapModel model)
    {
        Check(model.LoadPatterns.Add(LoadPatternName, eLoadPatternType.SuperDead, 0, true), "LoadPatterns.Add");
        Check(model.RespCombo.Add(ComboName, LinearAdditive), "RespCombo.Add");
        var caseType = eCNameType.LoadCase;
        Check(model.RespCombo.SetCaseList(ComboName, ref caseType, LoadPatternName, 1), "RespCombo.SetCaseList");
    }

    private static List<string> AddNodes(cSapModel model, ModelSpec spec)
    {
        var names = new List<string>(spec.Nodes.Count);
        foreach (var node in spec.Nodes)
        {
            var name = string.Empty;
            Check(model.PointObj.AddCartesian(node.XM, BeamYM, BeamZM, ref name, "", GlobalAxes, false, 0), "PointObj.AddCartesian");
            AssignSpring(model, name, node.Spring);
            AssignLoad(model, name, node);
            names.Add(name);
        }

        return names;
    }

    private static void AssignSpring(cSapModel model, string pointName, SpringKind spring)
    {
        if (spring != SpringKind.None)
        {
            var property = spring == SpringKind.End ? EndSpringName : InteriorSpringName;
            Check(model.PointObj.SetSpringAssignment(pointName, property, eItemType.Objects), "PointObj.SetSpringAssignment");
        }
    }

    /// <summary>Same mapping as the hand model: Qx along the beam, Qy across, N downwards, Mx about the beam axis.</summary>
    private static void AssignLoad(cSapModel model, string pointName, ModelNode node)
    {
        if (node.Load is not { } load)
        {
            return;
        }

        double[] forces = [load.ShearXT, load.ShearYT, -load.AxialT, load.MomentXTm, load.MomentYTm, 0];
        Check(model.PointObj.SetLoadForce(pointName, LoadPatternName, ref forces, false, GlobalAxes, eItemType.Objects), "PointObj.SetLoadForce");
    }

    private static void AddBeam(cSapModel model, List<string> pointNames)
    {
        foreach (var (start, end) in pointNames.Zip(pointNames.Skip(1)))
        {
            var name = string.Empty;
            Check(model.FrameObj.AddByPoint(start, end, ref name, SectionName, ""), "FrameObj.AddByPoint");
        }
    }
}
