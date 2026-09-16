namespace HPAutoCad.Aec.Model;

/// <summary>
///     Where every rule set (classification, CAD standards) comes from: <c>default</c> → the embedded JSON; <c>user</c> → the
///     tool's own file in the MCP server's <c>rules</c> folder; any other plain name → <c>rules\&lt;name&gt;.json</c>; null → the user
///     file when it exists, else the embedded one. Names with path separators or <c>..</c> are refused, and the profile folder
///     is never echoed — a source reads <c>embedded</c> or <c>rules\&lt;file&gt;</c>.
/// </summary>
public static class RuleFileLocator
{
    public const string DefaultName = "default";
    public const string UserName = "user";

    public sealed record Resolved(string Name, string Source, string? Json);

    /// <summary>User rule sets live next to the AutoCAD MCP server's registry, one JSON file per name.</summary>
    public static string UserRulesDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPAutoCad", "McpServer", "rules");

    /// <param name="userFileName">The file <c>user</c> (and null) stands for, e.g. <c>cad-standards.json</c>.</param>
    /// <returns><see cref="Resolved.Json"/> is null when the embedded set is the answer.</returns>
    public static Resolved Resolve(string? name, string userFileName)
    {
        name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (name is not null && (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\') || name.Contains("..")))
            throw new ArgumentException($"ruleSet must be '{DefaultName}', '{UserName}' or a plain file name inside the server's rules folder.");
        if (string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase)) return new Resolved(DefaultName, "embedded", null);

        var fileName = name is null || string.Equals(name, UserName, StringComparison.OrdinalIgnoreCase) ? userFileName : name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : name + ".json";
        var path = Path.Combine(UserRulesDirectory, fileName);
        if (File.Exists(path)) return new Resolved(name ?? UserName, "rules\\" + fileName, File.ReadAllText(path));
        if (name is null) return new Resolved(DefaultName, "embedded", null);
        throw new ArgumentException($"ruleSet '{name}' not found: expected rules\\{fileName} in the server's rules folder.");
    }

    public static string ReadEmbedded(Type anchor, string resourceName)
    {
        using var stream = anchor.Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"embedded rule set {resourceName} is missing from {anchor.Assembly.GetName().Name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
