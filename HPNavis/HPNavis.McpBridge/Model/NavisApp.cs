using Autodesk.Navisworks.Api;

namespace HPNavis.McpBridge.Model;

/// <summary>
///     The `app` global. <c>Autodesk.Navisworks.Api.Application</c> is a static class, so a script cannot
///     receive it as a value; this wrapper exposes the few facts a script legitimately needs and keeps
///     <c>Application.Gui</c> (the main window handle a script could parent dialogs to) out of reach.
/// </summary>
public sealed class NavisApp
{
    private readonly Func<bool> _hasClashModule;

    public NavisApp(Func<bool> hasClashModule)
    {
        _hasClashModule = hasClashModule;
    }

    /// <summary>Product year the bridge maps the runtime to, e.g. 2026.</summary>
    public int Year => NavisVersion.Year;

    /// <summary>Navisworks' own runtime version string, e.g. "23.0".</summary>
    public string Version => NavisVersion.Runtime;

    /// <summary>The Clash Detective module is available (Manage) — <c>doc.GetClash()</c> works.</summary>
    public bool HasClashModule => _hasClashModule();

    /// <summary>Every open document (Navisworks normally has one).</summary>
    public IReadOnlyList<Document> Documents => Application.Documents;

    public Document MainDocument => Application.MainDocument;

    /// <summary>True when Roamer was started through the Automation API rather than by a user.</summary>
    public bool IsAutomated => Application.IsAutomated;

    /// <summary>Unsaved edits exist — heavy operations are safer after the user saves.</summary>
    public bool IsModified => Application.ActiveDocument?.IsModified ?? false;
}

/// <summary>Navisworks reports its runtime as 23.0 for Manage 2026; the pipe name and the user speak in product years.</summary>
public static class NavisVersion
{
    /// <summary>The product year this build targets; keep in step with NavisworksYear in Directory.Build.props.</summary>
    public const int BuiltFor = 2026;

    private static readonly IReadOnlyDictionary<int, int> RuntimeMajorToYear = new Dictionary<int, int>
    {
        [21] = 2024,
        [22] = 2025,
        [23] = 2026,
        [24] = 2027,
    };

    public static string Runtime => Safe(() => $"{Application.Version.RuntimeMajor}.{Application.Version.RuntimeMinor}") ?? "?";

    /// <summary>Runtime major (23 for Manage 2026), null when the API refuses to answer.</summary>
    public static int? RuntimeMajor => Safe(() => (int?)Application.Version.RuntimeMajor);

    public static int Year => YearFor(RuntimeMajor, out _);

    /// <summary>Returns the product year for a runtime major version, or <see cref="BuiltFor"/> with <paramref name="known"/> = false.</summary>
    public static int YearFor(int? runtimeMajor, out bool known)
    {
        known = runtimeMajor is { } major && RuntimeMajorToYear.TryGetValue(major, out _);
        return known ? RuntimeMajorToYear[runtimeMajor!.Value] : BuiltFor;
    }

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch { return default; }
    }
}
