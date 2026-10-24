using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Core.Tests.TypeScript;

/// <summary>
///     Which C# contract travels with each command and event of protocol v1: the source of the frontend's CommandMap and
///     EventMap. A command or event added to <see cref="CommandNames" /> / <see cref="EventNames" /> without an entry here
///     fails <see cref="TypeScriptContractsTests" />.
/// </summary>
internal static class ProtocolCatalog
{
    /// <summary>Command name → request payload (null: the command takes none) and response payload.</summary>
    public static readonly IReadOnlyList<(string Name, Type? Request, Type Response)> Commands =
    [
        (CommandNames.Hello, null, typeof(HelloResult)),
        (CommandNames.GetDocumentInfo, null, typeof(DocumentSnapshot)),
        (CommandNames.GetStatistics, typeof(StatisticsRequest), typeof(ModelStatistics)),
        (CommandNames.GetSelection, null, typeof(SelectionSnapshot)),
        (CommandNames.ShowElements, typeof(ShowElementsRequest), typeof(ShowElementsResult)),
        (CommandNames.OpenTool, typeof(OpenToolRequest), typeof(OpenToolResult)),
    ];

    public static readonly IReadOnlyList<(string Name, Type Payload)> Events =
    [
        (EventNames.SelectionChanged, typeof(SelectionChangedEvent)),
        (EventNames.DocumentChanged, typeof(DocumentSnapshot)),
        (EventNames.ModelStale, typeof(ModelStaleEvent)),
        (EventNames.ThemeChanged, typeof(ThemeChangedEvent)),
    ];

    /// <summary>
    ///     String members whose values form a closed set on the wire; the page switches on them, so they get the union type
    ///     instead of <c>string</c>. The C# contracts stay strings so the JSON never changes.
    /// </summary>
    public static readonly IReadOnlyDictionary<(Type Owner, string Property), string> StringUnions =
        new Dictionary<(Type, string), string>
        {
            [(typeof(HelloResult), nameof(HelloResult.Theme))] = "Theme",
            [(typeof(ThemeChangedEvent), nameof(ThemeChangedEvent.Theme))] = "Theme",
        };

    /// <summary>Union types built from the public string constants of a class, in declaration order.</summary>
    public static readonly IReadOnlyList<(string Name, Type ConstantsOf)> ConstantUnions =
    [
        ("ErrorCode", typeof(BridgeErrorCodes)),
        ("Theme", typeof(ThemeNames)),
    ];
}
