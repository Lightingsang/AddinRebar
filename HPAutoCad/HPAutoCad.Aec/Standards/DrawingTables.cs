namespace HPAutoCad.Aec.Standards;

/// <summary>One layer as the standards checker sees it: name, state, and how the drawing's entities use it.</summary>
public sealed record LayerRecord(string Name, bool IsLocked, bool IsFrozen, bool IsOff, string Color, string Linetype, string Lineweight, bool IsXrefDependent, bool IsHidden = false);

/// <summary>One block definition: name and whether it is one of the kinds a naming rule should skip (anonymous, layout, xref, xref-dependent).</summary>
public sealed record BlockDefinitionRecord(string Name, bool IsAnonymous, bool IsLayout, bool IsXref, bool IsDependent = false);

/// <summary>
///     The symbol tables a CAD standards check reads (layers, text styles, dimension styles, block definitions) as plain data,
///     so the checker itself never touches AutoCAD and runs in unit tests.
/// </summary>
public sealed class DrawingTables
{
    public IReadOnlyList<LayerRecord> Layers { get; init; } = [];

    public IReadOnlyList<string> TextStyles { get; init; } = [];

    public IReadOnlyList<string> DimStyles { get; init; } = [];

    public IReadOnlyList<BlockDefinitionRecord> Blocks { get; init; } = [];

    /// <summary>Layers carried by entities inside local block definitions — a layer used only by a title block is not unused.</summary>
    public IReadOnlyList<string> LayersUsedInBlocks { get; init; } = [];

    public static readonly DrawingTables Empty = new();
}
