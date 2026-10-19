using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPKata.Core.Models;
using HPKata.Revit.Units;

namespace HPKata.Revit.Services
{
    /// <summary>
    /// Service querying framing rebar quantities and managing beam rebar schedules.
    /// </summary>
    public static class FramingScheduleService
    {
        public static List<RebarItem> ExtractFramingRebarTakeoff(Document doc, string partitionName)
        {
            var items = new List<RebarItem>();
            if (doc == null) return items;

            var rebars = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r =>
                {
                    var pPart = r.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString();
                    return string.Equals(pPart, partitionName, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            foreach (var rebar in rebars)
            {
                var barType = doc.GetElement(rebar.GetTypeId()) as RebarBarType;
                double diaMm = barType != null ? RevitUnitConverter.FeetToMm(barType.BarModelDiameter) : 20.0;
                double totalLenMm = RevitUnitConverter.FeetToMm(rebar.TotalLength);

                string mark = rebar.get_Parameter(BuiltInParameter.REBAR_NUMBER)?.AsString() ?? "1";
                int count = rebar.Quantity > 0 ? rebar.Quantity : 1;

                items.Add(new RebarItem
                {
                    ComponentMark = partitionName,
                    BarMark = mark,
                    DiameterMm = (int)Math.Round(diaMm),
                    CountPerMember = count,
                    MemberCount = 1,
                    CuttingLengthMm = Math.Round(totalLenMm / count, 1),
                    StructuralLengthMm = Math.Round(totalLenMm / count, 1)
                });
            }

            return items;
        }
    }
}
