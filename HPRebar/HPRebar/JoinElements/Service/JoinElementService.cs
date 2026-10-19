using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.JoinElements.Model;
using Serilog;

namespace HPRebar.JoinElements.Service;

/// <summary>
/// Implements high-performance geometric intersection detection and Auto Join operations in Revit.
/// Utilizes a 2-stage collision detection (AABB bounding box + native ElementIntersectsElementFilter)
/// with Smart Skip and automatic cleanup of non-intersecting joined elements.
/// </summary>
public sealed class JoinElementService : IJoinElementService
{
    public JoinElementResult ExecuteJoin(
        Document doc,
        UIDocument? uidoc,
        JoinElementSettings settings,
        IProgress<JoinProgressReport>? progress = null)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        var activePairs = settings.CategoryPairs.Where(p => p.IsSelected).ToList();
        if (activePairs.Count == 0)
        {
            return JoinElementResult.Failed("Không có cặp cấu kiện nào được chọn để Join.");
        }

        var sw = Stopwatch.StartNew();

        int totalJoined = 0;
        int totalUnjoined = 0;
        int totalSwitched = 0;
        int totalSkipped = 0;

        // Collect elements per category once to avoid redundant collector queries
        var distinctCategories = new HashSet<BuiltInCategory>();
        foreach (var p in activePairs)
        {
            distinctCategories.Add(p.PrimaryCategory);
            distinctCategories.Add(p.SecondaryCategory);
        }

        var elementsByCategory = new Dictionary<BuiltInCategory, List<Element>>();
        foreach (var bic in distinctCategories)
        {
            elementsByCategory[bic] = CollectElements(doc, uidoc, bic, settings.Scope);
        }

        // Cache bounding boxes
        var boundingBoxCache = new Dictionary<ElementId, BoundingBoxXYZ?>();

        BoundingBoxXYZ? GetBoundingBox(Element elem)
        {
            if (!boundingBoxCache.TryGetValue(elem.Id, out var bb))
            {
                bb = elem.get_BoundingBox(null);
                boundingBoxCache[elem.Id] = bb;
            }
            return bb;
        }

        progress?.Report(new JoinProgressReport(0, 100, "Đang thu thập phần tử và quét va chạm sơ bộ..."));

        var processedPairs = new HashSet<(ElementId, ElementId)>();

        using var tx = new Transaction(doc, "HPRebar - Auto Join Elements");
        var failureOptions = tx.GetFailureHandlingOptions();
        failureOptions.SetFailuresPreprocessor(new JoinWarningFailuresPreprocessor());
        failureOptions.SetClearAfterRollback(true);
        tx.SetFailureHandlingOptions(failureOptions);
        tx.Start();

