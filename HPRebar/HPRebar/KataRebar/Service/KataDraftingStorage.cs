using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// What Kata Rebar keeps on the drafting it adds (extensible storage, out of the user's sight): the beam run it was
/// made for — the sorted unique ids of its framing elements — and what it is (the long section view, a cut mark), so a
/// re-run finds its own section and replaces its own marks while anything the user drew there stays.
/// </summary>
internal static class KataDraftingStorage
{
    public const string SectionKind = "LongSection";
    public const string MarkKind = "BarEndMark";

    /// <summary>Dimensions, their ticks and the 2D lines of Kata's drawing (breaks, marking circles).</summary>
    public const string DraftingKind = "Drafting";

    public const string SheetKind = "Sheet";

    private const string CrossPrefix = "Cross:";

    /// <summary>The cross section of the run's <paramref name="index"/>-th section flag (in station order).</summary>
    public static string CrossKind(int index) => CrossPrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static bool IsCrossKind(string kind) => kind.StartsWith(CrossPrefix, StringComparison.Ordinal);

    private static readonly Guid SchemaId = new("B4E1D7A2-5C3F-4E8B-A6D9-1F0C2E7B9A35");
    private const string RunField = "Run";
    private const string KindField = "Kind";

    /// <summary>The key of a beam run: its elements' unique ids, sorted, so the order they were picked in does not matter.</summary>
    public static string RunKey(IEnumerable<Element> hosts) =>
        string.Join(";", hosts.Select(h => h.UniqueId).OrderBy(id => id, StringComparer.Ordinal));

    public static void Write(Element element, string runKey, string kind)
    {
        var entity = new Entity(SchemaOrBuild());
        entity.Set(RunField, runKey);
        entity.Set(KindField, kind);
        element.SetEntity(entity);
    }

    /// <summary>
    /// The run's drafting of one kind among the elements of class <typeparamref name="T"/>: only elements carrying the
    /// schema are read (an extensible-storage filter), not every line of the model.
    /// </summary>
    public static IReadOnlyList<T> Find<T>(Document doc, string runKey, string kind) where T : Element =>
        Find<T>(doc, runKey, k => k == kind);

    /// <summary>The run's drafting whose kind passes <paramref name="kind"/>.</summary>
    public static IReadOnlyList<T> Find<T>(Document doc, string runKey, Func<string, bool> kind) where T : Element
    {
        var schema = Schema.Lookup(SchemaId);
        if (schema is null) return Array.Empty<T>();

        var runField = schema.GetField(RunField);
        var kindField = schema.GetField(KindField);
        return new FilteredElementCollector(doc).OfClass(typeof(T)).WherePasses(new ExtensibleStorageFilter(SchemaId)).Cast<T>()
            .Where(e => e.GetEntity(schema) is { } entity && entity.IsValid()
                        && entity.Get<string>(runField) == runKey && kind(entity.Get<string>(kindField)))
            .ToList();
    }

    /// <summary>The kind an element was tagged with; null for one Kata Rebar did not tag.</summary>
    public static string? KindOf(Element element)
    {
        var schema = Schema.Lookup(SchemaId);
        if (schema is null) return null;
        var entity = element.GetEntity(schema);
        return entity is not null && entity.IsValid() ? entity.Get<string>(schema.GetField(KindField)) : null;
    }

    /// <summary>Removes the tag: the element stays in the model but is no longer the run's drafting.</summary>
    public static void Forget(Element element)
    {
        var schema = Schema.Lookup(SchemaId);
        if (schema is not null) element.DeleteEntity(schema);
    }

    private static Schema SchemaOrBuild()
    {
        var existing = Schema.Lookup(SchemaId);
        if (existing is not null) return existing;

        var builder = new SchemaBuilder(SchemaId);
        builder.SetSchemaName("HPRebarKataDrafting");
        builder.SetDocumentation("Kata Rebar: beam run and kind of a view or detail line it drew.");
        builder.SetReadAccessLevel(AccessLevel.Public);
        builder.SetWriteAccessLevel(AccessLevel.Public);
        builder.AddSimpleField(RunField, typeof(string));
        builder.AddSimpleField(KindField, typeof(string));
        return builder.Finish();
    }
}
