using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive framing beam/slab layout box DrawJig (VeMBKC_JIG).
    /// </summary>
    public sealed class VeMBKCJig : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double Width { get; set; } = 220.0;
        public string Alignment { get; set; } = "Mid";
        public string Label { get; set; } = "D1";

        public VeMBKCJig()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public VeMBKCJig(Point3d startPoint, double width, string alignment = "Mid", string label = "D1")
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            Width = width > 0 ? width : 220.0;
            Alignment = alignment ?? "Mid";
            Label = label ?? "D1";
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public PromptStatus StartJig(Editor editor, Point3d p1, double width, string alignment = "Mid", string label = "D1")
        {
            StartPoint = p1;
            CurrentPoint = p1;
            Width = width;
            Alignment = alignment;
            Label = label;
            return StartJig(editor);
        }

        public static (Point3d d1, Point3d d2, Point3d d3, Point3d d4, Point3d dText) CalculateCorners(
            Point3d p1, Point3d p2, double width, string alignment)
        {
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            Vector3d n = len > 1e-4 ? new Vector3d(-v.Y / len, v.X / len, 0) : new Vector3d(0, 1, 0);

            double shift = alignment?.ToUpperInvariant() switch
            {
                "TOP" => -width / 2.0,
                "BOT" => width / 2.0,
                _ => 0.0
            };

            Vector3d offsetTop = n * (width / 2.0 + shift);
            Vector3d offsetBot = n * (-width / 2.0 + shift);

            Point3d d1 = p1 + offsetBot;
            Point3d d2 = p2 + offsetBot;
            Point3d d3 = p2 + offsetTop;
            Point3d d4 = p1 + offsetTop;
            Point3d dText = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, (p1.Z + p2.Z) / 2.0) + (n * shift);

            return (d1, d2, d3, d4, dText);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick second point for beam framing:")
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

            var (d1, d2, d3, d4, _) = CalculateCorners(StartPoint, CurrentPoint, Width, Alignment);

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(4);
            pl.AddVertexAt(0, new Point2d(d1.X, d1.Y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(d2.X, d2.Y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(d3.X, d3.Y), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(d4.X, d4.Y), 0, 0, 0);
            pl.Closed = true;

            draw.Geometry.Draw(pl);
            return true;
        }
    }
}
