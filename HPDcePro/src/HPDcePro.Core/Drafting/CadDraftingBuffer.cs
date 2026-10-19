using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Drafting
{
    public sealed class CadDraftingBuffer
    {
        private readonly List<CadEntityPrimitive> _entities = new List<CadEntityPrimitive>();

        public IReadOnlyList<CadEntityPrimitive> Entities => _entities;
        public int Count => _entities.Count;

        public void AddPrimitive(CadEntityPrimitive primitive)
        {
            if (primitive == null) throw new ArgumentNullException(nameof(primitive));
            _entities.Add(primitive);
        }

        public void AddLine(Point2D start, Point2D end, string layer = "0", int colorIndex = 256, double lineWeight = 0.0)
        {
            var line = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Line,
                StartPoint = start,
                EndPoint = end,
                Layer = layer,
                Color = new CadColor(colorIndex),
                LineWeight = lineWeight
            };
            _entities.Add(line);
        }

        public void AddPolyline(
            IEnumerable<Point2D> points,
            bool isClosed,
            string layer = "0",
            int colorIndex = 256,
            double constantWidth = 0.0,
            IEnumerable<XDataPacket>? xdata = null)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var pline = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Polyline,
                IsClosed = isClosed,
                Layer = layer,
                Color = new CadColor(colorIndex),
                ConstantWidth = constantWidth
            };
            foreach (var pt in points) pline.Points.Add(pt);
            if (xdata != null)
            {
                foreach (var packet in xdata) pline.AttachXData(packet);
            }
            _entities.Add(pline);
        }

        public void AddCircle(
            Point2D center, double radius, string layer = "0", int colorIndex = 256,
            IEnumerable<XDataPacket>? xdata = null)
        {
            var circle = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Circle,
                Center = center,
                Radius = radius,
                Layer = layer,
                Color = new CadColor(colorIndex)
            };
            if (xdata != null)
            {
                foreach (var packet in xdata) circle.AttachXData(packet);
            }
            _entities.Add(circle);
        }

        public void AddText(
            string text,
            Point2D position,
            double height = 2.5,
            string layer = "0",
            int colorIndex = 256,
            double rotationRad = 0.0,
            string alignment = "Left",
            IEnumerable<XDataPacket>? xdata = null)
        {
            var txt = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Text,
                TextString = text ?? string.Empty,
                Position = position,
                TextHeight = height,
                Layer = layer,
                Color = new CadColor(colorIndex),
                RotationRad = rotationRad,
                Alignment = alignment
            };
            if (xdata != null)
            {
                foreach (var packet in xdata) txt.AttachXData(packet);
            }
            _entities.Add(txt);
        }

        public void AddDimension(
            Point2D p1,
            Point2D p2,
            Point2D textPoint,
            string layer = "0",
            int colorIndex = 256,
            string text = "",
            string style = "Standard")
        {
            var dim = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Dimension,
                DimensionPoint1 = p1,
                DimensionPoint2 = p2,
                TextPoint = textPoint,
                DimensionText = text,
                DimensionStyle = style,
                Layer = layer,
                Color = new CadColor(colorIndex)
            };
            _entities.Add(dim);
        }

        public void AddHatch(string pattern, IEnumerable<IEnumerable<Point2D>> boundaryLoops, string layer = "0", int colorIndex = 256)
        {
            var hatch = new CadEntityPrimitive
            {
                EntityType = CadEntityType.Hatch,
                PatternName = pattern ?? "SOLID",
                Layer = layer,
                Color = new CadColor(colorIndex)
            };
            if (boundaryLoops != null)
            {
                foreach (var loop in boundaryLoops)
                {
                    var loopPts = new List<Point2D>();
                    foreach (var p in loop) loopPts.Add(p);
                    hatch.BoundaryLoops.Add(loopPts);
                }
            }
            _entities.Add(hatch);
        }

        public void AddBlockInsert(
            string blockName,
            Point2D insertionPoint,
            double scale = 1.0,
            double rotationRad = 0.0,
            string layer = "0",
            IDictionary<string, string>? attributes = null)
        {
            var insert = new CadEntityPrimitive
            {
                EntityType = CadEntityType.BlockInsert,
                BlockName = blockName ?? string.Empty,
                InsertionPoint = insertionPoint,
                ScaleFactor = scale,
                RotationRad = rotationRad,
                Layer = layer
            };
            if (attributes != null)
            {
                foreach (var kvp in attributes) insert.Attributes[kvp.Key] = kvp.Value;
            }
            _entities.Add(insert);
        }

        public void Clear() => _entities.Clear();

        public void TransformBy(double dx, double dy, double scale = 1.0, double rotationRad = 0.0)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                _entities[i].TransformBy(dx, dy, scale, rotationRad);
            }
        }

        public (Point2D Min, Point2D Max) GetBoundingBox()
        {
            if (_entities.Count == 0) return (new Point2D(0, 0), new Point2D(0, 0));
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            for (int i = 0; i < _entities.Count; i++)
            {
                var (bMin, bMax) = _entities[i].ComputeBoundingBox();
                if (bMin.X < minX) minX = bMin.X;
                if (bMin.Y < minY) minY = bMin.Y;
                if (bMax.X > maxX) maxX = bMax.X;
                if (bMax.Y > maxY) maxY = bMax.Y;
            }
            return (new Point2D(minX, minY), new Point2D(maxX, maxY));
        }
    }
}
