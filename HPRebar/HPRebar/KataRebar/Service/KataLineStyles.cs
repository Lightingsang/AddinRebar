using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The line styles Kata Rebar draws with, named after Kata's layers and created (Lines subcategory, colour, weight) when
/// the model lacks them; and Revit's own invisible lines, which the dimension ticks use.
/// </summary>
internal static class KataLineStyles
{
    /// <summary>kata_thep chu: bars and their cut marks (red).</summary>
    public static GraphicsStyle Bar(Document doc) => GetOrCreate(doc, "kata_thep chu", new Color(255, 0, 0), 3);

    /// <summary>kata_dim: break lines (grey).</summary>
    public static GraphicsStyle Thin(Document doc) => GetOrCreate(doc, "kata_dim", new Color(128, 128, 128), 1);

    /// <summary>kata_net manh: marking circles round inner-layer bars (white/black ink).</summary>
    public static GraphicsStyle Fine(Document doc) => GetOrCreate(doc, "kata_net manh", new Color(0, 0, 0), 1);

    /// <summary>
    /// Revit's &lt;Invisible lines&gt;: drawn, never printed — what the dimensions measure to. Not reachable through the
    /// category tree (measured in Revit 2026: Category.GetCategory returns null, Lines' subcategories and a detail
    /// line's GetLineStyleIds do not list it), so it is looked up among the document's graphics styles, by its built-in
    /// category first, then by its name; the styles seen are logged when it is not found.
    /// </summary>
    public static GraphicsStyle Invisible(Document doc)
    {
        long wanted = (long)BuiltInCategory.OST_InvisibleLines;
        var styles = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().ToList();
        var found = styles.FirstOrDefault(s => s.GraphicsStyleCategory is { } c && KataSectionViews.IdValue(c.Id) == wanted)
                    ?? styles.FirstOrDefault(s => s.Name.IndexOf("Invisible", StringComparison.OrdinalIgnoreCase) >= 0);
        if (found is not null) return found;

        Serilog.Log.Warning("Kata Rebar: <Invisible lines> not found among {Count} graphics styles: {Names}", styles.Count,
            string.Join(" | ", styles.Where(s => s.GraphicsStyleCategory?.Parent?.Name == "Lines").Select(s => s.Name).Distinct()));
        throw new InvalidOperationException("Model không có kiểu nét <Invisible lines>.");
    }

    private static GraphicsStyle GetOrCreate(Document doc, string name, Color color, int weight)
    {
        var lines = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
        var sub = lines.SubCategories.Cast<Category>().FirstOrDefault(c => c.Name == name);
        if (sub is null)
        {
            sub = doc.Settings.Categories.NewSubcategory(lines, name);
            sub.LineColor = color;
            sub.SetLineWeight(weight, GraphicsStyleType.Projection);
        }

        return sub.GetGraphicsStyle(GraphicsStyleType.Projection);
    }
}
