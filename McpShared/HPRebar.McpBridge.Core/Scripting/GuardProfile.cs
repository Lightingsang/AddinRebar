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
    ///     reads/writes files); COM/Automation (spawns a second Navisworks or bypasses the .NET API); modal UI.
    ///     Reflection-by-expression is denied by the base list for every host. Heavy file/clash operations are
    ///     not listed: the Navisworks bridge gates those behind a second user opt-in with its own pre-pass.
    /// </summary>
    public static readonly GuardProfile Navis = new GuardProfile(
        "Navisworks",
        deniedIdentifiers: new[]
        {
            "MessageBox", "Transaction",
            "NavisworksApplication", "ComApiBridge", "NavisworksCommand", "NavisworksConnection", "NavisworksDataAdapter",
        },
        deniedMembers: new[]
        {
            // undo stack and transactions belong to the bridge
            "BeginTransaction", "Undo", "Redo", "Rollback", "TryUndo", "TryRedo", "TryRollback", "StartDisableUndo", "EndDisableUndo",
            // source-model units are the user's decision, custom properties need COM
            "SetModelUnitsAndTransform", "SetUserDefined",
            // embedded database
            "Database", "ToNavisworksConnection",
        },
        deniedNamespaces: new[]
        {
            "System.Windows.Forms", "System.Data",
            "Autodesk.Navisworks.Api.Automation", "Autodesk.Navisworks.Api.Interop", "Autodesk.Navisworks.Api.ComApi", "Autodesk.Navisworks.Api.Data",
        });

    /// <summary>
    ///     ETABS (out-of-process COM through the managed <c>ETABSv1.dll</c> wrapper; the bridge is a separate desktop
    ///     app). Denied: the <c>Helper</c> class (attaching to or starting an ETABS instance is the bridge's job, and
    ///     every <c>cHelper</c> member is unreachable once the class cannot be named); the <c>cOAPI</c> members that
    ///     start, exit, hide or re-register the application; the bridge's own assembly and the engine's host layer
    ///     (a script only needs <c>HPRebar.McpBridge.Core.Scripting</c>); modal UI. Reflection, interop and process
    ///     control are already on the base list. Writing and destructive OAPI members are not listed here: the
    ///     ETABS bridge classifies them per run behind its snapshot and its second user opt-in.
    /// </summary>
    public static readonly GuardProfile Etabs = new GuardProfile(
        "ETABS",
        deniedIdentifiers: new[] { "Helper", "MessageBox" },
        deniedMembers: new[]
        {
            "ApplicationExit", "ApplicationStart", "Hide", "Unhide", "SetAsActiveObject", "UnsetAsActiveObject", "InternalExec",
        },
        deniedNamespaces: new[] { "System.Windows.Forms", "HPEtabs.McpBridge", "HPRebar.McpBridge.Core.Host" });

    /// <summary>
    ///     Civil 3D is an AutoCAD vertical on the same acad.exe, so everything the AutoCAD profile denies applies
    ///     unchanged (the lists below start from it). On top, the Civil API has three things AutoCAD lacks:
    ///     rebuilds (<c>Corridor.Rebuild</c>, <c>CorridorCollection.RebuildAll</c>, <c>Surface.Rebuild</c>/
    ///     <c>RebuildSnapshot</c> — minutes long, not cancellable through <c>ct</c>, and the subject of Autodesk
    ///     knowledge-base articles about corridors vanishing after a rebuild), state that lives outside the drawing
    ///     (data-shortcut working/project folders and references, the survey database — an aborted transaction
    ///     cannot undo those), and members that read or write files through the native wrapper (<c>ExportToDEM</c>,
    ///     <c>CreateFromLandXML</c>, <c>CreateFromTin</c>, <c>CreateFromDEM</c>, <c>CreateFromIMX</c>,
    ///     <c>ImportPoints</c>/<c>ExportPoints</c>, the <c>CreateSolidsAt…ToFile</c> family), which would bypass the
    ///     base list's <c>System.IO</c> denial; <c>StyleBase.ExportTo</c> is denied for a neighbouring reason — it
    ///     writes styles into another open drawing, outside this drawing's transaction. The names are matched on any
    ///     receiver, so <c>Spline.Rebuild</c>/<c>NurbSurface.Rebuild</c> are caught too — accepted breadth. The dialog
    ///     assembly <c>AeccUiMgd</c> and the <c>Autodesk.AECC.Interop</c>
    ///     COM wrappers are denied as namespaces. <c>RebuildAutomatic</c>/<c>AutoRebuild</c> stay allowed: scripts
    ///     read them, and the setter is an undoable drawing setting, not a rebuild.
    /// </summary>
    public static readonly GuardProfile Civil3d = new GuardProfile(
        "Civil 3D",
        deniedIdentifiers: Autocad.DeniedIdentifiers.Concat(new[] { "DataShortcuts", "SurveyProject", "SurveyProjectCollection" }).ToArray(),
        deniedMembers: Autocad.DeniedMembers.Concat(new[]
        {
            // rebuilds
            "Rebuild", "RebuildAll", "RebuildSnapshot",
            // state outside the drawing
            "SetWorkingFolder", "SetCurrentProjectFolder", "CreateProjectFolder", "AssociateDSProject", "CreateReference",
            "CreatePartialReferenceSurface", "UpdatePartialReferenceSurface", "RepairBrokenDRef",
            "CreateDataShortcutManager", "SaveDataShortcutManager", "SurveyProjects",
            // file members (native wrapper reads/writes the path itself) + cross-drawing style export
            "ExportToDEM", "ExportTo", "CreateFromLandXML", "CreateFromTin", "CreateFromDEM", "CreateFromIMX", "ImportPoints", "ExportPoints",
            "CreateSolidsAtFixedElevationToFile", "CreateSolidsAtDepthToFile", "CreateSolidsAtSurfaceToFile",
        }).ToArray(),
        deniedMembersOnIdentifier: Autocad.DeniedMembersOnIdentifier,
        deniedNamespaces: Autocad.DeniedNamespaces.Concat(new[] { "Autodesk.Civil.DataShortcuts", "Autodesk.Civil.AeccUiMgd", "Autodesk.AECC.Interop" }).ToArray());

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
