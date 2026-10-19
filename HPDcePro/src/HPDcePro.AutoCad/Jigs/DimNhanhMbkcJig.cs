using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive framing plan multi-tier quick dimensioning DrawJig (DimNhanhMBKC).
    /// </summary>
    public sealed class DimNhanhMbkcJig : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public Point3d CurrentCursor { get; set; }
        public List<Point3d> IntermediateNodes { get; } = new List<Point3d>();
        public double TierSpacingMm { get; set; } = 500.0;
        public bool TwoTiers { get; set; } = true;

        public DimNhanhMbkcJig()
        {
            StartPoint = Point3d.Origin;
            EndPoint = Point3d.Origin;
            CurrentCursor = Point3d.Origin;
        }

        public DimNhanhMbkcJig(Point3d p1, Point3d p2, bool twoTiers = true, double tierSpacing = 500.0)
        {
            StartPoint = p1;
            EndPoint = p2;
            CurrentCursor = p1;
            TwoTiers = twoTiers;
            TierSpacingMm = tierSpacing > 0 ? tierSpacing : 500.0;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static (List<(Point3d s, Point3d e, Point3d d)> tier1, (Point3d s, Point3d e, Point3d d)? tier2)
            CalculateDimensionChains(Point3d p1, Point3d p2, IReadOnlyList<Point3d>? intermediateNodes, Point3d dimPt, double tierSpacing, bool twoTiers)
        {
            var tier1 = new List<(Point3d, Point3d, Point3d)>();
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-4) return (tier1, null);

            Vector3d u = new Vector3d(v.X / len, v.Y / len, 0);
            Vector3d n = new Vector3d(-u.Y, u.X, 0);

            // Project dimPt onto perpendicular offset
            Vector3d toDim = dimPt - p1;
            double perpDist = (toDim.X * n.X) + (toDim.Y * n.Y);
            Point3d tier1DimPt = p1 + (n * perpDist);

            var sortedNodes = new List<Point3d> { p1 };
            if (intermediateNodes != null)
            {
                for (int i = 0; i < intermediateNodes.Count; i++)
                {
                    sortedNodes.Add(intermediateNodes[i]);
                }
            }
            sortedNodes.Add(p2);

            for (int i = 0; i < sortedNodes.Count - 1; i++)
            {
                tier1.Add((sortedNodes[i], sortedNodes[i + 1], tier1DimPt));
            }

            (Point3d, Point3d, Point3d)? tier2 = null;
            if (twoTiers)
            {
                double tier2Sign = perpDist >= 0 ? 1.0 : -1.0;
                Point3d tier2DimPt = tier1DimPt + (n * (tier2Sign * tierSpacing));
                tier2 = (p1, p2, tier2DimPt);
            }

            return (tier1, tier2);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick dimension line placement offset point:")
            {
                BasePoint = StartPoint,
                UseBasePoint = true,
                Cursor = CursorType.RubberBand
            };

            PromptPointResult res = prompts.AcquirePoint(opts);
            if (res.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (res.Value.DistanceTo(CurrentCursor) < 1e-4) return SamplerStatus.NoChange;

            CurrentCursor = res.Value;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (draw == null) return false;

            var (tier1, tier2) = CalculateDimensionChains(
                StartPoint, EndPoint, IntermediateNodes, CurrentCursor, TierSpacingMm, TwoTiers);

            for (int i = 0; i < tier1.Count; i++)
            {
                var (s, e, d) = tier1[i];
                using var line = new Line(s, d);
                using var dimSeg = new Line(d, d + (e - s));
                draw.Geometry.Draw(line);
                draw.Geometry.Draw(dimSeg);
            }

            if (tier2.HasValue)
            {
                var (s, e, d) = tier2.Value;
                using var line = new Line(s, d);
                using var dimSeg = new Line(d, d + (e - s));
                draw.Geometry.Draw(line);
                draw.Geometry.Draw(dimSeg);
            }

            return true;
        }
    }
}
