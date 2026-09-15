using Autodesk.Navisworks.Api;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Builds the session snapshot the AI asks for before scripting. Runs on Navisworks' main thread. The
///     Revit-shaped fields get their Navisworks equivalents (the document is the "view", a model item's
///     class is the "category"); what has no counterpart elsewhere goes into <see cref="ContextResult.Navis"/>.
/// </summary>
public static class NavisContextReader
{
    private const int MaxSelection = 50;

    public static ContextResult Read(bool includeSelection, bool executionEnabled, bool heavyEnabled, string hostVersion, NavisQuiescence quiescence)
    {
        var doc = Application.ActiveDocument;

        var result = new ContextResult
        {
            RevitVersion = hostVersion,
            Host = PipeNaming.NavisHost,
            HostVersion = hostVersion,
            ExecutionEnabled = executionEnabled,
            OpenDocs = Application.Documents.Select(d => Safe(() => d.Title) ?? string.Empty).ToArray(),
        };

        if (doc is null) return result;

        var quiescent = quiescence.IsQuiescent(doc);
        var isClear = Safe(() => (bool?)doc.IsClear) ?? true;
        var units = NavisScriptRunner.UnitsOf(doc);

        result.DocTitle = Safe(() => doc.Title);
        result.DocPath = isClear ? null : Safe(() => doc.FileName);
        result.IsReadOnly = false;
        result.IsModifiable = !isClear && quiescent;
        result.Units = new UnitsInfo(units.Label);
        result.ActiveView = new ViewInfo(0, Safe(() => doc.Title) ?? "Untitled", "Document");
        result.Navis = new NavisInfo(
            DocumentUnits: units.Label,
            ModelCount: Safe(() => (int?)doc.Models.Count) ?? 0,
            Models: Safe(() => (IReadOnlyList<ModelSummary>?)doc.Models.Select(m => new ModelSummary(m.FileName, m.Units.ToString(), Safe(() => m.SourceFileName))).ToArray()) ?? [],
            SelectionSetCount: Safe(() => (int?)doc.SelectionSets.Value.Count) ?? 0,
            SavedViewpointCount: Safe(() => (int?)doc.SavedViewpoints.Value.Count) ?? 0,
            ClashTestCount: NavisClashModule.TestCount(doc),
            HasClashModule: NavisClashModule.IsAvailable,
            HeavyOperationsEnabled: heavyEnabled,
            IsClear: isClear,
            IsBusy: !quiescent,
            IsModified: Safe(() => (bool?)doc.IsModified) ?? false);

        if (includeSelection) result.Selection = ReadSelection(doc);

        return result;
    }

    /// <summary>One line per selected item: instance guid hash as the id, class as the category, display name as the name.</summary>
    private static IReadOnlyList<ElementInfo> ReadSelection(Document doc)
    {
        try
        {
            return doc.CurrentSelection.SelectedItems
                .Take(MaxSelection)
                .Select(item => new ElementInfo(item.InstanceGuid.GetHashCode(), Safe(() => item.ClassDisplayName), Safe(() => item.DisplayName)))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }
}
