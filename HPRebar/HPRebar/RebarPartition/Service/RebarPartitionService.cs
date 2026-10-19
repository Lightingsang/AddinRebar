using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using HPRebar.RebarPartition.Model;
using Serilog;

namespace HPRebar.RebarPartition.Service;

/// <summary>
/// Implements rebar inspection and partition assignment logic against the Revit API.
/// </summary>
public sealed class RebarPartitionService : IRebarPartitionService
{
    public IReadOnlyList<RebarPartitionPreviewItem> BuildPreview(
        Document doc,
        UIDocument? uidoc,
        RebarPartitionSettings settings)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        var rebars = CollectRebars(doc, uidoc, settings.Scope);
        var previewItems = new List<RebarPartitionPreviewItem>(rebars.Count);
        var hostCountCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var rebar in rebars)
        {
            var hostId = rebar.GetHostId();
            if (hostId is null || hostId == ElementId.InvalidElementId)
            {
                var curPart = GetRebarPartition(rebar);
                previewItems.Add(new RebarPartitionPreviewItem(
                    rebar.Id,
                    null,
                    "Không có Host",
                    "N/A",
                    string.Empty,
                    curPart,
                    string.Empty,
                    ItemStatus.MissingHost));
                continue;
            }

            var host = doc.GetElement(hostId);
            if (host is null)
            {
                var curPart = GetRebarPartition(rebar);
                previewItems.Add(new RebarPartitionPreviewItem(
                    rebar.Id,
                    hostId,
                    "Không tìm thấy Host",
                    "N/A",
                    string.Empty,
                    curPart,
                    string.Empty,
                    ItemStatus.MissingHost));
                continue;
            }

            // Category filter check
            var hostCategory = host.Category;
            if (hostCategory is not null && settings.EnabledCategories.Count > 0)
            {
                var bic = hostCategory.BuiltInCategory;
                if (!settings.EnabledCategories.Contains(bic))
                {
                    // Skip rebars whose host category is unchecked
                    continue;
                }
            }

            var hostCatName = hostCategory?.Name ?? "Kết cấu";
            var hostName = host.Name;
            var sourceValue = ExtractHostParameterValue(doc, host, settings.SourceParameter, settings.CustomParameterName);
            var currentPartition = GetRebarPartition(rebar);

            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                previewItems.Add(new RebarPartitionPreviewItem(
                    rebar.Id,
                    hostId,
                    hostCatName,
                    hostName,
                    string.Empty,
                    currentPartition,
                    string.Empty,
                    ItemStatus.MissingParameter));
                continue;
            }

            // Calculate continuous host count
            int hostCount = 1;
            if (settings.AssignNumberElement)
            {
                hostCount = CalculateHostCount(doc, host, sourceValue, settings, hostCountCache);
            }

            var targetPartition = $"{settings.Prefix}{sourceValue.Trim()}{settings.Suffix}";
            var status = string.Equals(currentPartition, targetPartition, StringComparison.OrdinalIgnoreCase)
                ? ItemStatus.AlreadyMatching
                : ItemStatus.ReadyToUpdate;

            previewItems.Add(new RebarPartitionPreviewItem(
                rebar.Id,
                hostId,
                hostCatName,
                hostName,
                sourceValue,
                currentPartition,
                targetPartition,
                status,
                hostCount));
        }

        return previewItems;
    }

    public RebarPartitionResult ApplyPartitions(
        Document doc,
        IReadOnlyList<RebarPartitionPreviewItem> itemsToApply,
        RebarPartitionSettings? settings = null)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));
        if (itemsToApply is null || itemsToApply.Count == 0)
            return RebarPartitionResult.Failed("Không có thanh thép nào được chọn để áp dụng.");

        int updated = 0;
        int skipped = 0;
        int errors = 0;

        string numElemParamName = settings?.NumberElementParameterName ?? "Number Element";
        bool assignNumElem = settings?.AssignNumberElement ?? true;

        using var tx = new Transaction(doc, "HPRebar - Gán Partition cốt thép");
        tx.Start();

        try
        {
            foreach (var item in itemsToApply)
            {
                if (!item.IsIncluded || !item.CanApply)
                {
                    skipped++;
                    continue;
                }

                var elem = doc.GetElement(item.RebarId);
                if (elem is not Rebar rebar)
                {
                    errors++;
                    continue;
                }

                bool partitionSet = false;
                var param = rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM);
                if (param is not null && !param.IsReadOnly)
                {
                    param.Set(item.TargetPartition);
                    partitionSet = true;
                }

                if (assignNumElem && item.HostCount > 0)
                {
                    var numParam = rebar.LookupParameter(numElemParamName);
                    if (numParam is not null && !numParam.IsReadOnly)
                    {
                        switch (numParam.StorageType)
                        {
                            case StorageType.Integer:
                                numParam.Set(item.HostCount);
                                break;
                            case StorageType.Double:
                                numParam.Set((double)item.HostCount);
                                break;
                            case StorageType.String:
                                numParam.Set(item.HostCount.ToString());
                                break;
                        }
                    }
                }

                if (partitionSet)
                {
                    updated++;
                }
                else
                {
                    errors++;
                }
            }

            tx.Commit();
            Log.Information("RebarPartitionService updated {Updated} rebars, skipped {Skipped}, errors {Errors}", updated, skipped, errors);
            return RebarPartitionResult.Succeeded(itemsToApply.Count, updated, skipped);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi xảy ra trong quá trình gán Partition cho cốt thép");
            if (tx.HasStarted() && !tx.HasEnded())
            {
                tx.RollBack();
            }
            return RebarPartitionResult.Failed($"Lỗi khi áp dụng Transaction: {ex.Message}");
        }
    }

    private static List<Rebar> CollectRebars(Document doc, UIDocument? uidoc, PartitionScope scope)
    {
        switch (scope)
        {
            case PartitionScope.ActiveSelection when uidoc is not null:
            {
                var selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count == 0) return new List<Rebar>();

                var rebars = new List<Rebar>();
                var hostIds = new HashSet<ElementId>();

                foreach (var id in selectedIds)
                {
                    var elem = doc.GetElement(id);
                    if (elem is Rebar rebar)
                    {
                        rebars.Add(rebar);
                    }
                    else if (elem is not null)
                    {
                        hostIds.Add(id);
                    }
                }

                // If user selected structural hosts, also grab their hosted rebars
                if (hostIds.Count > 0)
                {
                    var allRebars = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Rebar)
                        .WhereElementIsNotElementType()
                        .Cast<Rebar>();

                    foreach (var r in allRebars)
                    {
                        var hId = r.GetHostId();
                        if (hId is not null && hostIds.Contains(hId) && !rebars.Any(existing => existing.Id == r.Id))
                        {
                            rebars.Add(r);
                        }
                    }
                }

                return rebars;
            }

            case PartitionScope.ActiveView when doc.ActiveView is not null:
                return new FilteredElementCollector(doc, doc.ActiveView.Id)
                    .OfCategory(BuiltInCategory.OST_Rebar)
                    .WhereElementIsNotElementType()
                    .Cast<Rebar>()
                    .ToList();

            case PartitionScope.EntireModel:
            default:
                return new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rebar)
                    .WhereElementIsNotElementType()
                    .Cast<Rebar>()
                    .ToList();
        }
    }

    private static string ExtractHostParameterValue(
        Document doc,
        Element host,
        PartitionSourceParameter source,
        string customParamName)
    {
        switch (source)
        {
            case PartitionSourceParameter.ElementName:
            {
                // Primary: Look for "Element Name"
                var elemName = host.LookupParameter("Element Name")?.AsString();
                if (!string.IsNullOrWhiteSpace(elemName)) return elemName;

                // Fallback to Mark or Type Name if "Element Name" is missing
                var mark = host.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
                if (!string.IsNullOrWhiteSpace(mark)) return mark;

                return host.Name;
            }

            case PartitionSourceParameter.Mark:
            {
                var mark = host.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
                return !string.IsNullOrWhiteSpace(mark) ? mark : host.Name;
            }

            case PartitionSourceParameter.TypeName:
            {
                var typeElem = doc.GetElement(host.GetTypeId()) as ElementType;
                return typeElem?.Name ?? host.Name;
            }

            case PartitionSourceParameter.Comments:
            {
                return host.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty;
            }

            case PartitionSourceParameter.LevelAndMark:
            {
                var level = doc.GetElement(host.LevelId)?.Name ?? string.Empty;
                var mark = host.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? host.Name;
                return string.IsNullOrWhiteSpace(level) ? mark : $"{level}_{mark}";
            }

            case PartitionSourceParameter.Custom:
            {
                if (string.IsNullOrWhiteSpace(customParamName)) return string.Empty;
                return host.LookupParameter(customParamName)?.AsString() ?? string.Empty;
            }

            default:
                return host.Name;
        }
    }

    private static string GetRebarPartition(Rebar rebar) =>
        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM)?.AsString() ?? string.Empty;

    private static string GetHostGroupingName(Document doc, Element host)
    {
        var elemName = host.LookupParameter("Element Name")?.AsString();
        if (!string.IsNullOrWhiteSpace(elemName)) return elemName.Trim();

        var mark = host.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
        if (!string.IsNullOrWhiteSpace(mark)) return mark.Trim();

        var typeElem = doc.GetElement(host.GetTypeId()) as ElementType;
        if (typeElem != null && !string.IsNullOrWhiteSpace(typeElem.Name)) return typeElem.Name.Trim();

        return host.Name;
    }

    private static int CalculateHostCount(
        Document doc,
        Element host,
        string groupName,
        RebarPartitionSettings settings,
        Dictionary<string, int> cache)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return 1;

        var cat = host.Category;
        if (cat == null) return 1;

        var bic = cat.BuiltInCategory;
        var levelId = host.LevelId;
        if (levelId == ElementId.InvalidElementId)
        {
            var pLvl = host.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (pLvl != null) levelId = pLvl.AsElementId();
        }

        // Multi-version: ElementId value for cache key
