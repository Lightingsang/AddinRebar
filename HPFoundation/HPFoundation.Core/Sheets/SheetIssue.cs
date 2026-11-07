namespace HPFoundation.Core.Sheets;

/// <summary>A problem the engineer must fix in the sheet. Cell is empty when it concerns the footing as a whole.</summary>
public sealed record SheetIssue(string Cell, string Message);