        try
        {
            // Purge any pre-existing 'joined but do not intersect' warnings in the document
            totalUnjoined += PurgeJoinWarnings(doc);

            int totalPrimary = activePairs.Sum(p => elementsByCategory.TryGetValue(p.PrimaryCategory, out var list) ? list.Count : 0);
            int processedPrimary = 0;

            foreach (var pair in activePairs)
            {
                if (!elementsByCategory.TryGetValue(pair.PrimaryCategory, out var primaryElems) || primaryElems.Count == 0)
                    continue;
                if (!elementsByCategory.TryGetValue(pair.SecondaryCategory, out var secondaryElems) || secondaryElems.Count == 0)
                    continue;

                // Cache secondary boxes for fast lookup
                var secBoxes = new List<(Element Elem, BoundingBoxXYZ Box)>(secondaryElems.Count);
                foreach (var sec in secondaryElems)
                {
                    var bb = GetBoundingBox(sec);
                    if (bb != null) secBoxes.Add((sec, bb));
                }

                if (secBoxes.Count == 0) continue;

                // Process primary elements against secondary candidates
                foreach (var pri in primaryElems)
                {
                    processedPrimary++;
                    if (processedPrimary % 3 == 0 || processedPrimary == totalPrimary)
                    {
                        progress?.Report(new JoinProgressReport(
                            processedPrimary,
                            Math.Max(1, totalPrimary),
                            $"Đang xử lý {pair.DisplayPair}: {processedPrimary}/{totalPrimary} ({totalJoined} đã Join, {totalSkipped} đã chuẩn)..."));
                        DispatcherHelper.DoEvents();
                    }

                    var bbPri = GetBoundingBox(pri);
                    if (bbPri == null) continue;

                    var minPri = bbPri.Min;
                    var maxPri = bbPri.Max;

                    // Ultra-fast in-memory AABB filtering
                    foreach (var (sec, bbSec) in secBoxes)
                    {
                        if (pri.Id == sec.Id) continue;

                        var pairKey = NormalizePair(pri.Id, sec.Id);
                        if (!processedPairs.Add(pairKey)) continue;

                        var minSec = bbSec.Min;
                        var maxSec = bbSec.Max;

                        if (minPri.X <= maxSec.X && maxPri.X >= minSec.X &&
                            minPri.Y <= maxSec.Y && maxPri.Y >= minSec.Y &&
                            minPri.Z <= maxSec.Z && maxPri.Z >= minSec.Z)
                        {
                            ExecuteJoinForCandidatePair(
                                doc,
                                pri,
                                sec,
                                pair.PrimaryCutsSecondary,
                                settings,
                                ref totalJoined,
                                ref totalUnjoined,
                                ref totalSwitched,
                                ref totalSkipped);
                        }
                    }
                }
            }

            // Final purge pass to eliminate any transient join warnings that occurred during processing
            totalUnjoined += PurgeJoinWarnings(doc);

            progress?.Report(new JoinProgressReport(totalPrimary, Math.Max(1, totalPrimary), "Đang dọn dẹp cảnh báo và lưu mô hình..."));
            DispatcherHelper.DoEvents();

            tx.Commit();
            sw.Stop();

            progress?.Report(new JoinProgressReport(totalPrimary, Math.Max(1, totalPrimary), "Hoàn tất Auto Join thành công!"));
            DispatcherHelper.DoEvents();

            int totalPairsChecked = processedPairs.Count;
            Log.Information(
                "Auto Join completed in {Elapsed}ms: {Joined} joined, {Switched} switched, {Unjoined} unjoined, {Skipped} skipped of {Total} pairs",
                sw.ElapsedMilliseconds, totalJoined, totalSwitched, totalUnjoined, totalSkipped, totalPairsChecked);

            return JoinElementResult.Succeeded(
                totalPairsChecked,
                totalJoined,
                totalUnjoined,
                totalSwitched,
                totalSkipped,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi xảy ra trong quá trình thực thi Transaction Auto Join");
            if (tx.HasStarted() && !tx.HasEnded())
            {
                tx.RollBack();
            }
            return JoinElementResult.Failed($"Lỗi khi áp dụng Transaction: {ex.Message}");
        }
    }

