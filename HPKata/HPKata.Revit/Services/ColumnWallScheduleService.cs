using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using HPKata.Core.Columns.Models;
using HPKata.Core.Columns.Persistence;
using HPKata.Core.Walls.Models;
using HPKata.Core.Walls.Persistence;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Service collecting structural columns and walls from Revit and exporting ClosedXML schedules.
    /// </summary>
    public static class ColumnWallScheduleService
    {
        public static List<ColumnRebarLayout> CollectColumnLayouts(Document doc)
        {
            var results = new List<ColumnRebarLayout>();
            if (doc == null) return results;

            var cols = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var col in cols)
            {
                var layout = CreateLayoutFromInstance(col);
                results.Add(layout);
            }

            return results;
        }

        public static List<ShearWallModel> CollectWallModels(Document doc)
        {
            var results = new List<ShearWallModel>();
            if (doc == null) return results;

            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(w => w.StructuralUsage != Autodesk.Revit.DB.Structure.StructuralWallUsage.NonBearing)
                .ToList();

            foreach (var wall in walls)
            {
                results.Add(CreateWallFromInstance(wall));
            }

            return results;
        }

        public static void ExportColumnSchedule(Document doc, string filePath)
        {
            var layouts = CollectColumnLayouts(doc);
            ColumnPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
        }

        public static void ExportWallSchedule(Document doc, string filePath)
        {
            var walls = CollectWallModels(doc);
            WallPlanScheduleWorkbookGenerator.GenerateWorkbookFile(walls, filePath);
        }

        private static ColumnRebarLayout CreateLayoutFromInstance(FamilyInstance col)
        {
            string name = col.Name;
            var layout = new ColumnRebarLayout { ColumnMark = name };
            double wMm = 400.0;
            double dMm = 400.0;

            var symbol = col.Symbol;
            if (symbol != null)
            {
                var pB = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH);
                var pH = symbol.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                if (pB != null && pB.HasValue && pB.AsDouble() > 0) wMm = RevitUnitConverter.FeetToMm(pB.AsDouble());
                if (pH != null && pH.HasValue && pH.AsDouble() > 0) dMm = RevitUnitConverter.FeetToMm(pH.AsDouble());
            }

            layout.Stories.Add(new ColumnStorySegment
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                WidthMm = Math.Round(wMm, 0),
                DepthMm = Math.Round(dMm, 0),
                StoryHeightMm = 3600.0,
                Section = new ColumnSectionModel
                {
                    WidthMm = Math.Round(wMm, 0),
                    DepthMm = Math.Round(dMm, 0),
                    CornerDiameterMm = 20,
                    SideCountB = 1,
                    SideDiameterB = 18,
                    SideCountH = 1,
                    SideDiameterH = 18
                }
            });

            return layout;
        }

        private static ShearWallModel CreateWallFromInstance(Wall wall)
        {
            string mark = wall.Name;
            double tw = RevitUnitConverter.FeetToMm(wall.Width);
            double lw = 3000.0;
            if (wall.Location is LocationCurve loc)
            {
                lw = RevitUnitConverter.FeetToMm(loc.Curve.Length);
            }

            var model = new ShearWallModel
            {
                WallMark = mark,
                ThicknessMm = Math.Round(tw, 0),
                LengthMm = Math.Round(lw, 0)
            };

            model.Stories.Add(new WallStorySegment
            {
                StoryIndex = 0,
                StoryName = "Tầng 1",
                ThicknessMm = model.ThicknessMm,
                LengthMm = model.LengthMm,
                StoryHeightMm = 3600.0
            });

            return model;
        }
    }
}
