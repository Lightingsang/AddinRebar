using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

using MeshGeometry3D = System.Windows.Media.Media3D.MeshGeometry3D;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Assembles and batches complete 3D continuous beam models into 7 frozen <see cref="GeometryModel3D"/> instances
/// inside a single <see cref="Model3DGroup"/> for 60+ FPS DirectX rendering.
/// </summary>
public static class Beam3DSceneBuilder
{
    /// <summary>
    /// Builds the 7-model batched 3D scene from pure domain continuous stack and rebar specification.
    /// </summary>
    public static Model3DGroup BuildScene(BeamContinuousStack stack, BeamRebarSpec spec)
    {
        var group = new Model3DGroup();
        if (stack.Spans.Count == 0)
        {
            group.Freeze();
            return group;
        }

        // Model 1: Concrete Boxes
        group.Children.Add(BuildConcreteModel(stack));

        // Model 2 & 3: Top & Bottom Main Longitudinal Bars
        var (topMain, botMain) = BuildMainBarModels(stack, spec);
        group.Children.Add(topMain);
        group.Children.Add(botMain);

        // Model 4: Additional Bars (Support Top + Span Bottom)
        group.Children.Add(BuildAdditionalBarsModel(stack, spec));

        // Model 5: Side (Skin) Bars & Transverse Cross-Ties
        group.Children.Add(BuildSideBarsModel(stack, spec));

        // Model 6: Stirrups (Shear ties across spans, nodes, and secondary framing)
        group.Children.Add(BuildStirrupsModel(stack, spec));

        // Model 7: Supports (Upper and lower column/wall bearing stubs)
        group.Children.Add(BuildSupportsModel(stack));

        group.Freeze();
        return group;
    }

    private static GeometryModel3D BuildConcreteModel(BeamContinuousStack stack)
    {
        var mesh = Beam3DConcreteMeshBuilder.BuildConcreteBoxMesh(stack);
        var model = new GeometryModel3D(mesh, Beam3DMaterials.Concrete);
        model.Freeze();
        return model;
    }

    private static (GeometryModel3D Top, GeometryModel3D Bot) BuildMainBarModels(
        BeamContinuousStack stack,
        BeamRebarSpec spec)
    {
        IReadOnlyList<BarPolyline> topBars;
        IReadOnlyList<BarPolyline> botBars;
        try
        {
            topBars = BeamMainBarCalculator.ComputeTopMainBars(stack, spec.MainBars, spec.Stirrups.Diameter);
            botBars = BeamMainBarCalculator.ComputeBottomMainBars(stack, spec.MainBars, spec.Stirrups.Diameter);
        }
        catch
        {
            topBars = Array.Empty<BarPolyline>();
            botBars = Array.Empty<BarPolyline>();
        }

        var topMesh = Beam3DRebarMeshBuilder.BuildBarMesh(topBars);
        var botMesh = Beam3DRebarMeshBuilder.BuildBarMesh(botBars);

        var topModel = new GeometryModel3D(topMesh, Beam3DMaterials.MainTopBar);
        var botModel = new GeometryModel3D(botMesh, Beam3DMaterials.MainBottomBar);

        topModel.Freeze();
        botModel.Freeze();
        return (topModel, botModel);
    }

    private static GeometryModel3D BuildAdditionalBarsModel(
        BeamContinuousStack stack,
        BeamRebarSpec spec)
    {
        var allAdd = new List<BarPolyline>();
        try
        {
            var topAdd = BeamAdditionalBarCalculator.ComputeSupportTopBars(
                stack, spec.AdditionalBars, spec.Stirrups.Diameter);
            allAdd.AddRange(topAdd);

            var botAdd = BeamAdditionalBarCalculator.ComputeSpanBottomBars(
                stack, spec.AdditionalBars, spec.Stirrups.Diameter);
            allAdd.AddRange(botAdd);
        }
        catch
        {
            // Ignore calculation fallback
        }

        var mesh = Beam3DRebarMeshBuilder.BuildBarMesh(allAdd);
        var model = new GeometryModel3D(mesh, Beam3DMaterials.AdditionalBar);
        model.Freeze();
        return model;
    }

    private static GeometryModel3D BuildSideBarsModel(
        BeamContinuousStack stack,
        BeamRebarSpec spec)
    {
        var allSide = new List<BarPolyline>();
        try
        {
            if (spec.SideBars.AutoSkinBars)
            {
                var skin = BeamSideBarCalculator.ComputeLongitudinalSideBars(
                    stack, spec.SideBars, spec.Stirrups.Diameter, spec.MainBars.TopDiameter);
                allSide.AddRange(skin);

                var ties = BeamSideBarCalculator.ComputeCrossTies(
                    stack, spec.SideBars, spec.Stirrups.Diameter, spec.MainBars.TopDiameter);
                allSide.AddRange(ties);
            }
        }
        catch
        {
            // Ignore calculation fallback
        }

        var mesh = Beam3DRebarMeshBuilder.BuildBarMesh(allSide);
        var model = new GeometryModel3D(mesh, Beam3DMaterials.SideBar);
        model.Freeze();
        return model;
    }

    private static GeometryModel3D BuildStirrupsModel(
        BeamContinuousStack stack,
        BeamRebarSpec spec)
    {
        MeshGeometry3D mesh;
        try
        {
            mesh = Beam3DRebarMeshBuilder.BuildStirrupMesh(stack, spec.Stirrups, spec.SpecialBars);
        }
        catch
        {
            mesh = new MeshGeometry3D();
        }

        var model = new GeometryModel3D(mesh, Beam3DMaterials.Stirrup);
        model.Freeze();
        return model;
    }

    private static GeometryModel3D BuildSupportsModel(BeamContinuousStack stack)
    {
        var mesh = Beam3DConcreteMeshBuilder.BuildSupportStubMesh(stack);
        var model = new GeometryModel3D(mesh, Beam3DMaterials.Support);
        model.Freeze();
        return model;
    }
}
