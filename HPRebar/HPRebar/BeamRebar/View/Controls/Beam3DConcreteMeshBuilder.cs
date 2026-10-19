using System;
using System.Linq;
using System.Numerics;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using HPRebar.Core.BeamRebar.Models;

using MeshGeometry3D = System.Windows.Media.Media3D.MeshGeometry3D;
using Vector3 = System.Numerics.Vector3;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Constructs 3D bounding box meshes for continuous concrete beam spans and support column stubs.
/// </summary>
public static class Beam3DConcreteMeshBuilder
{
    private const double DefaultStubHeightMm = 600.0;

    /// <summary>
    /// Builds the 3D concrete envelope mesh covering all clear spans and beam-column joint regions.
    /// </summary>
    public static MeshGeometry3D BuildConcreteBoxMesh(BeamContinuousStack stack)
    {
        var builder = new MeshBuilder();
        if (stack.Spans.Count == 0)
        {
            return builder.ToMesh().ToWndMeshGeometry3D();
        }

        AddSpanBoxes(builder, stack);
        AddJointContinuityBoxes(builder, stack);

        var mesh = builder.ToMesh().ToWndMeshGeometry3D();
        mesh.Freeze();
        return mesh;
    }

    /// <summary>
    /// Builds 3D column/wall stub meshes extending above and below each support node.
    /// </summary>
    public static MeshGeometry3D BuildSupportStubMesh(
        BeamContinuousStack stack,
        double stubHeightMm = DefaultStubHeightMm)
    {
        var builder = new MeshBuilder();
        if (stack.Supports.Count == 0 || stack.Spans.Count == 0)
        {
            return builder.ToMesh().ToWndMeshGeometry3D();
        }

        foreach (var node in stack.Supports)
        {
            AddSupportStubs(builder, stack, node, stubHeightMm);
        }

        var mesh = builder.ToMesh().ToWndMeshGeometry3D();
        mesh.Freeze();
        return mesh;
    }

    private static void AddSpanBoxes(MeshBuilder builder, BeamContinuousStack stack)
    {
        foreach (var span in stack.Spans)
        {
            if (span.LengthClear <= 0 || span.Width <= 0 || span.Height <= 0) continue;

            double xc = span.StartX + (span.LengthClear / 2.0);
            double zc = (span.TopElevation + span.BottomElevation) / 2.0;

            var center = new Vector3((float)xc, 0f, (float)zc);
            builder.AddBox(center, (float)span.LengthClear, (float)span.Width, (float)span.Height);
        }
    }

    private static void AddJointContinuityBoxes(MeshBuilder builder, BeamContinuousStack stack)
    {
        for (int i = 1; i < stack.Supports.Count - 1 && i < stack.Spans.Count; i++)
        {
            var support = stack.Supports[i];
            if (support.Width <= 0) continue;

            var prevSpan = stack.Spans[i - 1];
            var nextSpan = stack.Spans[i];

            double w = Math.Max(prevSpan.Width, nextSpan.Width);
            double h = Math.Max(prevSpan.Height, nextSpan.Height);
            double zTop = Math.Max(prevSpan.TopElevation, nextSpan.TopElevation);
            double zBot = zTop - h;
            double zc = (zTop + zBot) / 2.0;

            var center = new Vector3((float)support.CenterX, 0f, (float)zc);
            builder.AddBox(center, (float)support.Width, (float)w, (float)h);
        }
    }

    private static void AddSupportStubs(
        MeshBuilder builder,
        BeamContinuousStack stack,
        BeamSupportNode node,
        double stubHeight)
    {
        double width = node.Width > 0 ? node.Width : 400.0;
        double depth = node.Depth > 0 ? node.Depth : width;

        double zBot = FindAdjacentElevation(stack, node, isTop: false);
        double zTop = FindAdjacentElevation(stack, node, isTop: true);

        // Lower column stub
        double lowerZc = zBot - (stubHeight / 2.0);
        builder.AddBox(new Vector3((float)node.CenterX, 0f, (float)lowerZc), (float)width, (float)depth, (float)stubHeight);

        // Upper column stub (40% shorter for visual balance)
        double upperH = stubHeight * 0.6;
        double upperZc = zTop + (upperH / 2.0);
        builder.AddBox(new Vector3((float)node.CenterX, 0f, (float)upperZc), (float)width, (float)depth, (float)upperH);
    }

    private static double FindAdjacentElevation(BeamContinuousStack stack, BeamSupportNode node, bool isTop)
    {
        var near = stack.Spans
            .Where(s => Math.Abs(s.StartX - node.CenterX) <= node.Width * 1.5 ||
                        Math.Abs(s.EndX - node.CenterX) <= node.Width * 1.5)
            .ToList();

        if (near.Count == 0)
        {
            return isTop ? stack.Spans[0].TopElevation : stack.Spans[0].BottomElevation;
        }

        return isTop ? near.Max(s => s.TopElevation) : near.Min(s => s.BottomElevation);
    }
}
