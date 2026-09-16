namespace HPAutoCad.Aec.Model;

/// <summary>
///     The envelope of every read-only AEC tool: what was found, how much of it is returned, and what
///     went wrong on the way. <c>Success</c> is true when the requested analysis ran (warnings allowed);
///     it is false only when nothing meaningful could be produced. Serialised camelCase by the bridge.
/// </summary>
public sealed class AnalysisResult<TItem>
{
    public bool Success { get; set; } = true;

    /// <summary>Tool-specific aggregate (counts by type, totals, extents) — small by construction.</summary>
    public object? Summary { get; set; }

    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>How many items matched before paging; compare with <c>items.Count</c> to know whether to page.</summary>
    public int Count { get; set; }

    public int Offset { get; set; }

    public bool Truncated { get; set; }

    public List<string> Warnings { get; } = [];

    public List<ToolError> Errors { get; } = [];

    public AnalysisResult<TItem> Warn(string warning)
    {
        Warnings.Add(warning);
        return this;
    }

    public AnalysisResult<TItem> Fail(ToolError error)
    {
        Errors.Add(error);
        return this;
    }
}
