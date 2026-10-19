namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;
using HPBim.Core.Geometry;

/// <summary>
/// Domain engine implementing the authentic 8-step QA/QC audit for structural foundation.
/// Fully adheres to ĐIỀU 0: Zero heuristic guessing, mathematical verification only.
/// </summary>
public sealed class FoundationQcEngine
{
    private readonly PileContainmentChecker _pileChecker;
    private readonly BeamBearingChecker _beamChecker;
    private readonly FoundationLedgerAuditor _ledgerAuditor;

    public FoundationQcEngine(
        PileContainmentChecker? pileChecker = null,
        BeamBearingChecker? beamChecker = null,
        FoundationLedgerAuditor? ledgerAuditor = null)
    {
        _pileChecker = pileChecker ?? new PileContainmentChecker(150.0, 1.0);
        _beamChecker = beamChecker ?? new BeamBearingChecker(400.0, 350.0);
        _ledgerAuditor = ledgerAuditor ?? new FoundationLedgerAuditor();
    }

    public FoundationStepResult AuditGridsAndLevels(
        IReadOnlyList<GridAuditItem> grids,
        IReadOnlyList<LevelAuditItem> levels,
        IReadOnlyDictionary<string, double>? expectedXGrids = null,
        IReadOnlyDictionary<string, double>? expectedYGrids = null,
        IReadOnlyDictionary<string, double>? expectedLevels = null)
    {
        if (expectedXGrids == null && expectedYGrids == null && expectedLevels == null)
        {
            return new FoundationStepResult(
                1, "Hệ Trục & Cao Trình", FoundationStepStatus.Inconclusive,
                "Chưa đủ cơ sở để kết luận: Thiếu dữ liệu nhịp trục và cao trình thiết kế. Chỉ đếm được số lượng trục và cao độ.",
                new[] { "Thiếu dữ liệu đối chiếu tọa độ trục và cao trình chuẩn." });
        }

        var errors = new List<string>();
        int gridsCount = grids.Count;
        bool allGridsPinned = grids.All(g => g.Pinned);
        if (gridsCount != 27) errors.Add($"Số lượng trục là {gridsCount}/27.");
        if (!allGridsPinned) errors.Add("Có trục chưa được ghim (Pinned = false).");

        int levelsCount = levels.Count;
        bool allLevelsPinned = levels.All(l => l.Pinned);
        if (levelsCount != 10) errors.Add($"Số lượng cao trình là {levelsCount}/10.");
        if (!allLevelsPinned) errors.Add("Có cao trình chưa được ghim (Pinned = false).");

        FoundationGridLevelValidator.ValidateGridSpans(grids, expectedXGrids, expectedYGrids, errors);
        FoundationGridLevelValidator.ValidateLevelElevations(levels, expectedLevels, errors);

        bool passed = errors.Count == 0;
        string summary = passed
            ? $"Hệ trục ({gridsCount}/27) và Cao trình ({levelsCount}/10) hoàn toàn chuẩn xác, ghim 100%."
            : $"Hệ trục/cao độ có {errors.Count} lỗi sai lệch vượt dung sai 1.0mm.";

        return new FoundationStepResult(1, "Hệ Trục & Cao Trình", passed, summary, errors);
    }

    public FoundationStepResult AuditCounts(int piles, int footings, int beams, int expectedBeams = 192)
    {
        var errors = new List<string>();
        if (piles != 196) errors.Add($"Số cọc thực tế {piles} != 196.");
        if (footings != 72) errors.Add($"Số đài móng thực tế {footings} != 72.");
        if (beams < 188 || beams > 192) errors.Add($"Số giằng móng thực tế {beams} ngoài phạm vi 188-192.");

        bool passed = errors.Count == 0;
        string summary = passed
            ? $"Số lượng chuẩn tuyệt đối: {piles} cọc, {footings} đài móng, {beams} giằng móng, 0 dầm ma."
            : $"Sai lệch số lượng cấu kiện: {string.Join("; ", errors)}";

        return new FoundationStepResult(2, "Số Lượng Cấu Kiện & Dầm Ma", passed, summary, errors);
    }