#if REVIT2024_OR_GREATER
        long lvlVal = levelId?.Value ?? -1;
#else
        int lvlVal = levelId?.IntegerValue ?? -1;
#endif

        string cacheKey = bic switch
        {
            BuiltInCategory.OST_StructuralFraming => $"Framing_{lvlVal}_{groupName}",
            BuiltInCategory.OST_StructuralColumns => $"Column_{groupName}",
            _ => $"Other_{(int)bic}_{groupName}"
        };

        if (cache.TryGetValue(cacheKey, out int cachedCount))
            return cachedCount;

        int count;
        if (bic == BuiltInCategory.OST_StructuralFraming)
        {
            count = CountContinuousBeamsOnLevel(doc, levelId ?? ElementId.InvalidElementId, groupName, settings.GroupContinuousGeometry);
        }
        else if (bic == BuiltInCategory.OST_StructuralColumns)
        {
            count = CountContinuousColumns(doc, groupName, settings.GroupContinuousGeometry);
        }
        else
        {
            count = CountOtherCategoryInstances(doc, bic, groupName);
        }

        count = Math.Max(1, count);
        cache[cacheKey] = count;
        return count;
    }

    private static int CountContinuousBeamsOnLevel(
        Document doc,
        ElementId levelId,
        string groupName,
        bool groupContinuous)
    {
        var beams = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .Where(b =>
            {
                var bLvl = b.LevelId;
                if (bLvl == ElementId.InvalidElementId)
                {
                    var p = b.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                    if (p != null) bLvl = p.AsElementId();
                }
                return bLvl == levelId && string.Equals(GetHostGroupingName(doc, b), groupName, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (beams.Count == 0) return 1;
        if (!groupContinuous) return beams.Count;

        var curves = new List<(XYZ P0, XYZ P1)?>(beams.Count);
        foreach (var b in beams)
        {
            if (b.Location is LocationCurve locCurve && locCurve.Curve != null)
            {
                curves.Add((locCurve.Curve.GetEndPoint(0), locCurve.Curve.GetEndPoint(1)));
            }
            else
            {
                curves.Add(null);
            }
        }

        int n = beams.Count;
        int[] parent = Enumerable.Range(0, n).ToArray();
        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }
        void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ) parent[rootI] = rootJ;
        }

        double tolFeet = 50.0 / 304.8; // 50mm in feet (~0.164 ft)
        for (int i = 0; i < n; i++)
        {
            var cI = curves[i];
            if (cI == null) continue;
            var (p0_i, p1_i) = cI.Value;

            for (int j = i + 1; j < n; j++)
            {
                var cJ = curves[j];
                if (cJ == null) continue;
                var (p0_j, p1_j) = cJ.Value;

                if (p0_i.DistanceTo(p0_j) <= tolFeet ||
                    p0_i.DistanceTo(p1_j) <= tolFeet ||
                    p1_i.DistanceTo(p0_j) <= tolFeet ||
                    p1_i.DistanceTo(p1_j) <= tolFeet)
                {
                    Union(i, j);
                }
            }
        }

        var uniqueRoots = new HashSet<int>();
        for (int i = 0; i < n; i++)
        {
            uniqueRoots.Add(Find(i));
        }
        return uniqueRoots.Count;
    }

    private static int CountContinuousColumns(
        Document doc,
        string groupName,
        bool groupContinuous)
    {
        var cols = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .WhereElementIsNotElementType()
            .Where(c => string.Equals(GetHostGroupingName(doc, c), groupName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (cols.Count == 0) return 1;
        if (!groupContinuous) return cols.Count;

        var centers = new List<XYZ?>(cols.Count);
        foreach (var c in cols)
        {
            if (c.Location is LocationPoint locPt && locPt.Point != null)
            {
                centers.Add(locPt.Point);
            }
            else
            {
                var bb = c.get_BoundingBox(null);
                if (bb != null)
                {
                    centers.Add((bb.Min + bb.Max) * 0.5);
                }
                else
                {
                    centers.Add(null);
                }
            }
        }

        int n = cols.Count;
        int[] parent = Enumerable.Range(0, n).ToArray();
        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }
        void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ) parent[rootI] = rootJ;
        }

        double tolColFeet = 100.0 / 304.8; // 100mm in feet (~0.328 ft)
        for (int i = 0; i < n; i++)
        {
            var ptI = centers[i];
            if (ptI == null) continue;

            for (int j = i + 1; j < n; j++)
            {
                var ptJ = centers[j];
                if (ptJ == null) continue;

                double dx = ptI.X - ptJ.X;
                double dy = ptI.Y - ptJ.Y;
                if (Math.Sqrt(dx * dx + dy * dy) <= tolColFeet)
                {
                    Union(i, j);
                }
            }
        }

        var uniqueRoots = new HashSet<int>();
        for (int i = 0; i < n; i++)
        {
            uniqueRoots.Add(Find(i));
        }
        return uniqueRoots.Count;
    }

    private static int CountOtherCategoryInstances(
        Document doc,
        BuiltInCategory bic,
        string groupName)
    {
        int count = new FilteredElementCollector(doc)
            .OfCategory(bic)
            .WhereElementIsNotElementType()
            .Count(e => string.Equals(GetHostGroupingName(doc, e), groupName, StringComparison.OrdinalIgnoreCase));

        return Math.Max(1, count);
    }
}
