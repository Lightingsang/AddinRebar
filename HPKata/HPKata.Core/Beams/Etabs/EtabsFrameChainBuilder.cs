using System;
using System.Collections.Generic;

namespace HPKata.Core.Beams.Etabs
{
    /// <summary>
    /// Geometry and endpoint coordinates of an ETABS frame element.
    /// </summary>
    public sealed class EtabsFrameGeometry
    {
        public string FrameName { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double EndZ { get; set; }
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;

        public double LengthMm =>
            Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2) + Math.Pow(EndZ - StartZ, 2));

        public (double Ux, double Uy, double Uz) UnitDirection
        {
            get
            {
                double len = LengthMm;
                if (len <= 0.0) return (1.0, 0.0, 0.0);
                return ((EndX - StartX) / len, (EndY - StartY) / len, (EndZ - StartZ) / len);
            }
        }
    }

    /// <summary>
    /// Chained sequence of collinear ETABS frames forming a continuous multi-span beam.
    /// </summary>
    public sealed class EtabsChainedRun
    {
        public string RunName { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public List<EtabsFrameGeometry> Frames { get; } = new List<EtabsFrameGeometry>();
        public double TotalLengthMm
        {
            get
            {
                double total = 0.0;
                foreach (var f in Frames) total += f.LengthMm;
                return total;
            }
        }
    }

    /// <summary>
    /// Groups and chains collinear ETABS frame elements into continuous beam runs.
    /// </summary>
    public static class EtabsFrameChainBuilder
    {
        public static List<EtabsChainedRun> BuildChains(List<EtabsFrameGeometry> allFrames)
        {
            var results = new List<EtabsChainedRun>();
            if (allFrames.Count == 0) return results;

            // Group by Story
            var byStory = new Dictionary<string, List<EtabsFrameGeometry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var frame in allFrames)
            {
                if (!byStory.TryGetValue(frame.Story, out var list))
                {
                    list = new List<EtabsFrameGeometry>();
                    byStory[frame.Story] = list;
                }
                list.Add(frame);
            }

            foreach (var kvp in byStory)
            {
                var storyRuns = ChainStoryFrames(kvp.Key, kvp.Value);
                results.AddRange(storyRuns);
            }

            return results;
        }

        private static List<EtabsChainedRun> ChainStoryFrames(string story, List<EtabsFrameGeometry> frames)
        {
            var runs = new List<EtabsChainedRun>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < frames.Count; i++)
            {
                var seed = frames[i];
                if (visited.Contains(seed.FrameName)) continue;

                var currentChain = new List<EtabsFrameGeometry> { seed };
                visited.Add(seed.FrameName);

                var remaining = new List<EtabsFrameGeometry>();
                for (int j = 0; j < frames.Count; j++)
                {
                    if (!visited.Contains(frames[j].FrameName))
                    {
                        remaining.Add(frames[j]);
                    }
                }

                (double X, double Y, double Z) chainStart = (seed.StartX, seed.StartY, seed.StartZ);
                (double X, double Y, double Z) chainEnd = (seed.EndX, seed.EndY, seed.EndZ);
                var seedDir = seed.UnitDirection;

                ExpandForward(currentChain, remaining, visited, seedDir, ref chainEnd, 50.0);
                ExpandBackward(currentChain, remaining, visited, seedDir, ref chainStart, 50.0);

                var run = new EtabsChainedRun
                {
                    RunName = currentChain[0].FrameName,
                    Story = story
                };
                run.Frames.AddRange(currentChain);
                runs.Add(run);
            }

            return runs;
        }

        private static void ExpandForward(
            List<EtabsFrameGeometry> chain,
            List<EtabsFrameGeometry> candidates,
            HashSet<string> visited,
            (double Ux, double Uy, double Uz) seedDir,
            ref (double X, double Y, double Z) chainEnd,
            double toleranceMm)
        {
            bool added = true;
            while (added)
            {
                added = false;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    var cand = candidates[i];
                    var dir = cand.UnitDirection;
                    double dot = Math.Abs(seedDir.Ux * dir.Ux + seedDir.Uy * dir.Uy + seedDir.Uz * dir.Uz);
                    if (dot < 0.999) continue;

                    if (DistanceBetween(chainEnd.X, chainEnd.Y, chainEnd.Z, cand.StartX, cand.StartY, cand.StartZ) <= toleranceMm)
                    {
                        chain.Add(cand);
                        chainEnd = (cand.EndX, cand.EndY, cand.EndZ);
                        visited.Add(cand.FrameName);
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                    if (DistanceBetween(chainEnd.X, chainEnd.Y, chainEnd.Z, cand.EndX, cand.EndY, cand.EndZ) <= toleranceMm)
                    {
                        chain.Add(cand);
                        chainEnd = (cand.StartX, cand.StartY, cand.StartZ);
                        visited.Add(cand.FrameName);
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                }
            }
        }

        private static void ExpandBackward(
            List<EtabsFrameGeometry> chain,
            List<EtabsFrameGeometry> candidates,
            HashSet<string> visited,
            (double Ux, double Uy, double Uz) seedDir,
            ref (double X, double Y, double Z) chainStart,
            double toleranceMm)
        {
            bool added = true;
            while (added)
            {
                added = false;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    var cand = candidates[i];
                    var dir = cand.UnitDirection;
                    double dot = Math.Abs(seedDir.Ux * dir.Ux + seedDir.Uy * dir.Uy + seedDir.Uz * dir.Uz);
                    if (dot < 0.999) continue;

                    if (DistanceBetween(chainStart.X, chainStart.Y, chainStart.Z, cand.EndX, cand.EndY, cand.EndZ) <= toleranceMm)
                    {
                        chain.Insert(0, cand);
                        chainStart = (cand.StartX, cand.StartY, cand.StartZ);
                        visited.Add(cand.FrameName);
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                    if (DistanceBetween(chainStart.X, chainStart.Y, chainStart.Z, cand.StartX, cand.StartY, cand.StartZ) <= toleranceMm)
                    {
                        chain.Insert(0, cand);
                        chainStart = (cand.EndX, cand.EndY, cand.EndZ);
                        visited.Add(cand.FrameName);
                        candidates.RemoveAt(i);
                        added = true;
                        break;
                    }
                }
            }
        }

        public static bool IsCollinearAndTouching(
            EtabsFrameGeometry frameA, EtabsFrameGeometry frameB, double toleranceMm = 50.0)
        {
            var uA = frameA.UnitDirection;
            var uB = frameB.UnitDirection;
            double dot = Math.Abs((uA.Ux * uB.Ux) + (uA.Uy * uB.Uy) + (uA.Uz * uB.Uz));

            if (dot < 0.999) return false;

            return DistanceBetween(frameA.EndX, frameA.EndY, frameA.EndZ, frameB.StartX, frameB.StartY, frameB.StartZ) <= toleranceMm
                || DistanceBetween(frameA.EndX, frameA.EndY, frameA.EndZ, frameB.EndX, frameB.EndY, frameB.EndZ) <= toleranceMm
                || DistanceBetween(frameA.StartX, frameA.StartY, frameA.StartZ, frameB.StartX, frameB.StartY, frameB.StartZ) <= toleranceMm
                || DistanceBetween(frameA.StartX, frameA.StartY, frameA.StartZ, frameB.EndX, frameB.EndY, frameB.EndZ) <= toleranceMm;
        }

        private static double DistanceBetween(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
        }
    }
}
