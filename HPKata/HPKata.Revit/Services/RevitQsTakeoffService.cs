using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPKata.Core.QuantitySurveying.Models;
using HPKata.Core.QuantitySurveying.Persistence;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Service querying Revit structural elements and extracting concrete volumes
    /// and formwork quantities into HPKata domain models.
    /// </summary>
    public static class RevitQsTakeoffService
    {
        public static List<QsElementModel> ExtractElements(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var models = new List<QsElementModel>();
            CollectCategory(doc, BuiltInCategory.OST_StructuralColumns, QsMemberType.Column, models);
            CollectCategory(doc, BuiltInCategory.OST_StructuralFraming, QsMemberType.Beam, models);
            CollectCategory(doc, BuiltInCategory.OST_Walls, QsMemberType.Wall, models);
            CollectCategory(doc, BuiltInCategory.OST_Floors, QsMemberType.Slab, models);
            CollectCategory(doc, BuiltInCategory.OST_StructuralFoundation, QsMemberType.Footing, models);
            CollectCategory(doc, BuiltInCategory.OST_Stairs, QsMemberType.Stair, models);

            return models;
        }

        public static QsTakeoffSummary ExtractAndAggregate(Document doc)
        {
            var elements = ExtractElements(doc);
            return QsScheduleAggregator.Aggregate(elements);
        }

        public static void ExportExcel(Document doc, string filePath)
        {
            var summary = ExtractAndAggregate(doc);
            QsWorkbookGenerator.ExportToFile(summary, filePath);
        }

        private static void CollectCategory(
            Document doc,
            BuiltInCategory category,
            QsMemberType memberType,
            List<QsElementModel> targetList)
        {
            var collector = new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType();

            foreach (var elem in collector)
            {
                var model = MapElementToQsModel(elem, memberType);
                if (model != null)
                {
                    targetList.Add(model);
                }
            }
        }

        private static QsElementModel? MapElementToQsModel(Element elem, QsMemberType memberType)
        {
            var bbox = elem.get_BoundingBox(null);
            if (bbox == null) return null;

            double dxFt = Math.Abs(bbox.Max.X - bbox.Min.X);
            double dyFt = Math.Abs(bbox.Max.Y - bbox.Min.Y);
            double dzFt = Math.Abs(bbox.Max.Z - bbox.Min.Z);

            double dimX = RevitUnitConverter.FeetToMm(dxFt);
            double dimY = RevitUnitConverter.FeetToMm(dyFt);
            double dimZ = RevitUnitConverter.FeetToMm(dzFt);

            string levelName = "Level 1";
            var levelParam = elem.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM) ??
                             elem.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            if (levelParam != null && levelParam.HasValue)
            {
                levelName = levelParam.AsString() ?? levelName;
            }

            return new QsElementModel
            {
                Id = elem.Id.ToString(),
                Mark = elem.Name,
                Story = levelName,
                MemberType = memberType,
                LengthMm = Math.Max(dimX, dimY),
                WidthMm = Math.Min(dimX, dimY),
                HeightMm = dimZ,
                ThicknessMm = memberType == QsMemberType.Slab ? dimZ : 0.0,
                PlanAreaMm2 = dimX * dimY
            };
        }
    }
}
