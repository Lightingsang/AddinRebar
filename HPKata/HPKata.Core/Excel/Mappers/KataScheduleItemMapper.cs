using System;
using System.Collections.Generic;
using HPKata.Core.Excel.Data;
using HPKata.Core.Models;

namespace HPKata.Core.Excel.Mappers
{
    /// <summary>
    /// Maps between core RebarItem instances and KataRebarScheduleItem wire/sheet DTOs.
    /// </summary>
    public static class KataScheduleItemMapper
    {
        public static KataRebarScheduleItem ToScheduleItem(RebarItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return new KataRebarScheduleItem
            {
                ComponentMark = item.ComponentMark,
                BarMark = item.BarMark,
                ShapeCode = item.ShapeCode,
                SegmentsMm = item.ParametersMm ?? Array.Empty<double>(),
                Notes = item.Notes,
                DiameterMm = item.DiameterMm,
                UnlappedLengthMm = item.StructuralLengthMm,
                CuttingLengthMm = item.CuttingLengthMm,
                MemberCount = item.MemberCount,
                CountPerMember = item.CountPerMember,
                UnitMassKgPerM = item.UnitMassKgPerM,
                IsCoupler = string.Equals(item.ShapeCode, "coupler", StringComparison.OrdinalIgnoreCase) ||
                            item.Notes.IndexOf("coupler", StringComparison.OrdinalIgnoreCase) >= 0,
                SubLayer = item.SubLayer,
                FloorElevation = item.FloorElevation
            };
        }

        public static RebarItem ToRebarItem(KataRebarScheduleItem dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new RebarItem
            {
                ComponentMark = dto.ComponentMark,
                BarMark = dto.BarMark,
                ShapeCode = dto.ShapeCode,
                ParametersMm = dto.SegmentsMm ?? Array.Empty<double>(),
                Notes = dto.Notes,
                DiameterMm = dto.DiameterMm,
                StructuralLengthMm = dto.UnlappedLengthMm,
                CuttingLengthMm = dto.CuttingLengthMm,
                MemberCount = dto.MemberCount,
                CountPerMember = dto.CountPerMember,
                UnitMassKgPerM = dto.UnitMassKgPerM,
                SubLayer = dto.SubLayer,
                FloorElevation = dto.FloorElevation
            };
        }

        public static List<KataRebarScheduleItem> ToScheduleItemList(IEnumerable<RebarItem> items)
        {
            var list = new List<KataRebarScheduleItem>();
            if (items == null) return list;

            foreach (var item in items)
            {
                list.Add(ToScheduleItem(item));
            }
            return list;
        }
    }
}
