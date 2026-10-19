using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit
{
    public sealed class RevitFramingElement
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string FamilyAndTypeName { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public string LevelName { get; set; } = string.Empty;
    }

    public sealed class RevitColumnElement
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string FamilyAndTypeName { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }
        public Point3D Location { get; set; }
        public string LevelName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service for bidirectional structural data exchange between Revit 2026 and HPDcePro domain models.
    /// </summary>
    public static class RevitDataExchangeService
    {
        public const double FeetToMmMultiplier = 304.8;
        public const double MmToFeetMultiplier = 1.0 / 304.8;

        public static double FeetToMm(double feet) => feet * FeetToMmMultiplier;
        public static double MmToFeet(double mm) => mm * MmToFeetMultiplier;

        public static RevitFramingElement ExtractFramingElement(FamilyInstance framing)
        {
            if (framing == null) throw new ArgumentNullException(nameof(framing));

            string mark = ReadParameterString(framing, BuiltInParameter.ALL_MODEL_MARK) ?? framing.Name;
            var (b, h) = ExtractCrossSection(framing.Symbol);

            double lengthMm = 0.0;
            var pStart = new Point3D(0, 0, 0);
            var pEnd = new Point3D(0, 0, 0);

            if (framing.Location is LocationCurve locCurve && locCurve.Curve != null)
            {
                lengthMm = FeetToMm(locCurve.Curve.Length);
                var xyz0 = locCurve.Curve.GetEndPoint(0);
                var xyz1 = locCurve.Curve.GetEndPoint(1);
                pStart = new Point3D(FeetToMm(xyz0.X), FeetToMm(xyz0.Y), FeetToMm(xyz0.Z));
                pEnd = new Point3D(FeetToMm(xyz1.X), FeetToMm(xyz1.Y), FeetToMm(xyz1.Z));
            }

            string levelName = ReadLevelName(framing);

            return new RevitFramingElement
            {
                UniqueId = framing.UniqueId,
                Mark = mark,
                FamilyAndTypeName = $"{framing.Symbol.FamilyName}: {framing.Symbol.Name}",
                WidthMm = b > 0 ? b : 220.0,
                HeightMm = h > 0 ? h : 400.0,
                LengthMm = lengthMm,
                StartPoint = pStart,
                EndPoint = pEnd,
                LevelName = levelName
            };
        }

        public static RevitColumnElement ExtractColumnElement(FamilyInstance column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            string mark = ReadParameterString(column, BuiltInParameter.ALL_MODEL_MARK) ?? column.Name;
            var (b, h) = ExtractCrossSection(column.Symbol);

            var pt = new Point3D(0, 0, 0);
            if (column.Location is LocationPoint locPt && locPt.Point != null)
            {
                pt = new Point3D(FeetToMm(locPt.Point.X), FeetToMm(locPt.Point.Y), FeetToMm(locPt.Point.Z));
            }

            double lengthMm = 3600.0;
            var lenParam = column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM);
            if (lenParam != null && lenParam.HasValue)
            {
                lengthMm = FeetToMm(lenParam.AsDouble());
            }

            return new RevitColumnElement
            {
                UniqueId = column.UniqueId,
                Mark = mark,
                FamilyAndTypeName = $"{column.Symbol.FamilyName}: {column.Symbol.Name}",
                WidthMm = b > 0 ? b : 220.0,
                HeightMm = h > 0 ? h : 220.0,
                LengthMm = lengthMm,
                Location = pt,
                LevelName = ReadLevelName(column)
            };
        }

        public static List<RevitFramingElement> CollectAllFramingElements(Document doc)
        {
            if (doc == null) return new List<RevitFramingElement>();

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilyInstance>()
                .Where(fi => fi.Symbol != null)
                .Select(ExtractFramingElement)
                .ToList();
        }

        public static List<RevitColumnElement> CollectAllColumnElements(Document doc)
        {
            if (doc == null) return new List<RevitColumnElement>();

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .Cast<FamilyInstance>()
                .Where(fi => fi.Symbol != null)
                .Select(ExtractColumnElement)
                .ToList();
        }

        private static (double WidthMm, double HeightMm) ExtractCrossSection(FamilySymbol symbol)
        {
            double b = ReadParameterDouble(symbol, "b") ?? ReadParameterDouble(symbol, "Width") ?? 0.0;
            double h = ReadParameterDouble(symbol, "h") ?? ReadParameterDouble(symbol, "Height") ?? 0.0;
            return (FeetToMm(b), FeetToMm(h));
        }

        private static double? ReadParameterDouble(Element element, string paramName)
        {
            var p = element.LookupParameter(paramName);
            if (p != null && p.HasValue && p.StorageType == StorageType.Double)
            {
                return p.AsDouble();
            }
            return null;
        }

        private static string? ReadParameterString(Element element, BuiltInParameter bip)
        {
            var p = element.get_Parameter(bip);
            return p != null && p.HasValue ? p.AsString() : null;
        }

        private static string ReadLevelName(FamilyInstance fi)
        {
            var levelParam = fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (levelParam != null && levelParam.HasValue)
            {
                return levelParam.AsValueString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
