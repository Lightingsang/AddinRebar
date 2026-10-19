using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive basement retaining wall section DrawJig (JigVeVachHam).
    /// </summary>
    public sealed class JigVeVachHam : DceDrawJigBase
    {
        public Point3d BasePoint { get; set; }
        public Point3d CurrentPoint { get; set; }
        public double WallThicknessMm { get; set; } = 350.0;
        public double CoverEarthMm { get; set; } = 50.0;
        public double CoverInteriorMm { get; set; } = 30.0;
        public bool HasWaterstop { get; set; } = true;

        public JigVeVachHam()
        {
            BasePoint = Point3d.Origin;
            CurrentPoint = Point3d.Origin;
        }

        public JigVeVachHam(Point3d basePt, double tw = 350.0, bool hasWaterstop = true)
        {
            BasePoint = basePt;
            CurrentPoint = basePt;
            WallThicknessMm = tw > 0 ? tw : 350.0;
            HasWaterstop = hasWaterstop;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static (Point3d[] outline, Point3d[] waterstop, Point3d earthStart, Point3d earthEnd, Point3d intStart, Point3d intEnd)
            CalculateRetainingWallSection(Point3d basePt, double height, double tw, double coverLeft, double coverRight, bool waterstop)
        {
            double h = Math.Abs(height) > 10.0 ? Math.Abs(height) : 3000.0;
            double w = tw > 50.0 ? tw : 350.0;

            Point3d p1 = basePt;
            Point3d p2 = new Point3d(basePt.X + w, basePt.Y, basePt.Z);
            Point3d p3 = new Point3d(basePt.X + w, basePt.Y + h, basePt.Z);
            Point3d p4 = new Point3d(basePt.X, basePt.Y + h, basePt.Z);
            Point3d[] outline = { p1, p2, p3, p4 };

            Point3d earthStart = new Point3d(basePt.X + coverLeft, basePt.Y + 50.0, basePt.Z);
            Point3d earthEnd = new Point3d(basePt.X + coverLeft, basePt.Y + h - 50.0, basePt.Z);

            Point3d intStart = new Point3d(basePt.X + w - coverRight, basePt.Y + 50.0, basePt.Z);
            Point3d intEnd = new Point3d(basePt.X + w - coverRight, basePt.Y + h - 50.0, basePt.Z);

            Point3d[] ws = Array.Empty<Point3d>();
            if (waterstop)
            {
                double midX = basePt.X + (w / 2.0);
                ws = new[]
                {
                    new Point3d(midX, basePt.Y - 100.0, basePt.Z),
                    new Point3d(midX, basePt.Y + 100.0, basePt.Z)
                };
            }

            return (outline, ws, earthStart, earthEnd, intStart, intEnd);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick top height point for basement wall section:")
            {
                BasePoint = BasePoint,
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

            double h = CurrentPoint.Y - BasePoint.Y;
            var (outline, ws, eStart, eEnd, iStart, iEnd) = CalculateRetainingWallSection(
                BasePoint, h, WallThicknessMm, CoverEarthMm, CoverInteriorMm, HasWaterstop);

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(4);
            for (int i = 0; i < 4; i++)
            {
                pl.AddVertexAt(i, new Point2d(outline[i].X, outline[i].Y), 0, 0, 0);
            }
            pl.Closed = true;
            draw.Geometry.Draw(pl);

            using var lineEarth = new Line(eStart, eEnd);
            using var lineInt = new Line(iStart, iEnd);
            draw.Geometry.Draw(lineEarth);
            draw.Geometry.Draw(lineInt);

            if (ws.Length >= 2)
            {
                using var wsLine = new Line(ws[0], ws[1]);
                draw.Geometry.Draw(wsLine);
            }

            return true;
        }
    }
}
