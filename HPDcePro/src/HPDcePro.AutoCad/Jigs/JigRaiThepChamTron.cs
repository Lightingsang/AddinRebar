using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive rebar dot cross-section distribution DrawJig (ijgRaithepchamtron).
    /// </summary>
    public sealed class JigRaiThepChamTron : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double OffsetLeftMm { get; set; } = 35.0;
        public double OffsetRightMm { get; set; } = 35.0;
        public double OffsetPerpendicularMm { get; set; } = 0.0;
        public double SpacingMm { get; set; } = 150.0;
        public int DiameterMm { get; set; } = 18;
        public bool SkipFirst { get; set; } = false;
        public bool SkipLast { get; set; } = false;

        public JigRaiThepChamTron()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigRaiThepChamTron(Point3d startPoint, double spacing = 150.0, int diameter = 18)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            SpacingMm = spacing > 0 ? spacing : 150.0;
            DiameterMm = diameter > 0 ? diameter : 18;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static List<Point3d> CalculateDotPositions(
            Point3d p1, Point3d p2, double ostrai, double osphai, double osvg, double spacing, bool boDau, bool boCuoi)
        {
            var dots = new List<Point3d>();
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-4 || spacing <= 0) return dots;

            Vector3d u = new Vector3d(v.X / len, v.Y / len, 0);
            Vector3d n = new Vector3d(-u.Y, u.X, 0);

            double effectiveLen = len - ostrai - osphai;
            if (effectiveLen < 0) effectiveLen = 0;

            Point3d startPt = p1 + (u * ostrai) + (n * osvg);
            int count = (int)Math.Floor(effectiveLen / spacing);
            if (count < 1) count = 1;

            for (int i = 0; i <= count; i++)
            {
                if (i == 0 && boDau) continue;
                if (i == count && boCuoi) continue;

                double d = i * spacing;
                if (d > effectiveLen) d = effectiveLen;
                dots.Add(startPt + (u * d));
            }

            return dots;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick rebar distribution endpoint:")
            {
                BasePoint = StartPoint,
                UseBasePoint = true,
                Cursor = CursorType.RubberBand
            };

            PromptPointResult res = prompts.AcquirePoint(opts);
            if (res.Status != PromptStatus.OK) return SamplerStatus.Cancel;
            if (res.Value.DistanceTo(CurrentPoint) < 1e-4) return SamplerStatus.NoChange;

            CurrentPoint = res.Value;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (draw == null) return false;

            var dots = CalculateDotPositions(
                StartPoint, CurrentPoint, OffsetLeftMm, OffsetRightMm, OffsetPerpendicularMm, SpacingMm, SkipFirst, SkipLast);

            double radius = DiameterMm / 2.0;
            if (radius < 1.0) radius = 8.0;

            for (int i = 0; i < dots.Count; i++)
            {
                using var circle = new Circle(dots[i], Vector3d.ZAxis, radius);
                draw.Geometry.Draw(circle);
            }

            return true;
        }
    }
}
