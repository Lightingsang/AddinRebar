using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive rebar spacing distribution and dimension tick DrawJig (TagChiaThep).
    /// </summary>
    public sealed class TagChiaThepJig : DceDrawJigBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double SpacingMm { get; set; } = 150.0;
        public double TickLengthMm { get; set; } = 60.0;
        public int DiameterMm { get; set; } = 10;

        public TagChiaThepJig()
        {
            StartPoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public TagChiaThepJig(Point3d startPoint, double spacing = 150.0, int diameter = 10)
        {
            StartPoint = startPoint;
            CurrentPoint = startPoint;
            SpacingMm = spacing > 0 ? spacing : 150.0;
            DiameterMm = diameter > 0 ? diameter : 10;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public PromptStatus StartJig(Editor editor, Point3d p1, double spacing, int diameter)
        {
            StartPoint = p1;
            CurrentPoint = p1;
            SpacingMm = spacing;
            DiameterMm = diameter;
            return StartJig(editor);
        }

        public static (Point3d lineStart, Point3d lineEnd, List<(Point3d t1, Point3d t2)> ticks, int count) CalculateDistributionTicks(
            Point3d p1, Point3d p2, double spacing, double tickLen)
        {
            var ticks = new List<(Point3d, Point3d)>();
            Vector3d v = p2 - p1;
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-4 || spacing <= 0)
            {
                return (p1, p2, ticks, 1);
            }

            Vector3d u = new Vector3d(v.X / len, v.Y / len, 0);
            Vector3d n = new Vector3d(-u.Y, u.X, 0) * (tickLen / 2.0);

            int intervals = (int)Math.Floor(len / spacing);
            if (intervals < 1) intervals = 1;
            int barCount = intervals + 1;

            for (int i = 0; i <= intervals; i++)
            {
                double dist = i * spacing;
                if (dist > len) dist = len;
                Point3d pt = p1 + (u * dist);
                ticks.Add((pt - n, pt + n));
            }

            // Always ensure closing tick at p2
            if (intervals * spacing < len - 1e-3)
            {
                ticks.Add((p2 - n, p2 + n));
                barCount++;
            }

            return (p1, p2, ticks, intervals);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick distribution extent endpoint:")
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

            var (lStart, lEnd, ticks, _) = CalculateDistributionTicks(StartPoint, CurrentPoint, SpacingMm, TickLengthMm);

            using var mainLine = new Line(lStart, lEnd);
            draw.Geometry.Draw(mainLine);

            for (int i = 0; i < ticks.Count; i++)
            {
                using var tickLine = new Line(ticks[i].t1, ticks[i].t2);
                draw.Geometry.Draw(tickLine);
            }

            return true;
        }
    }
}
