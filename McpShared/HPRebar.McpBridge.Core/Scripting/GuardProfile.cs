namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     What differs between hosts in the static deny-list: the host name used in messages and the
///     extra names that are dangerous only in that host. The base deny-list (System.IO, reflection,
///     threads, await, dynamic, unsafe) is the same everywhere. Profiles are plain data so Core never
///     references a host API and the same profile can drive tests without the host.
/// </summary>
public sealed class GuardProfile
{
    /// <summary>The Revit bridge's behaviour before profiles existed: base deny-list only.</summary>
    public static readonly GuardProfile Revit = new GuardProfile("Revit");

    /// <summary>
    ///     AutoCAD adds what would block or bypass the bridge: every interactive Editor prompt (waits for
    ///     the user on the main thread while the server waits on the pipe), command-context escapes
    ///     (async, no return value, outside the bridge's transaction), modal UI, and committing or
    ///     aborting the bridge-owned `tr` transaction from inside a script.
    /// </summary>
    public static readonly GuardProfile Autocad = new GuardProfile(
        "AutoCAD",
        deniedIdentifiers: new[] { "MessageBox", "SystemObjects" },
        deniedMembers: new[]
        {
            // Editor prompts
            "GetSelection", "GetPoint", "GetEntity", "GetString", "GetKeywords", "GetInteger", "GetDouble", "GetAngle",
            "GetDistance", "GetCorner", "GetFileNameForOpen", "GetFileNameForSave", "GetNestedEntity",
            "SelectWindow", "SelectCrossingWindow", "SelectFence", "SelectPolygon", "SelectWindowPolygon", "SelectCrossingPolygon",
            "SelectPrevious", "SelectLast", "SelectAtPickBox", "Drag", "DoPrompt",
            // command-context escapes and modal UI
            "SendStringToExecute", "Command", "CommandAsync", "ExecuteInApplicationContext", "ExecuteInCommandContextAsync",
            "ShowModalDialog", "ShowModalWindow", "ShowAlertDialog", "Quit", "CloseAndDiscard", "CloseAndSave",
        },
        deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["tr"] = new[] { "Commit", "Abort", "Dispose" },
        },
        deniedNamespaces: new[] { "Autodesk.AutoCAD.Interop", "System.Windows.Forms" });

    public GuardProfile(
        string hostName,
        IReadOnlyCollection<string>? deniedIdentifiers = null,
        IReadOnlyCollection<string>? deniedMembers = null,
        IReadOnlyDictionary<string, string[]>? deniedMembersOnIdentifier = null,
        IReadOnlyCollection<string>? deniedNamespaces = null)
    {
        HostName = hostName;
        DeniedIdentifiers = new HashSet<string>(deniedIdentifiers ?? Array.Empty<string>(), StringComparer.Ordinal);
        DeniedMembers = new HashSet<string>(deniedMembers ?? Array.Empty<string>(), StringComparer.Ordinal);
        DeniedMembersOnIdentifier = deniedMembersOnIdentifier ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
        DeniedNamespaces = deniedNamespaces ?? Array.Empty<string>();
    }

    /// <summary>Used in diagnostics: "… is not allowed in {HostName} scripts."</summary>
    public string HostName { get; }

    /// <summary>Bare type/identifier names denied on top of the base list.</summary>
    public IReadOnlySet<string> DeniedIdentifiers { get; }

    /// <summary>Member names (any receiver) denied on top of the base list.</summary>
    public IReadOnlySet<string> DeniedMembers { get; }

    /// <summary>Member names denied only when the receiver is a specific global, e.g. `tr.Commit()`.</summary>
    public IReadOnlyDictionary<string, string[]> DeniedMembersOnIdentifier { get; }

    /// <summary>Namespace prefixes denied on top of the base list.</summary>
    public IReadOnlyCollection<string> DeniedNamespaces { get; }
}
