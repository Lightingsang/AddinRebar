using System;
using System.Collections.Generic;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Represents an abstract framing segment with 3D endpoints for collinear chain analysis.
    /// </summary>
    public sealed class FramingSegment
    {
        public string Id { get; set; } = string.Empty;
        public RevitPoint3D StartPt { get; set; }
        public RevitPoint3D EndPt { get; set; }

        public RevitPoint3D Direction
        {
            get
            {
                double dx = EndPt.X - StartPt.X;
                double dy = EndPt.Y - StartPt.Y;
                double dz = EndPt.Z - StartPt.Z;
                double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                return len > 1e-6 ? new RevitPoint3D(dx / len, dy / len, dz / len) : RevitPoint3D.Zero;
            }
        }
    }

    /// <summary>
    /// Bidirectional collinear chain builder for structural framing elements.
    /// Supports interior seed elements, reversed curve orientations, and angle tolerance.
    /// </summary>
    public static class FramingChainCalculator
    {
        public static List<FramingSegment> ChainCollinearSegments(
            FramingSegment seed,
            List<FramingSegment> candidates,
            double angleToleranceDeg = 1.0,
            double touchToleranceFt = 0.164)
        {
            var chain = new List<FramingSegment> { seed };
            var remaining = new List<FramingSegment>(candidates);
            remaining.RemoveAll(c => c.Id == seed.Id);

            double cosTol = Math.Cos(angleToleranceDeg * Math.PI / 180.0);
            RevitPoint3D seedDir = seed.Direction;
            RevitPoint3D chainStart = seed.StartPt;
            RevitPoint3D chainEnd = seed.EndPt;

            ExpandForward(chain, remaining, seedDir, ref chainEnd, cosTol, touchToleranceFt);
            ExpandBackward(chain, remaining, seedDir, ref chainStart, cosTol, touchToleranceFt);

            return chain;
        }

        private static void ExpandForward(
            List<FramingSegment> chain, List<FramingSegment> candidates,
            RevitPoint3D seedDir, ref RevitPoint3D chainEnd, double cosTol, double touchTol)
        {
            bool added = true;
            while (added)
            {
                added = false;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    var cand = candidates[i];
                    RevitPoint3D dir = cand.Direction;
                    double dot = Math.Abs(seedDir.X * dir.X + seedDir.Y * dir.Y + seedDir.Z * dir.Z);
                    if (dot < cosTol) continue;

                    if (chainEnd.DistanceTo(cand.StartPt) <= touchTol)
                    {
                        chain.Add(cand);
                        chainEnd = cand.EndPt;
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                    if (chainEnd.DistanceTo(cand.EndPt) <= touchTol)
                    {
                        chain.Add(cand);
                        chainEnd = cand.StartPt;
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                }
            }
        }

        private static void ExpandBackward(
            List<FramingSegment> chain, List<FramingSegment> candidates,
            RevitPoint3D seedDir, ref RevitPoint3D chainStart, double cosTol, double touchTol)
        {
            bool added = true;
            while (added)
            {
                added = false;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    var cand = candidates[i];
                    RevitPoint3D dir = cand.Direction;
                    double dot = Math.Abs(seedDir.X * dir.X + seedDir.Y * dir.Y + seedDir.Z * dir.Z);
                    if (dot < cosTol) continue;

                    if (chainStart.DistanceTo(cand.EndPt) <= touchTol)
                    {
                        chain.Insert(0, cand);
                        chainStart = cand.StartPt;
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                    if (chainStart.DistanceTo(cand.StartPt) <= touchTol)
                    {
                        chain.Insert(0, cand);
                        chainStart = cand.EndPt;
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                }
            }
        }
    }
}
