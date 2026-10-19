using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive circular/closed hoop stirrup DrawJig with 135-degree seismic hooks (ijgVeDaiVong).
    /// </summary>
    public sealed class JigVeDaiVong : DceDrawJigBase
    {
        public Point3d CenterPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double HookLengthMm { get; set; } = 100.0;
        public double DiameterMm { get; set; } = 10.0;

        public JigVeDaiVong()
        {
            CenterPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigVeDaiVong(Point3d center, double diameter = 10.0, double hookLength = 100.0)
        {
            CenterPoint = center;
            CurrentPoint = center;
            DiameterMm = diameter > 0 ? diameter : 10.0;
            HookLengthMm = hookLength > 0 ? hookLength : 10.0 * DiameterMm;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static (Point3d center, double radius, Point3d hook1, Point3d hook2) CalculateHoopGeometry(
            Point3d center, Point3d cursor, double hookLength, double diameter)
        {
            Vector3d v = cursor - center;
            double radius = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (radius < 1.0) radius = 100.0;

            // 135-degree internal seismic hook legs from top splice (0, radius)
            double hookLen = hookLength > 0 ? hookLength : Math.Max(75.0, 10.0 * diameter);
            double angle135Rad = 135.0 * Math.PI / 180.0;

            Point3d splicePt = new Point3d(center.X, center.Y + radius, center.Z);
            Point3d hook1 = new Point3d(
                splicePt.X - (hookLen * Math.Cos(angle135Rad - (Math.PI / 2.0))),
                splicePt.Y - (hookLen * Math.Sin(angle135Rad - (Math.PI / 2.0))),
                center.Z);
            Point3d hook2 = new Point3d(
                splicePt.X + (hookLen * Math.Cos(angle135Rad - (Math.PI / 2.0))),
                splicePt.Y - (hookLen * Math.Sin(angle135Rad - (Math.PI / 2.0))),
                center.Z);

            return (center, radius, hook1, hook2);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick radius point for circular hoop stirrup:")
            {
                BasePoint = CenterPoint,
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

            var (center, radius, h1, h2) = CalculateHoopGeometry(CenterPoint, CurrentPoint, HookLengthMm, DiameterMm);

            using var circle = new Circle(center, Vector3d.ZAxis, radius);
            draw.Geometry.Draw(circle);

            Point3d splicePt = new Point3d(center.X, center.Y + radius, center.Z);
            using var hookLine1 = new Line(splicePt, h1);
            using var hookLine2 = new Line(splicePt, h2);
            draw.Geometry.Draw(hookLine1);
            draw.Geometry.Draw(hookLine2);

            return true;
        }
    }
}
