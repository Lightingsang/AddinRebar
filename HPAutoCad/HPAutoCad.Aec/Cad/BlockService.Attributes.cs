using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The attribute half of <see cref="BlockService"/>: <c>readAttributes</c> (capped per call and per item), and the two-phase
///     attribute writes — <see cref="ValidateAttributes"/> checks tags and the attribute references' own layers, <see cref="WriteAttributes"/>
///     sets what passed — behind <c>writeAttributes</c>, <c>batchUpdateAttributes</c> and <c>update_entities_batch</c>'s <c>attributes</c> key.
/// </summary>
public static partial class BlockService
{
    // ---------------------------------------------------------------- attributes

    public static AnalysisResult<Dictionary<string, object?>> ReadAttributes(EditContext cx, IReadOnlyList<string> handles)
    {
        if (handles.Count == 0) throw new ArgumentException("readAttributes needs handles [] of block references.");
        var result = new AnalysisResult<Dictionary<string, object?>>();
        handles = CapHandles(handles, MaxAttributeHandles, result);
        var items = new List<Dictionary<string, object?>>();
        var reader = new EntityShapeReader(cx.Db, cx.Tr, cx.Units, GeometryTolerance.Default);
        foreach (var handle in handles)
        {
            var block = OpenBlock(cx, handle, result.Errors);
            if (block is null) continue;
            var attributes = ReadAttributes(cx.Tr, block);
            items.Add(new Dictionary<string, object?> { ["handle"] = block.Handle.ToString(), ["blockName"] = reader.EffectiveBlockName(block), ["attributes"] = Capped(attributes), ["attributesTruncated"] = attributes.Count > MaxAttributesPerItem ? attributes.Count - MaxAttributesPerItem : null, ["isDynamic"] = block.IsDynamicBlock });
        }

        result.Items = items;
        result.Count = items.Count;
        result.Success = items.Count > 0 || result.Errors.Count == 0;
        result.Summary = new { requested = handles.Count, read = items.Count };
        return result;
    }

    public static EditResult WriteAttributesOp(EditContext cx, IReadOnlyList<ScriptArgs> items, IReadOnlyList<string> handles, ScriptArgs shared, bool atomic, CancellationToken ct)
    {
        var result = new EditResult();
        var work = items.Count > 0
            ? items.Select(i => (Handle: i.Str("handle"), Values: i.Obj("attributes"))).ToArray()
            : EditContext.DistinctHandles(handles, result.Warnings).Select(h => (Handle: (string?)h, Values: shared)).ToArray();
        if (work.Length == 0 || work.Any(w => w.Values.IsEmpty)) throw new ArgumentException("Give items [{handle, attributes{tag: value}}] or handles [] + attributes {tag: value}.");
        if (work.Length > BatchEditService.MaxBatchItems) throw new ArgumentException($"{work.Length} items; the maximum per call is {BatchEditService.MaxBatchItems}.");

        // Phase 1: the reference and every attribute it would touch must be editable; unknown tags are warnings, none matching is an error.
        var blocks = new BlockReference?[work.Length];
        var errors = new ToolError?[work.Length];
        for (var i = 0; i < work.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var list = new List<ToolError>();
            var entity = cx.OpenForEdit(work[i].Handle, out var error);
            if (entity is null) list.Add(error!);
            else if (entity is not BlockReference block) list.Add(NotABlock(entity));
            else
            {
                var itemWarnings = new List<string>();
                ValidateAttributes(cx, block, work[i].Values, list, itemWarnings);
                foreach (var w in itemWarnings) result.WarnItem(i, w);
                if (list.Count == 0) blocks[i] = block;
            }

            errors[i] = list.FirstOrDefault();
        }

        if (atomic && errors.Any(e => e is not null))
        {
            result.Settle(work.Select((_, i) => new ItemOutcome(i, false, blocks[i]?.Handle.ToString(), "INSERT", Error: errors[i] is null ? null : BatchEditService.Indexed(errors[i]!, i))).ToArray());
            result.Summary = new { requested = work.Length, modified = 0, refused = true, reason = $"atomic: {errors.Count(e => e is not null)} item(s) invalid, nothing modified" };
            return result;
        }

        var outcomes = new ItemOutcome[work.Length];
        for (var i = 0; i < work.Length; i++)
        {
            if (blocks[i] is not { } block)
            {
                outcomes[i] = new ItemOutcome(i, false, null, "INSERT", Error: BatchEditService.Indexed(errors[i]!, i));
                continue;
            }

            var handle = block.Handle.ToString();
            IReadOnlyList<string> written;
            try
            {
                written = WriteAttributes(cx, block, work[i].Values);
            }
            catch (AcadException exception)
            {
                if (atomic) throw new InvalidOperationException($"items[{i}] ({handle}) attributes could not be written ({exception.ErrorStatus}); atomic batch aborted, nothing kept.", exception);
                outcomes[i] = new ItemOutcome(i, false, handle, "INSERT", Error: BatchEditService.Indexed(ToolError.ForHandle(ToolErrorCode.Internal, handle, $"{handle} attributes could not be written: {exception.ErrorStatus}."), i));
                continue;
            }

            outcomes[i] = new ItemOutcome(i, true, handle, "INSERT", written.Select(t => "attributes." + t).ToArray());
            if (written.Count > 0) result.Modified(handle);
        }

        result.Settle(outcomes);
        result.Summary = new { requested = work.Length, modified = result.ModifiedCount, attributesWritten = outcomes.Sum(o => o.Changed?.Count ?? 0) };
        return result;
    }

    /// <summary>Phase 1 of an attribute write: tags that exist (unknown = warning, none = error) and attribute references whose own layer allows the edit.</summary>
    public static void ValidateAttributes(EditContext cx, BlockReference block, ScriptArgs values, List<ToolError> errors, List<string> warnings)
    {
        var handle = block.Handle.ToString();
        var byTag = AttributeReferences(cx.Tr, block);
        var matched = 0;
        foreach (var tag in values.Keys)
        {
            if (!byTag.TryGetValue(tag, out var attribute))
            {
                warnings.Add($"no attribute '{tag}' on this block (tags: {(byTag.Count == 0 ? "none" : string.Join(", ", byTag.Keys))}).");
                continue;
            }

            matched++;
            if (!cx.LayerAllowsEdit(attribute, out var layerError)) errors.Add(layerError! with { Message = $"attribute '{attribute.Tag}' of {handle}: {layerError.Message}" });
        }

        if (matched == 0) errors.Add(ToolError.ForHandle(ToolErrorCode.InvalidArgument, handle, byTag.Count == 0 ? $"{handle} has no attributes." : $"none of the tags exist on {handle} (tags: {string.Join(", ", byTag.Keys)})."));
    }

    /// <summary>Phase 2: sets the attributes <see cref="ValidateAttributes"/> accepted; returns the tags written. An AutoCAD exception propagates.</summary>
    public static IReadOnlyList<string> WriteAttributes(EditContext cx, BlockReference block, ScriptArgs values)
    {
        var byTag = AttributeReferences(cx.Tr, block);
        var written = new List<string>();
        foreach (var tag in values.Keys)
        {
            if (!byTag.TryGetValue(tag, out var attribute)) continue;
            if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
            attribute.TextString = values.Str(tag) ?? "";
            written.Add(attribute.Tag);
        }

        return written;
    }
}
