using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Text;

namespace HPAutoCad.Aec.Cad;

/// <summary>One text value of an entity before and after conversion: <c>text</c>, <c>prompt</c>, <c>contents</c>, <c>dimensionText</c>, <c>mtext</c> or a table cell.</summary>
public sealed record VnTextField(string Name, string Original, string Converted, int Row = -1, int Column = -1);

/// <summary>An entity whose legacy text changes, with the place it lives (a layout, or a block definition every reference shows) and the styles its text uses.</summary>
public sealed record VnTextTarget(ObjectId Id, string Handle, string Type, string Owner, bool InBlockDefinition, string Layer, string Style, IReadOnlyList<ObjectId> StyleIds,
    IReadOnlyList<VnTextField> Fields, IReadOnlyList<string> UnmappedFonts);

/// <summary>Which entities a run converts. No handles and no layers = the whole drawing.</summary>
public sealed record VnScanOptions(IReadOnlySet<string> Handles, IReadOnlyList<string> Layers, bool IncludeBlocks, bool IncludeTables)
{
    public bool IsFiltered => Handles.Count > 0 || Layers.Count > 0;
}

public sealed class VnScanResult
{
    public List<VnTextTarget> Targets { get; } = [];

    /// <summary>Selected text entities looked at.</summary>
    public int Examined { get; set; }

    /// <summary>Styles of every selected text entity, changed or not — the styles a filtered run may remap.</summary>
    public HashSet<ObjectId> StylesSeen { get; } = [];

    /// <summary>
    ///     Legacy text this run will not convert, counted by style: outside the selection, inside excluded blocks or tables, carrying a field,
    ///     or a multiline attribute. A style with any is kept — remapping it would show that text as garbage.
    /// </summary>
    public Dictionary<ObjectId, int> LegacyLeft { get; } = [];

    public Dictionary<string, int> Skipped { get; } = new(StringComparer.Ordinal);

    public void Skip(string reason) => Skipped[reason] = Skipped.GetValueOrDefault(reason) + 1;

    public void Leave(IEnumerable<ObjectId> styles)
    {
        foreach (var style in styles) LegacyLeft[style] = LegacyLeft.GetValueOrDefault(style) + 1;
    }
}

/// <summary>
///     Walks the layouts and every block definition — never xrefs, dependent blocks, or the anonymous <c>*D</c> dimension / <c>*T</c> table
///     blocks AutoCAD regenerates; dynamic-block copies (<c>*U</c>) are walked like their base — and converts every text value it meets.
///     Entities outside the selection are converted too, only to count the legacy text a run leaves behind per style. Read-only.
/// </summary>
public static class VnTextScanner
{
    public static VnScanResult Scan(Database db, Transaction tr, VnTextConverter converter, VnStyleCatalog styles, VnScanOptions options, CancellationToken ct)
    {
        var result = new VnScanResult();
        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (var blockId in blockTable)
        {
            var block = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
            if (!Walkable(block)) continue;
            var owner = block.IsLayout ? ((Layout)tr.GetObject(block.LayoutId, OpenMode.ForRead)).LayoutName : block.Name;
            var place = new Place(owner, !block.IsLayout, block.IsLayout || options.IncludeBlocks);
            foreach (var id in block)
            {
                ct.ThrowIfCancellationRequested();
                switch (tr.GetObject(id, OpenMode.ForRead))
                {
                    case Table table:
                        Visit(table, Read(table, converter, styles), Selected(table, null, options, place) && options.IncludeTables, place, styles, result);
                        break;
                    case BlockReference reference:
                        foreach (ObjectId attributeId in reference.AttributeCollection)
                            if (tr.GetObject(attributeId, OpenMode.ForRead) is AttributeReference attribute)
                                Visit(attribute, Read(attribute, converter, styles), Selected(attribute, reference.Handle.ToString(), options, place), place, styles, result);
                        break;
                    case Entity entity:
                        Visit(entity, Read(entity, converter, styles), Selected(entity, null, options, place), place, styles, result);
                        break;
                }
            }
        }

        return result;
    }

    private sealed record Place(string Owner, bool InBlockDefinition, bool Selectable);

    /// <summary>The text an entity holds: converted fields, the styles they use, legacy fonts named inline without a Unicode face, and whether it can be written.</summary>
    private sealed record Reading(List<VnTextField> Fields, List<ObjectId> Styles, List<string> Unmapped, string? Unwritable = null);

    private static bool Walkable(BlockTableRecord block) =>
        block.IsLayout || (!block.IsFromExternalReference && !block.IsDependent && (!block.IsAnonymous || block.Name.StartsWith("*U", StringComparison.OrdinalIgnoreCase)));

