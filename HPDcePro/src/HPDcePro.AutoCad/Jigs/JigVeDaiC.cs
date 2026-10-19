using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive C-shaped cross-tie stirrup DrawJig (JigVeDaiC).
    /// </summary>
    public sealed class JigVeDaiC : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double HookLeftMm { get; set; } = 80.0;
        public double HookRightMm { get; set; } = 80.0;
        public int DiameterMm { get; set; } = 8;

        public JigVeDaiC()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigVeDaiC(Point3d startPoint, int diameter = 8, double hookLeft = 80.0, double hookRight = 80.0)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            DiameterMm = diameter > 0 ? diameter : 8;
            HookLeftMm = hookLeft;
            HookRightMm = hookRight;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static Point3d[] CalculateCTiePoints(
            Point3d p1, Point3d p2, double hookLeft, double hookRight, int diameter)
        {
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-4) return new[] { p1, p2 };

            Vector3d u = new Vector3d(v.X / len, v.Y / len, 0);
            Vector3d n = new Vector3d(-u.Y, u.X, 0);

            double hL = hookLeft > 0 ? hookLeft : Math.Max(60.0, 8.0 * diameter);
            double hR = hookRight > 0 ? hookRight : Math.Max(60.0, 8.0 * diameter);

            Point3d pStartHook = p1 + (n * hL);
            Point3d pEndHook = p2 + (n * hR);

            return new[] { pStartHook, p1, p2, pEndHook };
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick second anchor point for C-shaped cross-tie:")
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

            Point3d[] pts = CalculateCTiePoints(StartPoint, CurrentPoint, HookLeftMm, HookRightMm, DiameterMm);
            if (pts.Length < 4) return true;

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(pts.Length);
            for (int i = 0; i < pts.Length; i++)
            {
                pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }

            draw.Geometry.Draw(pl);
            return true;
        }
    }
}
