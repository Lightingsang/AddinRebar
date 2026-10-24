namespace HPBimToolkit.Core.Domain.Dashboard;

/// <summary>Which elements a <c>dashboard.showElements</c> request names; the Revit adapter resolves them.</summary>
public enum ShowElementsSource
{
    /// <summary>What Revit already has selected: zoomed to, never replaced.</summary>
    CurrentSelection,

    /// <summary>Explicit element ids from the page.</summary>
    Ids,

    /// <summary>Every instance of one category in the requested scope.</summary>
    Category,
}
