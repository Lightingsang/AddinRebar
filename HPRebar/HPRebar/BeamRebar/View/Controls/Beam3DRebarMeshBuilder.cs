using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;

using MeshGeometry3D = System.Windows.Media.Media3D.MeshGeometry3D;
using Vector3 = System.Numerics.Vector3;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Converts pure domain rebar polylines and stirrup runs into cylindrical 3D tube meshes.
/// </summary>
public static class Beam3DRebarMeshBuilder
{
    public const int DefaultThetaDiv = 8;

    /// <summary>
    /// Builds a combined tube mesh for a collection of longitudinal or cross-tie rebar polylines.
    /// </summary>
    public static MeshGeometry3D BuildBarMesh(
        IEnumerable<BarPolyline> bars,
        int thetaDiv = DefaultThetaDiv)
    {
        var builder = new MeshBuilder();
        foreach (var bar in bars)
        {
            AddBarTube(builder, bar, thetaDiv);
        }

        var mesh = builder.ToMesh().ToWndMeshGeometry3D();
        mesh.Freeze();
        return mesh;
    }

    /// <summary>
    /// Builds closed rectangular stirrup tube meshes across all clear spans and node regions.
    /// </summary>
    public static MeshGeometry3D BuildStirrupMesh(
        BeamContinuousStack stack,
        BeamStirrupSpec stirrupSpec,
        BeamSpecialBarSpec? specialSpec = null,
        int thetaDiv = DefaultThetaDiv)
    {
        var builder = new MeshBuilder();
        if (stack.Spans.Count == 0 || stirrupSpec.Diameter <= 0)
        {
            return builder.ToMesh().ToWndMeshGeometry3D();
        }

        AddSpanStirrups(builder, stack, stirrupSpec, thetaDiv);
        AddNodeStirrups(builder, stack, stirrupSpec, thetaDiv);

        if (specialSpec is not null && specialSpec.EnableHangingStirrups)
        {
            AddHangingStirrups(builder, stack, stirrupSpec, specialSpec, thetaDiv);
        }

        var mesh = builder.ToMesh().ToWndMeshGeometry3D();
        mesh.Freeze();
        return mesh;
    }

    private static void AddBarTube(MeshBuilder builder, BarPolyline bar, int thetaDiv)
    {
        if (bar.Points.Count < 2) return;

        var clean = new List<Vector3>(bar.Points.Count);
        foreach (var pt in bar.Points)
        {
            var v = new Vector3((float)pt.X, (float)pt.Y, (float)pt.Z);
            if (clean.Count == 0 || Vector3.Distance(clean[clean.Count - 1], v) > 0.5f)
            {
                clean.Add(v);
            }
        }

        if (clean.Count >= 2)
        {
            float diameter = (float)Math.Max(4.0, bar.Diameter);
            builder.AddTube(clean, diameter, thetaDiv, bar.Polyline.IsClosed);
        }
    }

    private static void AddSpanStirrups(
        MeshBuilder builder,
        BeamContinuousStack stack,
        BeamStirrupSpec spec,
        int thetaDiv)
    {
        foreach (var span in stack.Spans)
        {
            if (span.LengthClear <= 0) continue;
            var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(span.LengthClear, spec, span.IsCantilever);

            foreach (var run in runs)
            {
                foreach (double xRel in run.Positions)
                {
                    double xAbs = span.StartX + xRel;
                    AddSingleStirrup(builder, xAbs, span.Width, span.BottomElevation, span.TopElevation, spec, thetaDiv);
                }
            }
        }
    }

    private static void AddNodeStirrups(
        MeshBuilder builder,
        BeamContinuousStack stack,
        BeamStirrupSpec spec,
        int thetaDiv)
    {
        if (!spec.IncludeStirrupsInNodes || stack.Supports.Count <= 2) return;

        for (int i = 1; i < stack.Supports.Count - 1; i++)
        {
            var support = stack.Supports[i];
            var run = BeamStirrupDistributionCalculator.ComputeNodeRun(support.Width, spec.Cover, spec.NodeSpacing);
            if (run.Count <= 0) continue;

            var prev = stack.Spans[i - 1];
            var next = stack.Spans[i];
            double w = Math.Min(prev.Width, next.Width);
            double zBot = Math.Max(prev.BottomElevation, next.BottomElevation);
            double zTop = Math.Min(prev.TopElevation, next.TopElevation);

            foreach (double xRel in run.Positions)
            {
                double xAbs = support.LeftFaceX + xRel;
                AddSingleStirrup(builder, xAbs, w, zBot, zTop, spec, thetaDiv);
            }
        }
    }

    private static void AddHangingStirrups(
        MeshBuilder builder,
        BeamContinuousStack stack,
        BeamStirrupSpec stirrupSpec,
        BeamSpecialBarSpec specialSpec,
        int thetaDiv)
    {
        foreach (var sec in stack.SecondaryIntersections)
        {
            var stations = BeamSpecialBarCalculator.ComputeHangingStirrupStations(
                sec.CenterX, sec.Width, specialSpec.HangingStirrupsPerSide, specialSpec.HangingStirrupSpacing);

            var span = stack.Spans.FirstOrDefault(s => sec.CenterX >= s.StartX && sec.CenterX <= s.EndX);
            if (span is null) continue;

            foreach (double xAbs in stations)
            {
                AddSingleStirrup(builder, xAbs, span.Width, span.BottomElevation, span.TopElevation, stirrupSpec, thetaDiv);
            }
        }
    }

    private static void AddSingleStirrup(
        MeshBuilder builder,
        double xAbs,
        double width,
        double zBot,
        double zTop,
        BeamStirrupSpec spec,
        int thetaDiv)
    {
        double cover = spec.Cover;
        double d = spec.Diameter;
        double halfD = d / 2.0;

        float y1 = (float)(-(width / 2.0) + cover + halfD);
        float y2 = (float)((width / 2.0) - cover - halfD);
        float z1 = (float)(zBot + cover + halfD);
        float z2 = (float)(zTop - cover - halfD);
        float x = (float)xAbs;

        if (y2 <= y1 || z2 <= z1) return;

        var loop = new List<Vector3>(4)
        {
            new(x, y1, z1),
            new(x, y2, z1),
            new(x, y2, z2),
            new(x, y1, z2)
        };

        builder.AddTube(loop, (float)d, thetaDiv, true);
    }
}
