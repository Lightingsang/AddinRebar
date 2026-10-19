using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive arbitrary polyline rebar drafting DrawJig (VePolylineBatKy).
    /// </summary>
    public sealed class VePolylineBatKyJig : DceDrawJigBase
    {
        public List<Point3d> Vertices { get; } = new List<Point3d>();
        public Point3d CurrentCursor { get; set; }
        public double BendRadiusMm { get; set; } = 25.0;

        public VePolylineBatKyJig()
        {
            CurrentCursor = Point3d.Origin;
        }

        public VePolylineBatKyJig(Point3d startPoint, double bendRadius = 25.0)
        {
            Vertices.Add(startPoint);
            CurrentCursor = startPoint;
            BendRadiusMm = bendRadius > 0 ? bendRadius : 25.0;
        }

        public void AddVertex(Point3d pt)
        {
            Vertices.Add(pt);
            CurrentCursor = pt;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public static List<Point3d> CalculatePolylineVertices(IReadOnlyList<Point3d> fixedPoints, Point3d cursor)
        {
            var result = new List<Point3d>();
            if (fixedPoints != null)
            {
                result.AddRange(fixedPoints);
            }
            if (result.Count == 0 || result[result.Count - 1].DistanceTo(cursor) > 1e-4)
            {
                result.Add(cursor);
            }
            return result;
        }

        public static (List<Point3d> pts, List<double> bulges) CalculateFilletedRebar(List<Point3d> pts, double bendRadius)
        {
            var resultPts = new List<Point3d>();
            var bulges = new List<double>();
            if (pts == null || pts.Count == 0) return (resultPts, bulges);

            for (int i = 0; i < pts.Count; i++)
            {
                resultPts.Add(pts[i]);
                bulges.Add(0.0);
            }
            return (resultPts, bulges);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            Point3d basePt = Vertices.Count > 0 ? Vertices[Vertices.Count - 1] : Point3d.Origin;
            var opts = new JigPromptPointOptions("\nPick next rebar vertex:")
            {
                BasePoint = basePt,
                UseBasePoint = Vertices.Count > 0,
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

            var allPts = CalculatePolylineVertices(Vertices, CurrentCursor);
            if (allPts.Count < 2) return true;

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(allPts.Count);
            for (int i = 0; i < allPts.Count; i++)
            {
                pl.AddVertexAt(i, new Point2d(allPts[i].X, allPts[i].Y), 0, 0, 0);
            }

            draw.Geometry.Draw(pl);
            return true;
        }
    }
}
