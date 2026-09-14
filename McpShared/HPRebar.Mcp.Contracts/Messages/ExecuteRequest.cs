using System.Text.Json;

namespace HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Parameters of `revit.execute`: a C# script and how the bridge should wrap it. <see cref="Args"/> is
///     data the script reads through its `args` global — parameters travel beside the code, never inside
///     it, so a stored tool's source stays byte-identical across calls and compiles once.
/// </summary>
public sealed record ExecuteRequest(
    string Code,
    string Transaction = TransactionModes.Auto,
    bool DryRun = false,
    int TimeoutSeconds = 30,
    string? Label = null,
    JsonElement? Args = null);

/// <summary>
///     How the bridge wraps a script in Revit transactions. Strings rather than an enum so the JSON
///     the AI writes (`"auto"`) is exactly what travels over the pipe with no casing rules to remember.
/// </summary>
public static class TransactionModes
{
    /// <summary>Bridge opens one Transaction around the script inside the undo group.</summary>
    public const string Auto = "auto";

    /// <summary>Script opens its own Transaction(s); the bridge only provides the surrounding group.</summary>
    public const string Manual = "manual";

    /// <summary>Read-only run; any modification fails inside Revit.</summary>
    public const string None = "none";

    public static readonly string[] All = { Auto, Manual, None };

    /// <summary>Accepts any casing and surrounding whitespace; null when the value is not a known mode.</summary>
    public static string? Normalize(string? value)
    {
        if (value is null) return Auto;

        var trimmed = value.Trim().ToLowerInvariant();

        return trimmed.Length == 0 ? Auto : System.Array.IndexOf(All, trimmed) >= 0 ? trimmed : null;
    }
}