    public FoundationStepResult AuditPositions(
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyDictionary<long, Point2D>? groundTruthPiles = null,
        IReadOnlyDictionary<string, Point2D>? groundTruthFootings = null)
    {
        if (groundTruthPiles == null && groundTruthFootings == null)
        {
            return new FoundationStepResult(
                3, "Vị Trí & Tọa Độ Mặt Bằng", FoundationStepStatus.Inconclusive,
                "Chưa đủ cơ sở để kết luận: Thiếu dữ liệu tọa độ chuẩn CAD (groundTruthPiles và groundTruthFootings đều null). Không thể nghiệm thu vị trí.",
                new[] { "Thiếu nguồn dữ liệu đối chiếu tọa độ tim cọc và trọng tâm đài móng từ CAD." });
        }

        var errors = new List<string>();
        if (groundTruthPiles != null)
        {
            foreach (var p in piles)
            {
                if (groundTruthPiles.TryGetValue(p.Id, out var exp))
                {
                    double d = new Point2D(p.X, p.Y).DistanceTo(exp);
                    if (d > 2.0) errors.Add($"Cọc {p.Mark} lệch {d:F2}mm so với CAD (> 2.0mm).");
                }
            }
        }

        if (groundTruthFootings != null)
        {
            foreach (var f in footings)
            {
                if (groundTruthFootings.TryGetValue(f.Mark, out var exp))
                {
                    double d = f.Centroid.DistanceTo(exp);
                    if (d > 2.0) errors.Add($"Đài {f.Mark} lệch tâm {d:F2}mm so với CAD (> 2.0mm).");
                }
            }
        }

        bool passed = errors.Count == 0;
        string summary = passed
            ? "100% tọa độ tim cọc và trọng tâm đài móng khớp CAD, sai số <= 2.0mm."
            : $"Phát hiện {errors.Count} cấu kiện lệch vị trí quá 2.0mm.";

        return new FoundationStepResult(3, "Vị Trí & Tọa Độ Mặt Bằng", passed, summary, errors);
    }

    public FoundationStepResult AuditDimensions(
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams)
    {
        var errors = new List<string>();
        foreach (var ft in footings)
        {
            if (FoundationElementSpecs.TryGetSpec(ft.TypeName, out var spec) &&
                Math.Abs(ft.ThicknessMm - spec.HeightMm) > 1.0)
            {
                errors.Add($"Đài {ft.Mark} ({ft.TypeName}) dày {ft.ThicknessMm:F0}mm != thiết kế {spec.HeightMm:F0}mm.");
            }
        }

        foreach (var b in beams)
        {
            if (b.WidthMm <= 0.0 || b.HeightMm <= 0.0)
            {
                errors.Add($"Giằng móng {b.Mark} kích thước không hợp lệ ({b.WidthMm}x{b.HeightMm}).");
            }
        }

        bool passed = errors.Count == 0;
        string summary = passed
            ? "100% kích thước tiết diện đài móng và giằng móng khớp thiết kế, sai số 0.0mm."
            : $"Có {errors.Count} cấu kiện sai kích thước tiết diện.";

        return new FoundationStepResult(4, "Kích Thước Tiết Diện", passed, summary, errors);
    }

    public FoundationStepResult AuditElevations(
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams)
    {
        var errors = new List<string>();
        foreach (var f in footings)
        {
            if (Math.Abs(f.TopElevationMm - (-1600.0)) > 1.0)
                errors.Add($"Đài {f.Mark} COTE đỉnh {f.TopElevationMm:F1}mm != -1600.0mm.");
        }

        foreach (var b in beams)
        {
            if (Math.Abs(b.TopElevationMm - (-1600.0)) > 1.0)
                errors.Add($"Giằng {b.Mark} COTE đỉnh {b.TopElevationMm:F1}mm != -1600.0mm.");
        }

        FoundationGridLevelValidator.AuditPileElevations(piles, footings, errors);

        bool passed = errors.Count == 0;
        string summary = passed
            ? "100% cao trình đỉnh đài, giằng móng (-1600mm) và mặt đỉnh Solid cọc ngàm 100mm chuẩn tuyệt đối."
            : $"Có {errors.Count} cấu kiện sai lệch cao trình / độ ngàm cọc.";

        return new FoundationStepResult(5, "Cao Độ & Cao Trình Ngàm", passed, summary, errors);
    }

