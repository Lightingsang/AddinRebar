using System;
using HPKata.Core.Excel.Data;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Excel.Mappers
{
    /// <summary>
    /// Bi-directional mapper between KataWallSheetData and ShearWallModel.
    /// </summary>
    public static class KataWallDataMapper
    {
        public static ShearWallModel ToShearWallModel(KataWallSheetData sheetData)
        {
            if (sheetData == null) throw new ArgumentNullException(nameof(sheetData));

            var wall = new ShearWallModel
            {
                WallMark = sheetData.WallMark,
                ShapeType = sheetData.ShapeType,
                ThicknessMm = sheetData.ThicknessMm,
                LengthMm = sheetData.LengthMm,
                ConcreteCoverMm = sheetData.ConcreteCoverMm,
                ConcreteGrade = sheetData.ConcreteGrade
            };

            double currentElevation = 0.0;
            for (int i = 0; i < sheetData.Stories.Count; i++)
            {
                var s = sheetData.Stories[i];
                wall.Stories.Add(new WallStorySegment
                {
                    StoryIndex = s.StoryIndex,
                    StoryName = s.StoryName,
                    BottomElevationMm = currentElevation,
                    StoryHeightMm = s.StoryHeightM * 1000.0,
                    ThicknessMm = s.ThicknessTwMm,
                    LengthMm = s.LengthLwMm
                });
                currentElevation += s.StoryHeightM * 1000.0;
            }

            if (sheetData.HasBoundaryElements)
            {
                wall.BoundaryElements.Add(CreateBoundaryElement("BE-L", WallBoundaryPosition.Left, sheetData));
                wall.BoundaryElements.Add(CreateBoundaryElement("BE-R", WallBoundaryPosition.Right, sheetData));
            }

            wall.WebReinforcement = new WallWebReinforcementModel
            {
                IsDoubleCurtain = sheetData.IsDoubleCurtain,
                VerticalBarDiameterMm = sheetData.WebVerticalDiameter,
                VerticalBarSpacingMm = sheetData.WebVerticalSpacing,
                HorizontalBarDiameterMm = sheetData.WebHorizontalDiameter,
                HorizontalBarSpacingMm = sheetData.WebHorizontalSpacing,
                CrossTieDiameterMm = sheetData.CrossTieDiameter,
                HasUBarEdgeCaps = sheetData.HasUBarEdgeCaps
            };

            return wall;
        }

        public static KataWallSheetData ToSheetData(ShearWallModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var data = new KataWallSheetData
            {
                WallMark = model.WallMark,
                ShapeType = model.ShapeType,
                ThicknessMm = model.ThicknessMm,
                LengthMm = model.LengthMm,
                ConcreteCoverMm = model.ConcreteCoverMm,
                ConcreteGrade = model.ConcreteGrade,
                HasBoundaryElements = model.BoundaryElements.Count > 0,
                IsDoubleCurtain = model.WebReinforcement.IsDoubleCurtain,
                WebVerticalDiameter = model.WebReinforcement.VerticalBarDiameterMm,
                WebVerticalSpacing = model.WebReinforcement.VerticalBarSpacingMm,
                WebHorizontalDiameter = model.WebReinforcement.HorizontalBarDiameterMm,
                WebHorizontalSpacing = model.WebReinforcement.HorizontalBarSpacingMm,
                CrossTieDiameter = model.WebReinforcement.CrossTieDiameterMm,
                HasUBarEdgeCaps = model.WebReinforcement.HasUBarEdgeCaps
            };

            foreach (var seg in model.Stories)
            {
                data.Stories.Add(new KataWallStoryData
                {
                    StoryIndex = seg.StoryIndex,
                    StoryName = seg.StoryName,
                    StoryHeightM = seg.StoryHeightMm / 1000.0,
                    LengthLwMm = seg.LengthMm,
                    ThicknessTwMm = seg.ThicknessMm
                });
            }

            return data;
        }

        private static WallBoundaryElementModel CreateBoundaryElement(
            string mark, WallBoundaryPosition pos, KataWallSheetData data)
        {
            return new WallBoundaryElementModel
            {
                Mark = mark,
                Position = pos,
                LengthBbMm = data.BoundaryLengthMm,
                ThicknessTbMm = data.ThicknessMm,
                VerticalBarCount = data.BoundaryBarCount,
                VerticalBarDiameterMm = data.BoundaryBarDiameter,
                TieDiameterMm = data.BoundaryTieDiameter,
                TieSpacingMm = data.BoundaryTieSpacing
            };
        }
    }
}
