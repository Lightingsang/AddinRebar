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
    ///     (async, no return value, outside the bridge's transaction), modal UI, committing or aborting
    ///     the bridge-owned `tr` transaction from inside a script, and starting a transaction of the
    ///     script's own — a Transaction wrapper the script forgets to dispose is finalised later on the
    ///     GC thread and takes acad.exe down (verified live), so `tr` is the only transaction a script gets.
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
            // transactions and locks of the script's own (see summary): the bridge already holds both
            "StartTransaction", "StartOpenCloseTransaction", "TopTransaction", "LockDocument",
        },
        deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["tr"] = new[] { "Commit", "Abort", "Dispose" },
        },
        deniedNamespaces: new[] { "Autodesk.AutoCAD.Interop", "System.Windows.Forms" });

    /// <summary>
    ///     Navisworks (Roamer.exe, .NET Framework 4.8) is a review tool: geometry is read-only and the API
    ///     offers no scoped rollback, so the bridge owns the only transaction and decides undo itself.
    ///     Denied here: anything that touches the undo stack or opens a transaction of the script's own
    ///     (<c>Document.Rollback()</c> undoes the user's last edit, not the script's); the embedded SQLite
    ///     surface (<c>Document.Database</c> → <c>NavisworksCommand</c> executes arbitrary SQL, <c>ATTACH</c>
    ///     reads/writes files); COM/Automation (spawns a second Navisworks or bypasses the .NET API);
    ///     reflection-by-expression (<c>Expression.Call(...).Compile()</c>, <c>Delegate.CreateDelegate</c>
    ///     reach any member by name, past this deny-list); modal UI. Heavy file/clash operations are not
    ///     listed: the Navisworks bridge gates those behind a second user opt-in with its own pre-pass.
    /// </summary>
    public static readonly GuardProfile Navis = new GuardProfile(
        "Navisworks",
        deniedIdentifiers: new[]
        {
            "MessageBox", "Transaction", "Expression", "Delegate",
            "NavisworksApplication", "ComApiBridge", "NavisworksCommand", "NavisworksConnection", "NavisworksDataAdapter",
        },
        deniedMembers: new[]
        {
            // undo stack and transactions belong to the bridge
            "BeginTransaction", "Undo", "Redo", "Rollback", "TryUndo", "TryRedo", "TryRollback", "StartDisableUndo", "EndDisableUndo",
            // source-model units are the user's decision, custom properties need COM
            "SetModelUnitsAndTransform", "SetUserDefined",
            // embedded database and reflection-by-expression
            "Database", "ToNavisworksConnection", "CreateDelegate", "Compile",
        },
        deniedNamespaces: new[]
        {
            "System.Windows.Forms", "Microsoft.Win32", "System.Data", "System.Linq.Expressions",
            "Autodesk.Navisworks.Api.Automation", "Autodesk.Navisworks.Api.Interop", "Autodesk.Navisworks.Api.ComApi", "Autodesk.Navisworks.Api.Data",
        });

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

#if NET48
    // IReadOnlySet<T> does not exist on .NET Framework; the read-only collection view keeps the set immutable to
    // scripts (which import this namespace) — the backing HashSet still answers Contains in O(1).
    /// <summary>Bare type/identifier names denied on top of the base list.</summary>
    public IReadOnlyCollection<string> DeniedIdentifiers { get; }

    /// <summary>Member names (any receiver) denied on top of the base list.</summary>
    public IReadOnlyCollection<string> DeniedMembers { get; }
#else
    /// <summary>Bare type/identifier names denied on top of the base list.</summary>
    public IReadOnlySet<string> DeniedIdentifiers { get; }

    /// <summary>Member names (any receiver) denied on top of the base list.</summary>
    public IReadOnlySet<string> DeniedMembers { get; }
#endif

    /// <summary>Member names denied only when the receiver is a specific global, e.g. `tr.Commit()`.</summary>
    public IReadOnlyDictionary<string, string[]> DeniedMembersOnIdentifier { get; }

    /// <summary>Namespace prefixes denied on top of the base list.</summary>
    public IReadOnlyCollection<string> DeniedNamespaces { get; }
}