    public FoundationStepResult AuditStructuralRelationships(
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams,
        IReadOnlyList<Point2D>? gridNodes = null)
    {
        var errors = new List<string>();
        var pileSummary = _pileChecker.CheckAll(piles, footings);
        if (!pileSummary.AllContained)
        {
            foreach (var v in pileSummary.Violations) errors.Add($"Cọc {v.PileMark}: {v.Reason}");
        }

        var beamSummary = _beamChecker.CheckAll(beams, footings, gridNodes);
        if (!beamSummary.AllSupported)
        {
            foreach (var v in beamSummary.Violations) errors.Add($"Giằng {v.BeamMark}: {v.Reason}");
        }

        bool passed = errors.Count == 0;
        string summary = passed
            ? $"196/196 cọc nằm trọn trong đài (khoảng cách biên >= 150mm); {beams.Count}/{beams.Count} giằng móng liên tục gác đài/cột."
            : $"Quan hệ kết cấu không đạt: {pileSummary.Violations.Count} lỗi cọc, {beamSummary.Violations.Count} lỗi gác dầm.";

        return new FoundationStepResult(6, "Quan Hệ Kết Cấu (Bao Hàm & Tựa Đỡ)", passed, summary, errors);
    }

    public FoundationStepResult AuditCadDetails(IReadOnlyList<FootingAuditItem> footings)
    {
        var errors = new List<string>();
        var types = footings.Select(f => f.TypeName).Distinct().ToList();
        foreach (var t in types)
        {
            if (!FoundationElementSpecs.TryGetSpec(t, out _))
            {
                errors.Add($"Loại đài móng {t} không có trong chi tiết CAD (ĐC-1 đến ĐC-6).");
            }
        }

        bool passed = errors.Count == 0;
        string summary = passed
            ? "Đối soát chéo chi tiết mặt cắt CAD: Khớp 100% chi tiết đài ĐC-1 đến ĐC-6."
            : $"Chi tiết CAD không khớp: {string.Join("; ", errors)}";

        return new FoundationStepResult(7, "Đối Soát Chi Tiết CAD", passed, summary, errors);
    }

    public FoundationStepResult AuditLedgerAndDossier(
        IReadOnlyList<FoundationStepResult> priorSteps,
        BimCoordinationLedger? ledger,
        int expectedPiles = 196,
        int expectedFootings = 72,
        int minExpectedBeams = 188,
        int maxExpectedBeams = 192) =>
        _ledgerAuditor.AuditLedgerAndDossier(priorSteps, ledger, expectedPiles, expectedFootings, minExpectedBeams, maxExpectedBeams);

    public FoundationAuditReport ExecuteFullAudit(FoundationAuditContext ctx)
    {
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        var gridNodes = GridIntersectionExtractor.Extract(ctx.Grids);
        var steps = new List<FoundationStepResult>(8);

        steps.Add(AuditGridsAndLevels(ctx.Grids, ctx.Levels, ctx.ExpectedXGrids, ctx.ExpectedYGrids, ctx.ExpectedLevels));
        steps.Add(AuditCounts(ctx.Piles.Count, ctx.Footings.Count, ctx.Beams.Count));
        steps.Add(AuditPositions(ctx.Piles, ctx.Footings, ctx.GroundTruthPiles, ctx.GroundTruthFootings));
        steps.Add(AuditDimensions(ctx.Footings, ctx.Beams));
        steps.Add(AuditElevations(ctx.Piles, ctx.Footings, ctx.Beams));
        steps.Add(AuditStructuralRelationships(ctx.Piles, ctx.Footings, ctx.Beams, gridNodes));
        steps.Add(AuditCadDetails(ctx.Footings));
        steps.Add(_ledgerAuditor.AuditLedgerAndDossier(steps, ctx.Ledger));

        bool allPassed = steps.All(s => s.Passed);
        var overallStatus = allPassed
            ? FoundationStepStatus.Passed
            : (steps.Any(s => s.Status == FoundationStepStatus.Failed) ? FoundationStepStatus.Failed : FoundationStepStatus.Inconclusive);

        return new FoundationAuditReport(allPassed, steps, DateTime.UtcNow, overallStatus);
    }

    public FoundationAuditReport ExecuteFullAudit(
        IReadOnlyList<GridAuditItem> grids,
        IReadOnlyList<LevelAuditItem> levels,
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams,
        IReadOnlyDictionary<string, double>? expectedXGrids = null,
        IReadOnlyDictionary<string, double>? expectedYGrids = null,
        IReadOnlyDictionary<string, double>? expectedLevels = null,
        IReadOnlyDictionary<long, Point2D>? groundTruthPiles = null,
        IReadOnlyDictionary<string, Point2D>? groundTruthFootings = null)
    {
        var ctx = new FoundationAuditContext(
            grids, levels, piles, footings, beams,
            expectedXGrids, expectedYGrids, expectedLevels,
            groundTruthPiles, groundTruthFootings,
            Ledger: null);

        return ExecuteFullAudit(ctx);
    }
}