    private static bool Selected(Entity entity, string? parentHandle, VnScanOptions options, Place place)
    {
        if (!place.Selectable) return false;
        if (options.Layers.Count > 0 && !EntityFilter.Matches(options.Layers, entity.Layer)) return false;
        if (options.Handles.Count == 0) return true;
        return options.Handles.Contains(entity.Handle.ToString()) || (parentHandle is not null && options.Handles.Contains(parentHandle));
    }

    private static void Visit(Entity entity, Reading? reading, bool selected, Place place, VnStyleCatalog styles, VnScanResult result)
    {
        if (reading is null) return; // not a text entity
        if (VnTextMarker.IsMarked(entity))
        {
            if (selected) result.Skip("already converted");
            return;
        }

        if (selected)
        {
            result.Examined++;
            foreach (var style in reading.Styles) result.StylesSeen.Add(style);
        }

        if (reading.Fields.Count == 0) return;
        var unwritable = reading.Unwritable ?? (entity.HasFields ? "text with a field" : null);
        if (!selected || unwritable is not null)
        {
            if (selected) result.Skip(unwritable!);
            result.Leave(reading.Styles);
            return;
        }

        result.Targets.Add(new VnTextTarget(entity.ObjectId, entity.Handle.ToString(), entity.GetType().Name, place.Owner, place.InBlockDefinition, entity.Layer,
            string.Join(", ", reading.Styles.Select(id => styles.Get(id)?.Name).OfType<string>()), reading.Styles, reading.Fields, reading.Unmapped));
    }

    private static Reading? Read(Entity entity, VnTextConverter converter, VnStyleCatalog styles)
    {
        var reading = new Reading([], [], []);
        switch (entity)
        {
            case MText mtext:
                AddMText(reading, "contents", mtext.Contents, mtext.TextStyleId, converter, styles);
                break;
            case AttributeReference { IsMTextAttribute: true } attribute:
                AddPlain(reading, "text", attribute.TextString, attribute.TextStyleId, converter, styles);
                return reading with { Unwritable = "multiline attribute" };
            case AttributeDefinition { IsMTextAttributeDefinition: true } definition:
                AddPlain(reading, "text", definition.TextString, definition.TextStyleId, converter, styles);
                return reading with { Unwritable = "multiline attribute definition" };
            case AttributeDefinition definition:
                AddPlain(reading, "text", definition.TextString, definition.TextStyleId, converter, styles);
                AddPlain(reading, "prompt", definition.Prompt, definition.TextStyleId, converter, styles);
                break;
            case DBText text: // also AttributeReference
                AddPlain(reading, "text", text.TextString, text.TextStyleId, converter, styles);
                break;
            case Dimension dimension:
                AddMText(reading, "dimensionText", dimension.DimensionText, dimension.TextStyleId, converter, styles);
                break;
            case MLeader leader when leader.ContentType == ContentType.MTextContent:
                using (var mtext = leader.MText)
                    AddMText(reading, "mtext", mtext.Contents, mtext.TextStyleId, converter, styles);
                break;
            default:
                return null;
        }

        return reading;
    }

    private static Reading Read(Table table, VnTextConverter converter, VnStyleCatalog styles)
    {
        var reading = new Reading([], [], []);
        for (var row = 0; row < table.Rows.Count; row++)
            for (var column = 0; column < table.Columns.Count; column++)
            {
                var cell = table.Cells[row, column];
                var text = cell.TextString ?? "";
                if (text.Length == 0) continue;
                var style = cell.TextStyleId ?? ObjectId.Null;
                if (!style.IsNull && !reading.Styles.Contains(style)) reading.Styles.Add(style);
                var conversion = converter.ConvertMText(text, styles.FontOf(style));
                reading.Unmapped.AddRange(conversion.UnmappedFonts);
                if (conversion.Changed) reading.Fields.Add(new VnTextField("cell", text, conversion.Text, row, column));
            }

        return reading;
    }

    private static void AddPlain(Reading reading, string name, string? text, ObjectId style, VnTextConverter converter, VnStyleCatalog styles)
    {
        if (!reading.Styles.Contains(style)) reading.Styles.Add(style);
        if (string.IsNullOrEmpty(text)) return;
        var conversion = converter.ConvertPlain(text, styles.FontOf(style));
        if (conversion.Changed) reading.Fields.Add(new VnTextField(name, text, conversion.Text));
    }

    private static void AddMText(Reading reading, string name, string? text, ObjectId style, VnTextConverter converter, VnStyleCatalog styles)
    {
        if (!reading.Styles.Contains(style)) reading.Styles.Add(style);
        if (string.IsNullOrEmpty(text)) return;
        var conversion = converter.ConvertMText(text, styles.FontOf(style));
        reading.Unmapped.AddRange(conversion.UnmappedFonts);
        if (conversion.Changed) reading.Fields.Add(new VnTextField(name, text, conversion.Text));
    }
}
