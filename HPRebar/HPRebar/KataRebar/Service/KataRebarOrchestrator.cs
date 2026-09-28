using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Master coordinator of reinforcement generation for Kata beams.
/// Owns the atomic TransactionGroup, executing phased sub-transactions with failure suppression
/// and assimilating all operations into a single clean Undo step.
/// </summary>
public static class KataRebarOrchestrator
{
    public static KataRebarGenerationResult Execute(
        Document doc,
        IReadOnlyList<FamilyInstance> hostBeams,
        PointMapper mapper,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes,
        KataRebarShapeResolver? shapes,
        IProgress<string>? progress = null)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (hostBeams is null || hostBeams.Count == 0) throw new ArgumentException("No host beams provided.", nameof(hostBeams));
        if (mapper is null) throw new ArgumentNullException(nameof(mapper));
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (layout is null) throw new ArgumentNullException(nameof(layout));

        using var group = new TransactionGroup(doc, $"Kata Rebar - {spec.BeamName}");
        group.Start();

        try
        {
            // Phase 1: Clean up prior Kata rebars (Idempotency)
            int deletedCount = 0;
            using (var t = new Transaction(doc, "Delete Prior Kata Rebar"))
            {
                t.Start();
                RebarFailureHandling.Apply(t);
                deletedCount = KataRebarCleanupService.DeleteExistingKataRebars(doc, hostBeams, spec.BeamName);
                t.Commit();
            }
            progress?.Report($"Đã xoá {deletedCount} thanh thép cũ.");

            // Phase 2: Create Stirrups
            var stirrups = new List<Rebar>();
            using (var t = new Transaction(doc, "Create Kata Stirrups"))
            {
                t.Start();
                RebarFailureHandling.Apply(t);
                stirrups.AddRange(KataRebarCreationService.CreateStirrups(
                    doc, hostBeams, mapper, spec, layout, resolvedBarTypes, shapes));
                t.Commit();
            }
            progress?.Report($"Đã tạo {stirrups.Count} cụm/thanh đai.");

            // Phase 3: Create Main Longitudinal Bars
            var mainBars = new List<Rebar>();
            using (var t = new Transaction(doc, "Create Kata Main Bars"))
            {
                t.Start();
                RebarFailureHandling.Apply(t);
                mainBars.AddRange(KataRebarCreationService.CreateMainBars(
                    doc, hostBeams, mapper, spec, layout, resolvedBarTypes));
                t.Commit();
            }
            progress?.Report($"Đã tạo {mainBars.Count} thanh thép chủ.");

            // Phase 4: Create Additional Bars
            var extraBars = new List<Rebar>();
            using (var t = new Transaction(doc, "Create Kata Additional Bars"))
            {
                t.Start();
                RebarFailureHandling.Apply(t);
                extraBars.AddRange(KataRebarCreationService.CreateAdditionalBars(
                    doc, hostBeams, mapper, spec, layout, resolvedBarTypes));
                t.Commit();
            }
            progress?.Report($"Đã tạo {extraBars.Count} thanh thép gia cường.");

            // Phase 5: Create Side Bars
            var sideBars = new List<Rebar>();
            using (var t = new Transaction(doc, "Create Kata Side Bars"))
            {
                t.Start();
                RebarFailureHandling.Apply(t);
                sideBars.AddRange(KataRebarCreationService.CreateSideBars(
                    doc, hostBeams, mapper, spec, layout, resolvedBarTypes));
                t.Commit();
            }
            progress?.Report($"Đã tạo {sideBars.Count} thanh thép mang / cấu tạo.");

            // Assimilate into a single Undo record in Revit
            group.Assimilate();

            int totalCreated = mainBars.Count + extraBars.Count + sideBars.Count + stirrups.Count;
            return new KataRebarGenerationResult
            {
                IsSuccess = true,
                Message = $"Tạo thép thành công cho dầm {spec.BeamName} ({totalCreated} phần tử rebar).",
                DeletedOldBarsCount = deletedCount,
                CreatedMainBarsCount = mainBars.Count,
                CreatedExtraBarsCount = extraBars.Count,
                CreatedSideBarsCount = sideBars.Count,
                CreatedStirrupsCount = stirrups.Count
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi trong quá trình tạo thép Kata Rebar cho dầm {BeamName}", spec.BeamName);
            if (group.HasStarted())
            {
                group.RollBack();
            }

            return new KataRebarGenerationResult
            {
                IsSuccess = false,
                Message = $"Lỗi khi tạo thép: {ex.Message}"
            };
        }
    }
}
