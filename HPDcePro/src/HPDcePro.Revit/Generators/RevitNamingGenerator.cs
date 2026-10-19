using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record BeamNamingItem(
        string UniqueId,
        string CurrentMark,
        Point3D StartMm,
        Point3D EndMm,
        string LevelName);

    public sealed record BeamNamingResultItem(
        string UniqueId,
        string CurrentMark,
        string ProposedMark,
        string StoryPrefix,
        int SequenceNumber);

    public static class RevitNamingGenerator
    {
        public static IReadOnlyList<BeamNamingResultItem> GenerateNames(
            IEnumerable<BeamNamingItem> beams,
            string prefix = "D")
        {
            if (beams == null) return Array.Empty<BeamNamingResultItem>();

            var results = new List<BeamNamingResultItem>();
            var byLevel = beams.GroupBy(b => b.LevelName ?? "L1");

            foreach (var levelGroup in byLevel)
            {
                string story = levelGroup.Key;
                var beamList = levelGroup.ToList();

                // Sort beams geometrically: horizontal beams (dx > dy) first from North to South (Y descending) then West to East (X ascending)
                // then vertical beams (dy >= dx) from West to East (X ascending) then North to South (Y descending)
                var sorted = beamList.OrderBy(b =>
                {
                    double dx = Math.Abs(b.EndMm.X - b.StartMm.X);
                    double dy = Math.Abs(b.EndMm.Y - b.StartMm.Y);
                    bool isHorizontal = dx >= dy;
                    return isHorizontal ? 0 : 1;
                })
                .ThenByDescending(b => Math.Max(b.StartMm.Y, b.EndMm.Y))
                .ThenBy(b => Math.Min(b.StartMm.X, b.EndMm.X))
                .ToList();

                int seq = 1;
                foreach (var item in sorted)
                {
                    string proposed = string.Format(CultureInfo.InvariantCulture, "{0}{1}-{2}", story, prefix, seq);
                    results.Add(new BeamNamingResultItem(
                        item.UniqueId,
                        item.CurrentMark,
                        proposed,
                        story,
                        seq));
                    seq++;
                }
            }

            return results;
        }
    }

    public static class BeamNamingGenerator
    {
        public static IReadOnlyList<BeamNamingResultItem> GenerateNames(
            IEnumerable<BeamNamingItem> beams,
            string prefix = "D") =>
            RevitNamingGenerator.GenerateNames(beams, prefix);
    }
}
