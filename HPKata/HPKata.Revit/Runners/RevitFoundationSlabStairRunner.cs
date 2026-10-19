using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPKata.Core.Foundations.Models;
using HPKata.Core.Foundations.Persistence;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;
using HPKata.Core.Stairs.Models;
using HPKata.Core.Stairs.Persistence;
using HPKata.Revit.Abstractions;

namespace HPKata.Revit.Runners
{
    /// <summary>
    /// Headless runner exporting foundation, slab, and stair schedules from document context.
    /// </summary>
    public static class RevitFoundationSlabStairRunner
    {
        public static string ExportFoundationSchedule(IRevitDocumentContext context, string filePath)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            var rebars = context.GetAllRebars()
                .Where(r => r.Partition.StartsWith("Mong", StringComparison.OrdinalIgnoreCase) ||
                            r.Partition.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var layouts = new List<FoundationRebarLayout>();
            if (rebars.Count > 0)
            {
                var grouped = rebars.GroupBy(r => r.Partition);
                foreach (var g in grouped)
                {
                    layouts.Add(FoundationRebarLayout.FromFooting(new IsolatedFootingModel
                    {
                        FootingMark = g.Key,
                        MemberCount = g.Count()
                    }));
                }
            }
            else
            {
                layouts.Add(FoundationRebarLayout.FromFooting(new IsolatedFootingModel { FootingMark = "F1" }));
            }

            FoundationPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
            context.WriteNotification($"Đã xuất bảng thống kê móng ra file: {filePath}");
            return filePath;
        }

        public static string ExportSlabSchedule(IRevitDocumentContext context, string filePath)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            var rebars = context.GetAllRebars()
                .Where(r => r.Partition.StartsWith("San", StringComparison.OrdinalIgnoreCase) ||
                            r.Partition.StartsWith("S", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var layouts = new List<SlabRebarLayout>();
            if (rebars.Count > 0)
            {
                var grouped = rebars.GroupBy(r => r.Partition);
                foreach (var g in grouped)
                {
                    layouts.Add(SlabRebarLayout.FromPanel(new SlabPanelModel
                    {
                        PanelMark = g.Key
                    }));
                }
            }
            else
            {
                layouts.Add(SlabRebarLayout.FromPanel(new SlabPanelModel { PanelMark = "S1" }));
            }

            SlabPlanScheduleWorkbookGenerator.GenerateWorkbookFile(layouts, filePath);
            context.WriteNotification($"Đã xuất bảng thống kê sàn ra file: {filePath}");
            return filePath;
        }

        public static string ExportStairSchedule(IRevitDocumentContext context, string filePath)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            var rebars = context.GetAllRebars()
                .Where(r => r.Partition.StartsWith("Thang", StringComparison.OrdinalIgnoreCase) ||
                            r.Partition.StartsWith("ST", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var models = new List<StairReinforcementModel>();
            if (rebars.Count > 0)
            {
                var grouped = rebars.GroupBy(r => r.Partition);
                foreach (var g in grouped)
                {
                    models.Add(StairReinforcementModel.FromFlight(new StairFlightModel
                    {
                        FlightMark = g.Key
                    }));
                }
            }
            else
            {
                models.Add(StairReinforcementModel.FromFlight(new StairFlightModel { FlightMark = "ST1" }));
            }

            StairPlanScheduleWorkbookGenerator.GenerateWorkbookFile(models, filePath);
            context.WriteNotification($"Đã xuất bảng thống kê thang ra file: {filePath}");
            return filePath;
        }
    }
}
