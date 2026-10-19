using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive slab reinforcement bar DrawJig with hooks and curtailment cutoffs (JigSlabRebar).
    /// </summary>
    public sealed class JigSlabRebar : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public int DiameterMm { get; set; } = 10;
        public double OffsetLeftMm { get; set; } = 50.0;
        public double HookLeftMm { get; set; } = 100.0;
        public double OffsetRightMm { get; set; } = 50.0;
        public double HookRightMm { get; set; } = 100.0;

        public JigSlabRebar()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigSlabRebar(Point3d startPoint, int diameter = 10, double offsetL = 50.0, double hookL = 100.0)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            DiameterMm = diameter > 0 ? diameter : 10;
            OffsetLeftMm = offsetL;
            HookLeftMm = hookL;
            OffsetRightMm = offsetL;
            HookRightMm = hookL;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public PromptStatus StartJig(Editor editor, Point3d p1, int diameter, double offsetL, double hookL, double offsetR, double hookR)
        {
            StartPoint = p1;
            CurrentPoint = p1;
            DiameterMm = diameter;
            OffsetLeftMm = offsetL;
            HookLeftMm = hookL;
            OffsetRightMm = offsetR;
            HookRightMm = hookR;
            return StartJig(editor);
        }

        public static Point3d[] CalculateRebarVertices(
            Point3d p1, Point3d p2, double offsetL, double hookL, double offsetR, double hookR, int diameter)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-4)
            {
                return new[] { p1, p2 };
            }

            double ux = dx / len;
            double uy = dy / len;
            double nx = -uy;
            double ny = ux;

            Point3d startBar = new Point3d(p1.X + ux * offsetL, p1.Y + uy * offsetL, p1.Z);
            Point3d endBar = new Point3d(p2.X - ux * offsetR, p2.Y - uy * offsetR, p2.Z);

            var vertices = new List<Point3d>();
            if (hookL > 0)
            {
                vertices.Add(new Point3d(startBar.X - nx * hookL, startBar.Y - ny * hookL, startBar.Z));
            }

            vertices.Add(startBar);
            vertices.Add(endBar);

            if (hookR > 0)
            {
                vertices.Add(new Point3d(endBar.X - nx * hookR, endBar.Y - ny * hookR, endBar.Z));
            }

            return vertices.ToArray();
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick end point of slab rebar span:")
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

            Point3d[] vertices = CalculateRebarVertices(
                StartPoint, CurrentPoint, OffsetLeftMm, HookLeftMm, OffsetRightMm, HookRightMm, DiameterMm);

            if (vertices.Length < 2) return true;

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                pl.AddVertexAt(i, new Point2d(vertices[i].X, vertices[i].Y), 0, 0, 0);
            }

            draw.Geometry.Draw(pl);
            return true;
        }
    }
}
