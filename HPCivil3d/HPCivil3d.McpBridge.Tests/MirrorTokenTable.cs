using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HPCivil3d.McpBridge.Tests;

/// <summary>
///     The mirror contract between HPAutoCad/ and HPCivil3d/, read from <c>HPCivil3d/tools/mirror-tokens.json</c>:
///     ordered tokens, version patterns, the mirrored file pairs and the Civil-owned files. <see cref="Normalize"/>
///     implements the comparison the file documents — tokens applied to the AutoCAD text in order, `civil-only`
///     blocks stripped from the Civil text, versions replaced by a placeholder, blank lines / trailing whitespace /
///     CRLF / BOM ignored.
/// </summary>
public sealed class MirrorTokenTable
{
    private static readonly Regex CsBlock = new(@"^[ \t]*//[ \t]*civil-only:[ \t]*begin.*?^[ \t]*//[ \t]*civil-only:[ \t]*end[^\n]*\n?", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex XmlBlock = new(@"^[ \t]*<!--[ \t]*civil-only:[ \t]*begin.*?civil-only:[ \t]*end[ \t]*-->[^\n]*\n?", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

    public required string Note { get; init; }
    public required IReadOnlyList<Token> Tokens { get; init; }
    public required IReadOnlyList<string> VersionPatterns { get; init; }
    public required IReadOnlyList<FilePair> MirroredFiles { get; init; }
    public required IReadOnlyList<string> CivilOwnedFiles { get; init; }
    public required IReadOnlyList<OwnedCounterpart> OwnedCounterparts { get; init; }

    public sealed record Token([property: JsonPropertyName("autocad")] string Autocad, [property: JsonPropertyName("civil3d")] string Civil3d);

    public sealed record FilePair([property: JsonPropertyName("autocad")] string Autocad, [property: JsonPropertyName("civil3d")] string Civil3d);

    /// <summary>A Civil-owned file hand-ported from an AutoCAD one; the AutoCAD text is pinned by hash.</summary>
    public sealed record OwnedCounterpart([property: JsonPropertyName("autocad")] string Autocad, [property: JsonPropertyName("civil3d")] string Civil3d, [property: JsonPropertyName("autocadSha256")] string AutocadSha256);

    /// <summary>The repository root: the first parent of the test binary that holds both HPCivil3d/ and McpShared/.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string TokenFilePath => Path.Combine(RepoRoot, "HPCivil3d", "tools", "mirror-tokens.json");

    public static MirrorTokenTable Load()
    {
        var json = File.ReadAllText(TokenFilePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
        return JsonSerializer.Deserialize<MirrorTokenTable>(json, options) ?? throw new InvalidOperationException("mirror-tokens.json is empty");
    }

    /// <summary>The AutoCAD source text with every token applied, in order.</summary>
    public string ApplyTokens(string autocadText)
    {
        foreach (var token in Tokens) autocadText = autocadText.Replace(token.Autocad, token.Civil3d, StringComparison.Ordinal);
        return autocadText;
    }

    /// <summary>Comparable lines: blocks stripped when asked, versions neutralised, blank lines and trailing whitespace gone.</summary>
    public IReadOnlyList<string> Normalize(string text, bool stripCivilOnlyBlocks)
    {
        text = text.Replace("\r\n", "\n").TrimStart('\uFEFF');
        if (stripCivilOnlyBlocks)
        {
            text = CsBlock.Replace(text, "");
            text = XmlBlock.Replace(text, "");
        }

        foreach (var pattern in VersionPatterns) text = Regex.Replace(text, pattern, "<version>");

        return text.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToArray();
    }

    /// <summary>Every source file of the given projects: everything except build output and binary assets.</summary>
    public static IEnumerable<string> SourceFiles(string root, params string[] projects) => projects
        .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), "*.*", SearchOption.AllDirectories))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
        // seed tools are written per host (tool.json + code.cs + examples.json), never mirrored
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Registry{Path.DirectorySeparatorChar}SeedLibrary{Path.DirectorySeparatorChar}"))
        .Where(path => Path.GetExtension(path).ToLowerInvariant() is not (".png" or ".ico" or ".dll" or ".pdb" or ".user" or ".cache" or ".md"));

    /// <summary>SHA-256 of the text with CRLF folded to LF and the BOM dropped, so line endings never count as a change.</summary>
    public static string Sha256OfText(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n").TrimStart('\uFEFF');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    public static string AutocadPath(string relative) => Path.Combine(RepoRoot, "HPAutoCad", relative.Replace('/', Path.DirectorySeparatorChar));

    public static string CivilPath(string relative) => Path.Combine(RepoRoot, "HPCivil3d", relative.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "HPCivil3d")) && Directory.Exists(Path.Combine(directory.FullName, "McpShared")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The repository root (HPCivil3d/ beside McpShared/) was not found above " + AppContext.BaseDirectory);
    }
}
