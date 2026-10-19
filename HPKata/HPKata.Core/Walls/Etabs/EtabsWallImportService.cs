using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Walls.Calculators;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Etabs
{
    /// <summary>
    /// Service converting imported ETABS wall pier designs into fully detailed ShearWallModels.
    /// </summary>
    public sealed class EtabsWallImportService
    {
        public List<ShearWallModel> ImportFromTableText(string tableText)
        {
            var designs = EtabsWallTableMapper.MapTableContent(tableText);
            return ProcessDesigns(designs);
        }

        public List<ShearWallModel> ProcessDesigns(List<EtabsPierDesign> designs)
        {
            var walls = new List<ShearWallModel>();
            if (designs == null || designs.Count == 0) return walls;

            var groupedByMark = designs.GroupBy(d => d.PierMark, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedByMark)
            {
                var wall = BuildWallModel(group.Key, group.ToList());
                walls.Add(wall);
            }

            return walls;
        }

        private static ShearWallModel BuildWallModel(string mark, List<EtabsPierDesign> storyDesigns)
        {
            var wall = new ShearWallModel { WallMark = mark };
            if (storyDesigns.Count == 0) return wall;

            var first = storyDesigns[0];
            wall.ThicknessMm = first.ThicknessMm;
            wall.LengthMm = first.LengthMm;

            double currentElevation = 0.0;
            for (int i = 0; i < storyDesigns.Count; i++)
            {
                var d = storyDesigns[i];
                var story = new WallStorySegment
                {
                    StoryIndex = i,
                    StoryName = string.IsNullOrWhiteSpace(d.StoryName) ? $"Story{i + 1}" : d.StoryName,
                    BottomElevationMm = currentElevation,
                    StoryHeightMm = d.HeightMm > 0.0 ? d.HeightMm : 3600.0,
                    ThicknessMm = d.ThicknessMm,
                    LengthMm = d.LengthMm
                };
                wall.Stories.Add(story);
                currentElevation += story.StoryHeightMm;
            }

            double maxAsb = storyDesigns.Max(d => d.RequiredBoundaryAsbMm2);
            var leftBE = WallBoundaryCalculator.CreateBoundaryElement(
                "BE-L", WallBoundaryPosition.Left, wall.LengthMm, wall.ThicknessMm, maxAsb);
            var rightBE = WallBoundaryCalculator.CreateBoundaryElement(
                "BE-R", WallBoundaryPosition.Right, wall.LengthMm, wall.ThicknessMm, maxAsb);

            wall.BoundaryElements.Add(leftBE);
            wall.BoundaryElements.Add(rightBE);

            wall.WebReinforcement = CreateDefaultWebReinforcement(wall.ThicknessMm);

            return wall;
        }

        private static WallWebReinforcementModel CreateDefaultWebReinforcement(double thicknessMm)
        {
            return new WallWebReinforcementModel
            {
                IsDoubleCurtain = thicknessMm >= 150.0,
                VerticalBarDiameterMm = 12,
                VerticalBarSpacingMm = 200.0,
                HorizontalBarDiameterMm = 12,
                HorizontalBarSpacingMm = 200.0,
                CrossTieDiameterMm = 8,
                CrossTiesPerSquareMeter = 4.0,
                HasUBarEdgeCaps = true,
                UBarDiameterMm = 12,
                UBarLapLengthMm = 400.0
            };
        }
    }
}
