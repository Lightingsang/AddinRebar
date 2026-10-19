using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Interactive rebar tag leader pointer DrawJig (TagLeader).
    /// </summary>
    public sealed class TagLeaderJig : DceDrawJigBase
    {
        public Point3d AnchorPoint { get; set; }
        public Point3d CurrentCursor { get; set; }
        public double ShelfLength { get; set; } = 300.0;
        public bool Mirror { get; set; } = false;
        public string TagContent { get; set; } = "1Φ18";

        public TagLeaderJig()
        {
            AnchorPoint = Point3d.Origin;
            CurrentCursor = Point3d.Origin;
        }

        public TagLeaderJig(Point3d anchor, string content = "1Φ18", double shelfLength = 300.0)
        {
            AnchorPoint = anchor;
            CurrentCursor = anchor;
            TagContent = content ?? "1Φ18";
            ShelfLength = shelfLength > 0 ? shelfLength : 300.0;
        }

        public override PromptStatus StartJig(Editor editor)
        {
            if (editor == null) return PromptStatus.Cancel;
            return editor.Drag(this).Status;
        }

        public PromptStatus StartJig(Editor editor, Point3d anchor, string content, double shelfLength = 300.0, bool mirror = false)
        {
            AnchorPoint = anchor;
            CurrentCursor = anchor;
            TagContent = content;
            ShelfLength = shelfLength;
            Mirror = mirror;
            return StartJig(editor);
        }

        public static (Point3d p1, Point3d p2, Point3d p3, Point3d textOrigin) CalculateLeaderPoints(
            Point3d p1, Point3d cursor, double shelfLength, bool mirror)
        {
            Point3d p2 = cursor;
            double dir = cursor.X >= p1.X ? 1.0 : -1.0;
            if (mirror) dir = -dir;

            Point3d p3 = new Point3d(p2.X + (dir * shelfLength), p2.Y, p2.Z);
            double textOffset = 25.0;
            Point3d textOrigin = new Point3d(
                dir > 0 ? p2.X + 20.0 : p3.X + 20.0,
                p2.Y + textOffset,
                p2.Z);

            return (p1, p2, p3, textOrigin);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            if (prompts == null) return SamplerStatus.NoChange;

            var opts = new JigPromptPointOptions("\nPick leader shoulder placement point:")
            {
                BasePoint = AnchorPoint,
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

            var (p1, p2, p3, _) = CalculateLeaderPoints(AnchorPoint, CurrentCursor, ShelfLength, Mirror);

            using var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline(3);
            pl.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0, 0, 0);

            draw.Geometry.Draw(pl);
            return true;
        }
    }
}
