using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.AutoStr.Steps;

namespace HPDcePro.Core.Calculations.Takeoff
{
    /// <summary>
    /// Computes multi-story structural quantity takeoff: concrete (m3), formwork (m2), and rebar (kg).
    /// </summary>
    public static class QuantityTakeoffCalculator
    {
        public static double GetUnitWeightKgPerMeter(double diameterMm)
        {
            return 0.006165376 * diameterMm * diameterMm;
        }

        public static RebarDiameterCategory CategorizeDiameter(double diameterMm)
        {
            if (diameterMm <= 10.0) return RebarDiameterCategory.SmallLe10;
            if (diameterMm <= 18.0) return RebarDiameterCategory.MediumLe18;
            return RebarDiameterCategory.LargeGt18;
        }

        public static QuantityTakeoffResult CalculateTakeoff(AutoStrStepContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var elements = new List<ElementTakeoffItem>();
            var rebars = new List<RebarTakeoffItem>();

            var storyNames = GetStoryNames(context);
            foreach (var story in storyNames)
            {
                if (story.IndexOf("Móng", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AddFootingTakeoff(context, story, elements, rebars);
                }
                else
                {
                    AddSuperstructureTakeoff(context, story, elements, rebars);
                }
            }

            return CalculateFromElements(elements, rebars);
        }

        public static QuantityTakeoffResult CalculateFromElements(
            IEnumerable<ElementTakeoffItem> elements,
            IEnumerable<RebarTakeoffItem> rebars)
        {
            var elList = elements?.ToList() ?? new List<ElementTakeoffItem>();
            var rbList = rebars?.ToList() ?? new List<RebarTakeoffItem>();

            var storyMap = new Dictionary<string, StoryTakeoffSummary>();

            foreach (var el in elList)
            {
                var summary = GetOrCreateStorySummary(storyMap, el.StoryName);
                summary.ConcreteVolumeM3 += el.ConcreteVolumeM3;
                summary.FormworkAreaM2 += el.FormworkAreaM2;
            }

            foreach (var rb in rbList)
            {
                var summary = GetOrCreateStorySummary(storyMap, rb.StoryName);
                if (rb.Category == RebarDiameterCategory.SmallLe10)
                    summary.RebarKgLe10 += rb.WeightKg;
                else if (rb.Category == RebarDiameterCategory.MediumLe18)
                    summary.RebarKgLe18 += rb.WeightKg;
                else
                    summary.RebarKgGt18 += rb.WeightKg;
            }

            var result = new QuantityTakeoffResult
            {
                Elements = elList,
                Rebars = rbList,
                StorySummaries = storyMap.Values.ToList(),
                TotalConcreteVolumeM3 = Math.Round(elList.Sum(e => e.ConcreteVolumeM3), 3),
                TotalFormworkAreaM2 = Math.Round(elList.Sum(e => e.FormworkAreaM2), 2),
                TotalRebarKgLe10 = Math.Round(rbList.Where(r => r.Category == RebarDiameterCategory.SmallLe10).Sum(r => r.WeightKg), 2),
                TotalRebarKgLe18 = Math.Round(rbList.Where(r => r.Category == RebarDiameterCategory.MediumLe18).Sum(r => r.WeightKg), 2),
                TotalRebarKgGt18 = Math.Round(rbList.Where(r => r.Category == RebarDiameterCategory.LargeGt18).Sum(r => r.WeightKg), 2)
            };

            return result;
        }

        private static List<string> GetStoryNames(AutoStrStepContext context)
        {
            if (context.Config.Floors != null && context.Config.Floors.Count > 0)
            {
                var list = new List<string> { "TẦNG MÓNG" };
                list.AddRange(context.Config.Floors.Select(f => f.TenTang));
                return list;
            }
            return new List<string> { "TẦNG MÓNG", "TẦNG 1", "TẦNG 2", "TẦNG 3", "TẦNG MÁI" };
        }

        private static StoryTakeoffSummary GetOrCreateStorySummary(Dictionary<string, StoryTakeoffSummary> map, string storyName)
        {
            if (!map.TryGetValue(storyName, out var summary))
            {
                summary = new StoryTakeoffSummary { StoryName = storyName };
                map[storyName] = summary;
            }
            return summary;
        }

        private static ElementTakeoffItem CreateElement(
            string story, TakeoffElementType type, string mark, int count,
            double w, double h, double l, double vol, double form) =>
            new ElementTakeoffItem
            {
                StoryName = story,
                ElementType = type,
                Mark = mark,
                Count = count,
                WidthMm = w,
                HeightMm = h,
                LengthMm = l,
                ConcreteVolumeM3 = vol,
                FormworkAreaM2 = form
            };

        private static RebarTakeoffItem CreateRebar(
            string story, TakeoffElementType type, string mark, double d, double lenM) =>
            new RebarTakeoffItem
            {
                StoryName = story,
                ElementType = type,
                Mark = mark,
                DiameterMm = d,
                TotalLengthM = lenM,
                WeightKg = lenM * GetUnitWeightKgPerMeter(d),
                Category = CategorizeDiameter(d)
            };

        private static void AddFootingTakeoff(
            AutoStrStepContext context,
            string story,
            List<ElementTakeoffItem> elements,
            List<RebarTakeoffItem> rebars)
        {
            var caps = context.GetArtifact<List<PileCapLayoutDefinition>>("PileCapDefinitions");
            int count = caps != null ? caps.Count : 12;

            double b = 1.4, l = 1.4, h = 0.8;
            elements.Add(CreateElement(story, TakeoffElementType.Footing, "ĐÀI CỌC", count,
                b * 1000.0, h * 1000.0, l * 1000.0, count * (b * l * h), count * 2.0 * (b + l) * h));

            rebars.Add(CreateRebar(story, TakeoffElementType.Footing, "Thép đài d14", 14.0, count * 39.2));
        }

        private static void AddSuperstructureTakeoff(
            AutoStrStepContext context,
            string story,
            List<ElementTakeoffItem> elements,
            List<RebarTakeoffItem> rebars)
        {
            AddColumnTakeoff(context, story, elements, rebars);
            AddBeamTakeoff(context, story, elements, rebars);
            AddSlabTakeoff(context, story, elements, rebars);
        }

        private static void AddColumnTakeoff(
            AutoStrStepContext context,
            string story,
            List<ElementTakeoffItem> elements,
            List<RebarTakeoffItem> rebars)
        {
            var cols = context.GetArtifact<List<ColumnSectionDefinition>>("ColumnDefinitions");
            int count = cols != null ? cols.Count : 12;
            double w = (cols != null && cols.Count > 0) ? cols[0].WidthMm : 300.0;
            double h = (cols != null && cols.Count > 0) ? cols[0].HeightMm : 300.0;
            double len = 3600.0;

            double vol = count * (w / 1000.0) * (h / 1000.0) * (len / 1000.0);
            double form = count * 2.0 * ((w + h) / 1000.0) * (len / 1000.0);

            elements.Add(CreateElement(story, TakeoffElementType.Column, "CỘT", count, w, h, len, vol, form));
            rebars.Add(CreateRebar(story, TakeoffElementType.Column, "Thép chủ cột d20", 20.0, count * 4 * (len / 1000.0)));
            rebars.Add(CreateRebar(story, TakeoffElementType.Column, "Đai cột d8", 8.0, count * 24 * (4 * w / 1000.0)));
        }

        private static void AddBeamTakeoff(
            AutoStrStepContext context,
            string story,
            List<ElementTakeoffItem> elements,
            List<RebarTakeoffItem> rebars)
        {
            int count = context.Topology.BeamCenterlines.Count;
            if (count == 0) count = 12;
            double w = context.Config.BeRongDamDienHinh;
            double h = context.Config.ChieuCaoDamDienHinh;
            double span = 5000.0;
            double slabH = context.Config.ChieuDaySanDienHinh;

            double vol = count * (w / 1000.0) * (h / 1000.0) * (span / 1000.0);
            double form = count * (2.0 * ((h - slabH) / 1000.0) + (w / 1000.0)) * (span / 1000.0);

            elements.Add(CreateElement(story, TakeoffElementType.Beam, "DẦM", count, w, h, span, vol, form));
            rebars.Add(CreateRebar(story, TakeoffElementType.Beam, "Thép chủ dầm d18", 18.0, count * 4 * (span / 1000.0)));
        }

        private static void AddSlabTakeoff(
            AutoStrStepContext context,
            string story,
            List<ElementTakeoffItem> elements,
            List<RebarTakeoffItem> rebars)
        {
            double slabArea = 120.0; // m2 typical
            double slabH = context.Config.ChieuDaySanDienHinh / 1000.0;
            double vol = slabArea * slabH;
            double form = slabArea + 2.0 * (12.0 + 10.0) * slabH;

            elements.Add(CreateElement(story, TakeoffElementType.Slab, "SÀN", 1, 10000.0, slabH * 1000.0, 12000.0, vol, form));
            rebars.Add(CreateRebar(story, TakeoffElementType.Slab, "Thép sàn d10", 10.0, slabArea * 12.0));
        }
    }
}
