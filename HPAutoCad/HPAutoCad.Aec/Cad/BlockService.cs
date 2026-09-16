using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Block definitions, references and attributes behind <c>manage_blocks_attributes</c> (dynamic properties in the
///     partner file). Read ops fill an analysis envelope; write ops an edit envelope. Attribute tags are matched
///     case-insensitively; every attribute reference is checked for a locked/frozen layer of its own before any write.
///     Page sizes are measured against the 64 KB result cap: a reference with eight attributes is ~500 B, a definition ~250 B.
/// </summary>
public static partial class BlockService
{
    public static readonly IReadOnlyList<string> Ops = ["listDefinitions", "findReferences", "insert", "readAttributes", "writeAttributes", "batchUpdateAttributes", "inspectDynamic", "setDynamic"];

    public const int MaxReferenceLimit = 100;
    public const int MaxDefinitionLimit = 200;
    public const int MaxAttributeHandles = 100;
    public const int MaxAttributesPerItem = 8;
    public const int MaxAttributeChars = 120;
    private const int MaxTagsListed = 16;

    // ---------------------------------------------------------------- definitions

    public static AnalysisResult<Dictionary<string, object?>> ListDefinitions(EditContext cx, CancellationToken ct, string? namePattern, bool includeAnonymous, int limit, int offset)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        if (limit > MaxDefinitionLimit) result.Warn($"limit is capped at {MaxDefinitionLimit} definitions per page; page with offset.");
        limit = Math.Clamp(limit, 1, MaxDefinitionLimit);
        var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
        var all = new List<Dictionary<string, object?>>();
        foreach (var id in table)
        {
            ct.ThrowIfCancellationRequested();
            var btr = (BlockTableRecord)cx.Tr.GetObject(id, OpenMode.ForRead);
            if (btr.IsLayout || (btr.IsAnonymous && !includeAnonymous)) continue;
            if (!string.IsNullOrWhiteSpace(namePattern) && !EntityFilter.WildcardMatch(namePattern, btr.Name)) continue;
            // An xref's record holds the whole other drawing: never walked here (its attribute definitions and entity count are not what a user means).
            var tags = btr.IsFromExternalReference || !btr.HasAttributeDefinitions ? [] : AttributeDefinitions(cx.Tr, btr).Select(a => a.Tag).ToArray();
            all.Add(new Dictionary<string, object?>
            {
                ["name"] = btr.Name,
                ["isXref"] = btr.IsFromExternalReference,
                ["isOverlay"] = btr.IsFromOverlayReference,
                ["isDynamic"] = btr.IsDynamicBlock,
                ["isAnonymous"] = btr.IsAnonymous,
                ["hasAttributes"] = tags.Length > 0,
                ["attributeTags"] = tags.Take(MaxTagsListed).ToArray(),
                ["attributeTagsTruncated"] = tags.Length > MaxTagsListed ? tags.Length - MaxTagsListed : null,
                ["referenceCount"] = btr.GetBlockReferenceIds(true, false).Count,
                ["entityCount"] = btr.IsFromExternalReference ? null : btr.Cast<ObjectId>().Count(),
                ["comments"] = string.IsNullOrEmpty(btr.Comments) ? null : btr.Comments,
            });
        }

        all.Sort((a, b) => string.CompareOrdinal((string)a["name"]!, (string)b["name"]!));
        result.Items = all.Skip(offset).Take(limit).ToArray();
        result.Count = all.Count;
        result.Offset = offset;
        result.Truncated = all.Count > offset + result.Items.Count;
        result.Summary = new { definitions = all.Count, dynamic = all.Count(d => (bool)d["isDynamic"]!), xrefs = all.Count(d => (bool)d["isXref"]!), withAttributes = all.Count(d => (bool)d["hasAttributes"]!) };
        return result;
    }

    // ---------------------------------------------------------------- references

    public static AnalysisResult<Dictionary<string, object?>> FindReferences(EditContext cx, Editor ed, CancellationToken ct, EntityFilter filter, int limit, int offset, int maxCandidates)
    {
        var result = new AnalysisResult<Dictionary<string, object?>>();
        if (limit > MaxReferenceLimit) result.Warn($"limit is capped at {MaxReferenceLimit} references per page; page with offset.");
        limit = Math.Clamp(limit, 1, MaxReferenceLimit);
        var inserts = new EntityFilter { Types = ["INSERT"], Layers = filter.Layers, BlockNames = filter.BlockNames, Handles = filter.Handles, TextContains = filter.TextContains, VisibleOnly = filter.VisibleOnly, Space = filter.Space };
        var query = EntityQueryService.Query(cx.Db, ed, cx.Tr, cx.Units, ct, inserts, GeometryTolerance.Default, limit, offset, false, maxCandidates);
        result.Errors.AddRange(query.Errors);
        result.Warnings.AddRange(query.Warnings);
        var items = new List<Dictionary<string, object?>>();
        foreach (var record in query.Records)
        {
            if (HandleResolver.OpenEntity(cx.Db, cx.Tr, record.Handle, out _) is not BlockReference block) continue;
            items.Add(new Dictionary<string, object?>
            {
                ["handle"] = record.Handle,
                ["blockName"] = record.BlockName,
                ["layer"] = record.Layer,
                ["space"] = record.Space,
                ["positionMm"] = record.PositionMm,
                ["rotationDeg"] = Math.Round(block.Rotation * 180 / Math.PI, 3),
                ["scale"] = new { x = Math.Round(block.ScaleFactors.X, 4), y = Math.Round(block.ScaleFactors.Y, 4), z = Math.Round(block.ScaleFactors.Z, 4) },
                ["isDynamic"] = block.IsDynamicBlock,
                ["attributes"] = Capped(record.Attributes),
                ["attributesTruncated"] = record.Attributes is { Count: > MaxAttributesPerItem } a ? a.Count - MaxAttributesPerItem : null,
            });
        }

        result.Items = items;
        result.Count = query.Count;
        result.Offset = offset;
        result.Truncated = query.Truncated || query.Count > offset + items.Count;
        result.Summary = new { matched = query.Count, returned = items.Count, byBlock = items.GroupBy(i => (string?)i["blockName"] ?? "?").ToDictionary(g => g.Key, g => g.Count()) };
        return result;
    }

    // ---------------------------------------------------------------- insert

    public static EditResult Insert(EditContext cx, ScriptArgs args, string? space)
    {
        var target = cx.Space(space, out var spaceError) ?? throw new ArgumentException(spaceError!.Message);
        var warnings = new List<string>();
        var built = new EntityFactory(cx).BuildBlockReference(args, warnings);
        if (built.Entity is null) return EditResult.Refused(built.Error!, type: "INSERT");
        var result = new EditResult();
        foreach (var w in warnings) result.Warn(w);
        target.AppendEntity(built.Entity);
        cx.Tr.AddNewlyCreatedDBObject(built.Entity, true);
        var block = (BlockReference)built.Entity;
        var unmatched = built.AfterAppend?.Invoke(cx.Tr) ?? [];
        foreach (var w in unmatched) result.Warn(w);
        var handle = block.Handle.ToString();
        result.Created(handle);
        result.Items = [new ItemOutcome(0, true, handle, "INSERT")];
        var requested = args.Obj("attributes").Keys.Count;
        result.Summary = new { blockName = args.Str("blockName"), handle, attributesSet = requested - unmatched.Count, attributesRequested = requested, space = target.Name };
        return result;
    }

    /// <summary>
    ///     Attribute references for every non-constant definition of the block, filled from {tag: value}; run after the reference is
    ///     in the database. Returns one warning per value whose tag the block does not define.
    /// </summary>
    public static IReadOnlyList<string> AppendAttributes(Transaction tr, BlockReference reference, ObjectId definitionId, ScriptArgs values)
    {
        var definition = (BlockTableRecord)tr.GetObject(definitionId, OpenMode.ForRead);
        var defined = new List<string>();
        foreach (var attDef in AttributeDefinitions(tr, definition))
        {
            defined.Add(attDef.Tag);
            var attribute = new AttributeReference();
            attribute.SetAttributeFromBlock(attDef, reference.BlockTransform);
            var value = values.Str(attDef.Tag);
            if (value is not null) attribute.TextString = value;
            reference.AttributeCollection.AppendAttribute(attribute);
            tr.AddNewlyCreatedDBObject(attribute, true);
        }

        return values.Keys.Where(k => !defined.Contains(k, StringComparer.OrdinalIgnoreCase))
            .Select(k => $"block '{definition.Name}' has no attribute '{k}' (tags: {(defined.Count == 0 ? "none" : string.Join(", ", defined))}).").ToArray();
    }

    // ---------------------------------------------------------------- helpers

    private static Dictionary<string, AttributeReference> AttributeReferences(Transaction tr, BlockReference block)
    {
        var byTag = new Dictionary<string, AttributeReference>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in block.AttributeCollection)
            if (tr.GetObject(id, OpenMode.ForRead) is AttributeReference attribute && !attribute.IsErased) byTag[attribute.Tag] = attribute;
        return byTag;
    }

    private static Dictionary<string, string>? Capped(IReadOnlyDictionary<string, string>? attributes) =>
        attributes is null ? null : attributes.Take(MaxAttributesPerItem).ToDictionary(kv => kv.Key, kv => kv.Value.Length <= MaxAttributeChars ? kv.Value : kv.Value[..MaxAttributeChars] + "…", StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<string> CapHandles<T>(IReadOnlyList<string> handles, int max, AnalysisResult<T> result)
    {
        if (handles.Count <= max) return handles;
        result.Warn($"{handles.Count} handles given; only the first {max} are read per call — call again with the rest.");
        result.Truncated = true;
        return handles.Take(max).ToArray();
    }

    private static ToolError NotABlock(Entity entity) =>
        ToolError.ForHandle(ToolErrorCode.NotAnEntity, entity.Handle.ToString(), $"{entity.Handle} is a {entity.ObjectId.ObjectClass.DxfName}, not a block reference.");

    private static BlockReference? OpenBlock(EditContext cx, string? handle, List<ToolError> errors)
    {
        var entity = HandleResolver.OpenEntity(cx.Db, cx.Tr, handle, out var error);
        if (entity is null) { errors.Add(error!); return null; }
        if (entity is BlockReference block) return block;
        errors.Add(NotABlock(entity));
        return null;
    }

    private static IEnumerable<AttributeDefinition> AttributeDefinitions(Transaction tr, BlockTableRecord definition)
    {
        foreach (ObjectId id in definition)
            if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition attDef && !attDef.Constant) yield return attDef;
    }

    private static Dictionary<string, string> ReadAttributes(Transaction tr, BlockReference block)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId id in block.AttributeCollection)
            if (tr.GetObject(id, OpenMode.ForRead) is AttributeReference attribute && !attribute.IsErased) values[attribute.Tag] = attribute.TextString;
        return values;
    }
}
