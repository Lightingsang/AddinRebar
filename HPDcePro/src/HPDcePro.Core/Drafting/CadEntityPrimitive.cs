using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Drafting
{
    public sealed class CadEntityPrimitive
    {
        public CadEntityType EntityType { get; set; }
        public string Layer { get; set; } = "0";
        public CadColor Color { get; set; } = CadColor.ByLayer;
        public double LineWeight { get; set; } = 0.0;

        public Point2D StartPoint { get; set; }
        public Point2D EndPoint { get; set; }
        public List<Point2D> Points { get; } = new List<Point2D>();
        public bool IsClosed { get; set; }
        public double ConstantWidth { get; set; }

        public Point2D Center { get; set; }
        public double Radius { get; set; }

        public Point2D Position { get; set; }
        public string TextString { get; set; } = string.Empty;
        public double TextHeight { get; set; } = 2.5;
        public double RotationRad { get; set; }
        public string Alignment { get; set; } = "Left";

        public Point2D DimensionPoint1 { get; set; }
        public Point2D DimensionPoint2 { get; set; }
        public Point2D TextPoint { get; set; }
        public string DimensionText { get; set; } = string.Empty;
        public string DimensionStyle { get; set; } = "Standard";

        public string PatternName { get; set; } = "SOLID";
        public List<List<Point2D>> BoundaryLoops { get; } = new List<List<Point2D>>();

        public string BlockName { get; set; } = string.Empty;
        public Point2D InsertionPoint { get; set; }
        public double ScaleFactor { get; set; } = 1.0;
        public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<XDataPacket> AttachedXData { get; } = new List<XDataPacket>();

        public void AttachXData(XDataPacket packet)
        {
            if (packet != null) AttachedXData.Add(packet);
        }

        public (Point2D Min, Point2D Max) ComputeBoundingBox()
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            void Include(Point2D p)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            switch (EntityType)
            {
                case CadEntityType.Line:
                    Include(StartPoint);
                    Include(EndPoint);
                    break;
                case CadEntityType.Polyline:
                    for (int i = 0; i < Points.Count; i++) Include(Points[i]);
                    break;
                case CadEntityType.Circle:
                    Include(new Point2D(Center.X - Radius, Center.Y - Radius));
                    Include(new Point2D(Center.X + Radius, Center.Y + Radius));
                    break;
                case CadEntityType.Dimension:
                    Include(DimensionPoint1);
                    Include(DimensionPoint2);
                    Include(TextPoint);
                    break;
                case CadEntityType.Text:
                    Include(Position);
                    Include(new Point2D(Position.X + TextString.Length * TextHeight * 0.8, Position.Y + TextHeight));
                    break;
                default:
                    Include(InsertionPoint);
                    break;
            }

            if (minX > maxX) return (new Point2D(0, 0), new Point2D(0, 0));
            return (new Point2D(minX, minY), new Point2D(maxX, maxY));
        }

        public void TransformBy(double dx, double dy, double scale = 1.0, double rotationRad = 0.0)
        {
            StartPoint = TransformPoint(StartPoint, dx, dy, scale, rotationRad);
            EndPoint = TransformPoint(EndPoint, dx, dy, scale, rotationRad);
            Center = TransformPoint(Center, dx, dy, scale, rotationRad);
            Position = TransformPoint(Position, dx, dy, scale, rotationRad);
            DimensionPoint1 = TransformPoint(DimensionPoint1, dx, dy, scale, rotationRad);
            DimensionPoint2 = TransformPoint(DimensionPoint2, dx, dy, scale, rotationRad);
            TextPoint = TransformPoint(TextPoint, dx, dy, scale, rotationRad);
            InsertionPoint = TransformPoint(InsertionPoint, dx, dy, scale, rotationRad);
            Radius *= scale;
            TextHeight *= scale;
            RotationRad += rotationRad;

            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = TransformPoint(Points[i], dx, dy, scale, rotationRad);
            }
            for (int l = 0; l < BoundaryLoops.Count; l++)
            {
                var loop = BoundaryLoops[l];
                for (int i = 0; i < loop.Count; i++)
                {
                    loop[i] = TransformPoint(loop[i], dx, dy, scale, rotationRad);
                }
            }
        }

        private static Point2D TransformPoint(Point2D p, double dx, double dy, double scale, double rotationRad)
        {
            double sx = p.X * scale;
            double sy = p.Y * scale;
            if (Math.Abs(rotationRad) > 1e-9)
            {
                double cos = Math.Cos(rotationRad);
                double sin = Math.Sin(rotationRad);
                double rx = sx * cos - sy * sin;
                double ry = sx * sin + sy * cos;
                return new Point2D(rx + dx, ry + dy);
            }
            return new Point2D(sx + dx, sy + dy);
        }
    }
}
