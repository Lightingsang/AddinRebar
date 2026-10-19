using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive shear wall boundary element detailing DrawJig (ijgVeThepVachCT).
    /// </summary>
    public sealed class JigVeThepVachCt : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double WallThicknessMm { get; set; } = 250.0;
        public double BoundaryLengthMm { get; set; } = 400.0;
        public double CoverMm { get; set; } = 25.0;
        public int BoundaryBarCount { get; set; } = 6;
        public int WebBarCount { get; set; } = 4;

        public JigVeThepVachCt()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigVeThepVachCt(Point3d startPoint, double tw = 250.0, double lc = 400.0)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            WallThicknessMm = tw > 0 ? tw : 250.0;
            BoundaryLengthMm = lc > 0 ? lc : 400.0;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static (Point3d[] wallOutline, Point3d[] leftCage, Point3d[] rightCage, List<Point3d> barDots) CalculateBoundaryCage(
            Point3d p1, Point3d p2, double tw, double lc, double cover, int boundaryBarCount, int webBarCount)
        {
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-4) len = 1000.0;

            Vector3d u = len > 1e-4 ? new Vector3d(v.X / len, v.Y / len, 0) : new Vector3d(1, 0, 0);
            Vector3d n = new Vector3d(-u.Y, u.X, 0);

            Point3d w1 = p1 + (n * (tw / 2.0));
            Point3d w2 = p2 + (n * (tw / 2.0));
            Point3d w3 = p2 - (n * (tw / 2.0));
            Point3d w4 = p1 - (n * (tw / 2.0));
            Point3d[] wallOutline = { w1, w2, w3, w4 };

            double effLc = Math.Min(lc, len / 2.0 - 50.0);
            if (effLc < 100.0) effLc = 100.0;

            double halfCoreY = (tw / 2.0) - cover;
            Point3d c1 = p1 + (u * cover) + (n * halfCoreY);
            Point3d c2 = p1 + (u * (cover + effLc)) + (n * halfCoreY);
            Point3d c3 = p1 + (u * (cover + effLc)) - (n * halfCoreY);
            Point3d c4 = p1 + (u * cover) - (n * halfCoreY);
            Point3d[] leftCage = { c1, c2, c3, c4 };

            Point3d r1 = p2 - (u * (cover + effLc)) + (n * halfCoreY);
            Point3d r2 = p2 - (u * cover) + (n * halfCoreY);
            Point3d r3 = p2 - (u * cover) - (n * halfCoreY);
            Point3d r4 = p2 - (u * (cover + effLc)) - (n * halfCoreY);
            Point3d[] rightCage = { r1, r2, r3, r4 };

            var dots = new List<Point3d> { c1, c2, c3, c4, r1, r2, r3, r4 };
            return (wallOutline, leftCage, rightCage, dots);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick shear wall endpoint:")
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

            var (wall, leftCage, rightCage, dots) = CalculateBoundaryCage(
                StartPoint, CurrentPoint, WallThicknessMm, BoundaryLengthMm, CoverMm, BoundaryBarCount, WebBarCount);

            DrawPolygon(draw, wall);
            DrawPolygon(draw, leftCage);
            DrawPolygon(draw, rightCage);

            for (int i = 0; i < dots.Count; i++)
            {
                using var c = new Circle(dots[i], Vector3d.ZAxis, 6.0);
                draw.Geometry.Draw(c);
            }

            return true;
        }

        private static void DrawPolygon(WorldDraw draw, Point3d[] pts)
        {
            if (pts.Length < 3) return;
            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(pts.Length);
            for (int i = 0; i < pts.Length; i++)
            {
                pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
            pl.Closed = true;
            draw.Geometry.Draw(pl);
        }
    }
}
