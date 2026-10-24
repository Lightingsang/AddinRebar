namespace HPBimToolkit.Core.Contracts;

/// <summary>Command names of protocol v1: <c>&lt;module&gt;.&lt;verb&gt;</c>, mirrored in the frontend's protocol.ts.</summary>
public static class CommandNames
{
    public const string Hello = "app.hello";
    public const string GetDocumentInfo = "dashboard.getDocumentInfo";
    public const string GetStatistics = "dashboard.getStatistics";
    public const string GetSelection = "dashboard.getSelection";
    public const string ShowElements = "dashboard.showElements";
    public const string OpenTool = "dashboard.openTool";
}

/// <summary>Event names of protocol v1.</summary>
public static class EventNames
{
    public const string SelectionChanged = "selection.changed";
    public const string DocumentChanged = "document.changed";
    public const string ModelStale = "model.stale";
    public const string ThemeChanged = "theme.changed";
}