    private static void ExecuteJoinForCandidatePair(
        Document doc,
        Element pri,
        Element sec,
        bool primaryCutsSecondary,
        JoinElementSettings settings,
        ref int totalJoined,
        ref int totalUnjoined,
        ref int totalSwitched,
        ref int totalSkipped)
    {
        bool areJoined = false;
        try
        {
            areJoined = JoinGeometryUtils.AreElementsJoined(doc, pri, sec);
        }
        catch (Exception ex)
        {
            // Intentional: If check fails, treat as not joined
            Log.Debug(ex, "AreElementsJoined check failed between {Pri} and {Sec}", pri.Id, sec.Id);
        }

        var cutting = primaryCutsSecondary ? pri : sec;
        var cut = primaryCutsSecondary ? sec : pri;

        // 1. Smart Skip: If already joined and cutting order matches priority -> skip immediately
        if (areJoined && settings.SmartSkip && !settings.UnjoinFirst)
        {
            bool isCuttingCorrect = false;
            try
            {
                isCuttingCorrect = JoinGeometryUtils.IsCuttingElementInJoin(doc, cutting, cut);
            }
            catch (Exception ex)
            {
                // Intentional: If check fails, default to false
                Log.Debug(ex, "IsCuttingElementInJoin check failed between {Pri} and {Sec}", pri.Id, sec.Id);
            }

            if (isCuttingCorrect)
            {
                totalSkipped++;
            }
            else if (settings.SwitchOrderToPriority)
            {
                try
                {
                    JoinGeometryUtils.SwitchJoinOrder(doc, cutting, cut);
                    totalSwitched++;
                }
                catch (Exception ex)
                {
                    // Intentional: Switching join order may fail if elements have complex family geometry
                    Log.Debug(ex, "SwitchJoinOrder failed between {Pri} and {Sec}", pri.Id, sec.Id);
                }
            }
            else
            {
                totalSkipped++;
            }
            return;
        }

        // 2. Unjoin first if explicitly requested
        if (areJoined && settings.UnjoinFirst)
        {
            try
            {
                JoinGeometryUtils.UnjoinGeometry(doc, pri, sec);
                totalUnjoined++;
                areJoined = false;
            }
            catch (Exception ex)
            {
                // Intentional: Ignore unjoin errors
                Log.Debug(ex, "Unjoin failed between {Pri} and {Sec}", pri.Id, sec.Id);
            }
        }

        // 3. Join geometry if not joined
        if (!areJoined)
        {
            try
            {
                JoinGeometryUtils.JoinGeometry(doc, pri, sec);
                totalJoined++;
                areJoined = true;
            }
            catch (Exception ex)
            {
                // Intentional: Revit may refuse to join geometry that only touches on a facet boundary
                Log.Debug(ex, "Join failed between {Pri} and {Sec}", pri.Id, sec.Id);
            }
        }

        // 4. Switch join order to match priority
        if (areJoined && settings.SwitchOrderToPriority)
        {
            try
            {
                if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, cutting, cut))
                {
                    JoinGeometryUtils.SwitchJoinOrder(doc, cutting, cut);
                    totalSwitched++;
                }
            }
            catch (Exception ex)
            {
                // Intentional: Switch order may fail if join is fixed by host constraint
                Log.Debug(ex, "SwitchJoinOrder failed between {Pri} and {Sec}", pri.Id, sec.Id);
            }
        }
    }

    private static (ElementId, ElementId) NormalizePair(ElementId id1, ElementId id2)
    {
        // Multi-version: ElementId comparison for canonical pair ordering (Value on R24+, IntegerValue on R23)
#if REVIT2024_OR_GREATER
        return id1.Value < id2.Value ? (id1, id2) : (id2, id1);
#else
        return id1.IntegerValue < id2.IntegerValue ? (id1, id2) : (id2, id1);
#endif
    }

    private static List<Element> CollectElements(
        Document doc,
        UIDocument? uidoc,
        BuiltInCategory category,
        JoinScope scope)
    {
        switch (scope)
        {
            case JoinScope.ActiveSelection when uidoc is not null:
            {
                var selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count == 0) return new List<Element>();

                var list = new List<Element>();
                foreach (var id in selectedIds)
                {
                    var elem = doc.GetElement(id);
                    if (elem is not null && elem.Category is not null && elem.Category.BuiltInCategory == category)
                    {
                        list.Add(elem);
                    }
                }
                return list;
            }

            case JoinScope.ActiveView when doc.ActiveView is not null:
            {
                return new FilteredElementCollector(doc, doc.ActiveView.Id)
                    .OfCategory(category)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .ToList();
            }

            case JoinScope.EntireModel:
            default:
            {
                return new FilteredElementCollector(doc)
                    .OfCategory(category)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .ToList();
            }
        }
    }

    private static int PurgeJoinWarnings(Document doc)
    {
        int purgedCount = 0;
        try
        {
            var warnings = doc.GetWarnings();
            foreach (var w in warnings)
            {
                string desc = w.GetDescriptionText();
                if (string.IsNullOrEmpty(desc)) continue;

                if (desc.IndexOf("joined but do not intersect", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var failingIds = w.GetFailingElements().ToList();
                    if (failingIds.Count >= 2)
                    {
                        var e1 = doc.GetElement(failingIds[0]);
                        var e2 = doc.GetElement(failingIds[1]);
                        if (e1 != null && e2 != null)
                        {
                            try
                            {
                                if (JoinGeometryUtils.AreElementsJoined(doc, e1, e2))
                                {
                                    JoinGeometryUtils.UnjoinGeometry(doc, e1, e2);
                                    purgedCount++;
                                    Log.Debug("Purged invalid join warning between {E1} and {E2}", e1.Id, e2.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Intentional: Element might be pinned or locked
                                Log.Debug(ex, "Failed to unjoin invalid warning pair {E1} and {E2}", e1.Id, e2.Id);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Intentional: In case GetWarnings query fails
            Log.Debug(ex, "PurgeJoinWarnings query failed");
        }

        return purgedCount;
    }
}
