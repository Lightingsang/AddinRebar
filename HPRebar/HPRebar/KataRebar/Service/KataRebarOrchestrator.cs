using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Draws a plan in one undo step "Kata Rebar - {beam}": deletes the bars of the previous run, gives the beams the
/// stirrup cover of J9, creates the stirrup sets, then the main bars, then gives every bar its Kata number as Rebar Number. Each step is its own transaction; a step that fails rolls the whole
/// group back, so the model is either fully updated or untouched.
/// </summary>
public static class KataRebarOrchestrator
{
    public static KataRebarGenerationResult Execute(
        Document doc,
        KataBeamMatchResult match,
        KataRebarPlan plan,
        IReadOnlyDictionary<double, RebarBarType> barTypes,
        RebarShape? stirrupShape)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (match?.Run is null) throw new ArgumentException("The beam run was not measured.", nameof(match));
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        string name = string.IsNullOrWhiteSpace(plan.Spec.BeamName) ? "dầm" : plan.Spec.BeamName.Trim();
        var hosts = match.Run.Pieces.Select(p => (Element)p.Element).ToList();
        var placement = new KataBeamPlacement(match, plan.Reversed);
        var runner = new KataTransactionRunner(doc);
        KataRebarLog.Plan(match, plan, stirrupShape);

        using var group = new TransactionGroup(doc, $"Kata Rebar - {name}");
        group.Start();
        try
        {
            int deleted = runner.Run("Kata Rebar: xoá thép cũ", () => KataRebarCleanupService.DeletePrevious(doc, hosts));
            var coverWarnings = runner.Run("Kata Rebar: lớp bảo vệ dầm", () => KataHostCoverService.Apply(doc, hosts, plan.Rules.StirrupCover));
            var stirrups = plan.Layout.StirrupZones.Count > 0
                ? runner.Run("Kata Rebar: đai", () => KataStirrupSetCreator.Create(doc, plan, placement, stirrupShape, barTypes))
                : new KataStirrupOutcome(0, 0);
            var longitudinal = runner.Run("Kata Rebar: thép dọc", () => KataRebarCreationService.CreateLongitudinalBars(doc, plan, placement, barTypes));
            var barSets = plan.Layout.BarSets.Count > 0
                ? runner.Run("Kata Rebar: móc C, đai trong", () => KataBarSetCreator.Create(doc, plan, placement, barTypes))
                : new KataBarSetOutcome(0, 0, Array.Empty<string>());
            var numberWarnings = runner.Run("Kata Rebar: số hiệu", () => KataRebarNumberAssigner.Apply(doc, hosts));
            int extraTop = plan.Layout.ExtraTopBars.Count;
            int extraBottom = plan.Layout.ExtraBottomBars.Count;
            int sideBars = plan.Layout.SideBars.Count;
            int mainBars = longitudinal.Bars - extraTop - extraBottom - sideBars;

            var status = group.Assimilate();
            if (status != TransactionStatus.Committed)
                throw new InvalidOperationException($"Revit không gộp được các bước thành một lần Undo ({status}).");

            var result = new KataRebarGenerationResult
            {
                IsSuccess = true,
                Message = $"Đã vẽ dầm {name}: {mainBars} thanh thép chủ"
                          + (extraTop > 0 ? $", {extraTop} thanh gia cường gối" : "")
                          + (extraBottom > 0 ? $", {extraBottom} thanh gia cường nhịp" : "")
                          + (sideBars > 0 ? $", {sideBars} thanh cốt giá" : "")
                          + $" (thép dọc: {longitudinal.Sets} bộ Fixed Number, {longitudinal.Elements - longitudinal.Sets} thanh Single{(longitudinal.FallbackSets > 0 ? $", {longitudinal.FallbackSets} bộ Revit không rải được nên vẽ từng thanh" : "")})"
                          + (barSets.Sets > 0 ? $", {barSets.Sets} bộ móc C / đai trong ({barSets.Bars} thanh)" : "")
                          + $", {stirrups.Sets} bộ đai"
                          + (stirrups.SingleBars > 0 ? $" + {stirrups.SingleBars} đai lẻ" : "")
                          + (deleted > 0 ? $"; xoá {deleted} phần tử thép của lần chạy trước." : ".")
                          + (coverWarnings.Count > 0 ? " " + string.Join(" ", coverWarnings) : "")
                          + (barSets.Warnings.Count > 0 ? " " + string.Join(" ", barSets.Warnings) : "")
                          + (numberWarnings.Count > 0 ? " " + string.Join(" ", numberWarnings) : ""),
                DeletedCount = deleted,
                MainBarCount = mainBars,
                ExtraTopBarCount = extraTop,
                ExtraBottomBarCount = extraBottom,
                SideBarCount = sideBars,
                BarSetCount = barSets.Sets,
                StirrupSetCount = stirrups.Sets,
                StirrupSingleBarCount = stirrups.SingleBars,
                RevitWarnings = runner.Warnings.Distinct().ToList()
            };
            KataRebarLog.Result(name, result);
            return result;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException
                                       or Autodesk.Revit.Exceptions.ApplicationException)
        {
            if (group.GetStatus() == TransactionStatus.Started) group.RollBack();
            Log.Error(ex, "Kata Rebar: drawing beam {Beam} failed and was rolled back ({Reason})", name, ex.Message);
            return KataRebarGenerationResult.Failed($"Không vẽ được dầm {name} (đã hoàn tác): {ex.Message}");
        }
    }
}
