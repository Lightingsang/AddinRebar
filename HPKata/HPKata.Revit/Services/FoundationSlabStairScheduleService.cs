using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;
using HPKata.Core.Stairs.Models;
using HPKata.Core.Stairs.Persistence;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Service collecting structural foundations, floors, and stairs from Revit and exporting ClosedXML schedules.
    /// </summary>
    public static class FoundationSlabStairScheduleService
    {
        public static List<FoundationRebarLayout> CollectFoundationLayouts(Document doc)
        {
            var results = new List<FoundationRebarLayout>();
            if (doc == null) return results;

            var items = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var f in items)
            {
                var bbox = f.get_BoundingBox(null);
                double lMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.X - bbox.Min.X) : 1800.0;
                double bMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.Y - bbox.Min.Y) : 1800.0;
                double hMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.Z - bbox.Min.Z) : 900.0;

                var model = new IsolatedFootingModel
                {
                    FootingMark = f.Name ?? "F1",
                    LengthMm = Math.Max(600.0, lMm),
                    WidthMm = Math.Max(600.0, bMm),
                    TotalHeightMm = Math.Max(300.0, hMm)
                };
                results.Add(FoundationRebarLayout.FromFooting(model));
            }

            return results;
        }

        public static List<SlabRebarLayout> CollectSlabLayouts(Document doc)
        {
            var results = new List<SlabRebarLayout>();
            if (doc == null) return results;

            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .Cast<Floor>()
                .ToList();

            foreach (var floor in floors)
            {
                var bbox = floor.get_BoundingBox(null);
                double lMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.X - bbox.Min.X) : 4000.0;
                double bMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.Y - bbox.Min.Y) : 5000.0;
                double thkMm = bbox != null ? RevitUnitConverter.FeetToMm(bbox.Max.Z - bbox.Min.Z) : 120.0;

                var panel = new SlabPanelModel
                {
                    PanelMark = floor.Name ?? "S1",
                    SpanXMm = Math.Max(1000.0, lMm),
                    SpanYMm = Math.Max(1000.0, bMm),
                    ThicknessMm = Math.Max(80.0, thkMm)
                };
                results.Add(SlabRebarLayout.FromPanel(panel));
            }

            return results;
        }

        public static List<StairReinforcementModel> CollectStairModels(Document doc)
        {
            var results = new List<StairReinforcementModel>();
            if (doc == null) return results;

            var stairs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Stairs)
                .WhereElementIsNotElementType()
                .ToList();

            foreach (var st in stairs)
            {
                var flight = new StairFlightModel
                {
                    FlightMark = st.Name ?? "ST1"
                };
                results.Add(StairReinforcementModel.FromFlight(flight));
            }

            return results;
        }

        public static void ExportFoundationSchedule(Document doc, string filePath)
        {
            var layouts = CollectFoundationLayouts(doc);
            if (layouts.Count == 0)
            {
                layouts.Add(FoundationRebarLayout.FromFooting(new IsolatedFootingModel { FootingMark = "F1" }));
            }
            FoundationPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
        }

        public static void ExportSlabSchedule(Document doc, string filePath)
        {
            var layouts = CollectSlabLayouts(doc);
            if (layouts.Count == 0)
            {
                layouts.Add(SlabRebarLayout.FromPanel(new SlabPanelModel { PanelMark = "S1" }));
            }
            SlabPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
        }

        public static void ExportStairSchedule(Document doc, string filePath)
        {
            var models = CollectStairModels(doc);
            if (models.Count == 0)
            {
                models.Add(StairReinforcementModel.FromFlight(new StairFlightModel { FlightMark = "ST1" }));
            }
            StairPlanScheduleWorkbookGenerator.GenerateWorkbookFile(models, filePath);
        }
    }
}
