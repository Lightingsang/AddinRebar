namespace HPBimToolkit.Core.Contracts;

// Payloads of protocol v1. Element ids travel as long (ElementId.Value); nothing here knows a Revit type.

/// <summary>Answer to <c>app.hello</c>: what the page is talking to.</summary>
public sealed record HelloResult(string Version, string RevitVersion, string Theme, IReadOnlyList<string> Capabilities);

/// <summary>Answer to <c>dashboard.getDocumentInfo</c> and payload of <c>document.changed</c>.</summary>
public sealed record DocumentSnapshot(
    bool HasDocument,
    string? Title,
    string? Path,
    bool IsWorkshared,
    bool IsModified,
    bool IsReadOnly,
    bool IsFamily,
    ActiveViewSummary? ActiveView)
{
    public static DocumentSnapshot None { get; } = new(false, null, null, false, false, false, false, null);

    /// <summary>Identity of a document across messages: its path, or its title while it was never saved (frontend: documentKey).</summary>
    public static string KeyOf(string? path, string title) => string.IsNullOrEmpty(path) ? title : path;
}

/// <param name="Scale">View scale denominator (100 for 1:100); 0 for views without a scale (schedules, sheets).</param>
public sealed record ActiveViewSummary(long Id, string Name, string ViewType, int Scale);

public enum StatisticsScope
{
    Model,
    ActiveView,
}

public sealed record StatisticsRequest(StatisticsScope Scope = StatisticsScope.Model);

/// <summary>Answer to <c>dashboard.getStatistics</c>: instance elements per category in the scope plus model-wide counts.</summary>
/// <param name="DocumentKey">Path of the counted document (title while unsaved): the page drops numbers of another document.</param>
/// <param name="ViewId">The counted view for <see cref="StatisticsScope.ActiveView" />, else null.</param>
public sealed record ModelStatistics(
    string DocumentKey,
    long? ViewId,
    StatisticsScope Scope,
    int Total,
    IReadOnlyList<CategoryCount> Categories,
    ModelMetrics Metrics,
    long ElapsedMs);

public sealed record CategoryCount(long CategoryId, string Name, int Count);

/// <param name="Worksets">User worksets; null when the document is not workshared.</param>
public sealed record ModelMetrics(
    int ElementTypes,
    int Views,
    int Sheets,
    int Levels,
    int Families,
    int Warnings,
    int Links,
    int? Worksets);

/// <summary>Answer to <c>dashboard.getSelection</c>; <see cref="Ids" /> is capped, <see cref="Count" /> is not.</summary>
public sealed record SelectionSnapshot(int Count, IReadOnlyList<long> Ids, bool Truncated)
{
    public const int MaxIds = 200;

    public static SelectionSnapshot Empty { get; } = new(0, [], false);

    public static SelectionSnapshot From(IReadOnlyCollection<long> ids) =>
        new(ids.Count, ids.Take(MaxIds).ToArray(), ids.Count > MaxIds);
}

public enum ShowMode
{
    Select,
    Zoom,
}

/// <summary>
///     Request of <c>dashboard.showElements</c>: one category of the scope, explicit ids, or — with
///     <see cref="CurrentSelection" /> — the elements already selected, which are then only zoomed to and never replaced.
/// </summary>
public sealed record ShowElementsRequest(
    long? CategoryId = null,
    IReadOnlyList<long>? Ids = null,
    ShowMode Mode = ShowMode.Select,
    StatisticsScope Scope = StatisticsScope.Model,
    bool CurrentSelection = false);

/// <param name="Shown">Elements now selected (or, for currentSelection, already selected).</param>
/// <param name="Zoomed">Of those, the ones the active view shows and was zoomed to; 0 when the active view shows none.</param>
public sealed record ShowElementsResult(int Shown, int Zoomed);

public sealed record OpenToolRequest(string ToolId);

public sealed record OpenToolResult(bool Opened);

public sealed record SelectionChangedEvent(int Count);

/// <summary>Element ids added, deleted and modified since the last statistics run; the page marks its numbers stale.</summary>
public sealed record ModelStaleEvent(int Added, int Deleted, int Modified);

public sealed record ThemeChangedEvent(string Theme);

public static class ThemeNames
{
    public const string Light = "light";
    public const string Dark = "dark";

    public static string From(bool isDark) => isDark ? Dark : Light;
}
