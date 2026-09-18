using Xunit;

namespace HPCivil3d.McpBridge.Tests;

/// <summary>
///     HPCivil3d/ is a copy of the AutoCAD bridge kept in step by these tests: every fix that lands in HPAutoCad/ must be
///     copied here and every Civil change must stay inside a `civil-only` block, a token or a Civil-owned file.
///     These tests are the fence: the AutoCAD file with the tokens applied must equal the Civil file with its
///     blocks stripped, line for line. A red test names the file and the first differing lines — copy the
///     missing hunk or move the change into a block, never widen the token table to hide it.
/// </summary>
public sealed class MirrorTests
{
    private static readonly MirrorTokenTable Table = MirrorTokenTable.Load();

    public static TheoryData<string, string> MirroredFiles()
    {
        var data = new TheoryData<string, string>();
        foreach (var pair in Table.MirroredFiles) data.Add(pair.Autocad, pair.Civil3d);
        return data;
    }

    [Theory]
    [MemberData(nameof(MirroredFiles))]
    public void Civil_file_equals_the_AutoCAD_file_after_tokens_and_block_stripping(string autocadRelative, string civilRelative)
    {
        Assert.SkipWhen(!Directory.Exists(MirrorTokenTable.AutocadPath("")), "HPAutoCad/ is not beside HPCivil3d/ in this checkout");
        var autocadPath = MirrorTokenTable.AutocadPath(autocadRelative);
        Assert.True(File.Exists(autocadPath), $"AutoCAD file moved or renamed: {autocadRelative} — update mirroredFiles in tools/mirror-tokens.json");

        var civilPath = MirrorTokenTable.CivilPath(civilRelative);
        Assert.True(File.Exists(civilPath), $"mirrored file missing: {civilRelative}");

        var expected = Table.Normalize(Table.ApplyTokens(File.ReadAllText(autocadPath)), stripCivilOnlyBlocks: false);
        var actual = Table.Normalize(File.ReadAllText(civilPath), stripCivilOnlyBlocks: true);

        var firstDifference = FirstDifference(expected, actual);
        Assert.True(firstDifference is null,
            $"{civilRelative} drifted from {autocadRelative}: {firstDifference}. Copy the AutoCAD change, or put the Civil change inside `// civil-only: begin/end`.");
    }

    [Fact]
    public void Every_civil_only_block_is_balanced()
    {
        foreach (var pair in Table.MirroredFiles)
        {
            var lines = File.ReadAllLines(MirrorTokenTable.CivilPath(pair.Civil3d));
            var depth = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("civil-only: begin", StringComparison.Ordinal)) depth++;
                if (lines[i].Contains("civil-only: end", StringComparison.Ordinal)) depth--;
                Assert.True(depth is 0 or 1, $"{pair.Civil3d}:{i + 1} nests or closes a civil-only block that was not opened");
            }

            Assert.True(depth == 0, $"{pair.Civil3d} leaves a civil-only block open");
        }
    }

    [Fact]
    public void Every_Civil_bridge_source_file_is_either_mirrored_or_civil_owned()
    {
        var root = MirrorTokenTable.CivilPath("");
        var known = Table.MirroredFiles.Select(pair => pair.Civil3d).Concat(Table.CivilOwnedFiles)
            .Select(relative => Path.GetFullPath(MirrorTokenTable.CivilPath(relative)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = MirrorTokenTable.SourceFiles(root, "HPCivil3d.McpBridge", "HPCivil3d.McpBridge.Loader")
            .Where(path => !known.Contains(Path.GetFullPath(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.True(unknown.Length == 0, "files without a mirror decision (add to mirroredFiles or civilOwnedFiles in tools/mirror-tokens.json): " + string.Join(", ", unknown));
    }

    [Fact]
    public void Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart()
    {
        var root = MirrorTokenTable.AutocadPath("");
        Assert.SkipWhen(!Directory.Exists(root), "HPAutoCad/ is not beside HPCivil3d/ in this checkout");

        // a file AutoCAD adds and Civil never copied is drift too: it must be mirrored, or named as the source of a Civil-owned file
        var known = Table.MirroredFiles.Select(pair => pair.Autocad).Concat(Table.OwnedCounterparts.Select(pair => pair.Autocad))
            .Select(relative => Path.GetFullPath(MirrorTokenTable.AutocadPath(relative)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader")
            .Where(path => !known.Contains(Path.GetFullPath(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.True(unknown.Length == 0, "AutoCAD bridge files with no Civil decision (copy + add to mirroredFiles, or add to ownedCounterparts): " + string.Join(", ", unknown));
    }

    public static TheoryData<string, string, string> OwnedCounterparts()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var pair in Table.OwnedCounterparts) data.Add(pair.Autocad, pair.Civil3d, pair.AutocadSha256);
        return data;
    }

    [Theory]
    [MemberData(nameof(OwnedCounterparts))]
    public void An_owned_counterpart_is_pinned_to_the_AutoCAD_source_it_was_ported_from(string autocadRelative, string civilRelative, string pinnedSha256)
    {
        Assert.SkipWhen(!Directory.Exists(MirrorTokenTable.AutocadPath("")), "HPAutoCad/ is not beside HPCivil3d/ in this checkout");
        var autocadPath = MirrorTokenTable.AutocadPath(autocadRelative);
        Assert.True(File.Exists(autocadPath), $"AutoCAD file moved or renamed: {autocadRelative} — update ownedCounterparts in tools/mirror-tokens.json");
        Assert.True(File.Exists(MirrorTokenTable.CivilPath(civilRelative)), $"civil-owned file missing: {civilRelative}");

        // the Civil file is hand-ported, so the fence is a pin: when AutoCAD changes its side, this fails until someone
        // ports the change (or decides not to) and re-pins the hash
        var actual = MirrorTokenTable.Sha256OfText(autocadPath);
        Assert.True(string.Equals(actual, pinnedSha256, StringComparison.OrdinalIgnoreCase),
            $"{autocadRelative} changed since {civilRelative} was ported from it (sha256 {actual[..12]}… vs pinned {pinnedSha256[..12]}…): port the change, then set autocadSha256 to {actual} in tools/mirror-tokens.json");
    }

    [Fact]
    public void Civil_owned_files_exist()
    {
        foreach (var relative in Table.CivilOwnedFiles) Assert.True(File.Exists(MirrorTokenTable.CivilPath(relative)), $"civil-owned file missing: {relative}");
    }

    [Fact]
    public void Every_token_occurs_in_the_AutoCAD_bridge_sources()
    {
        var autocadRoot = MirrorTokenTable.AutocadPath("");
        Assert.SkipWhen(!Directory.Exists(autocadRoot), "HPAutoCad/ is not beside HPCivil3d/ in this checkout");

        // the whole AutoCAD bridge, Civil-owned counterparts included (BridgeEntry, the self-check, the bundle manifest)
        var autocadTexts = new[] { "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(autocadRoot, project), "*.*", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Path.GetExtension(path) is ".cs" or ".xaml" or ".xml" or ".csproj" or ".json")
            .Select(File.ReadAllText)
            .ToArray();
        var dead = Table.Tokens.Where(token => !autocadTexts.Any(text => text.Contains(token.Autocad, StringComparison.Ordinal))).Select(token => token.Autocad).ToArray();

        Assert.True(dead.Length == 0, "tokens no AutoCAD bridge file contains (dead entries hide drift): " + string.Join(" | ", dead));
    }

    [Fact]
    public void Tokens_are_unique_and_never_map_a_string_onto_itself()
    {
        Assert.Equal(Table.Tokens.Count, Table.Tokens.Select(token => token.Autocad).Distinct(StringComparer.Ordinal).Count());
        Assert.All(Table.Tokens, token => Assert.NotEqual(token.Autocad, token.Civil3d));
        Assert.All(Table.Tokens, token => Assert.False(string.IsNullOrWhiteSpace(token.Autocad)));
    }

    [Fact]
    public void Normalization_strips_blocks_versions_and_blank_lines_only()
    {
        const string civil = "using A;\r\n// civil-only: begin\r\nusing B;\r\n// civil-only: end\r\n\r\n<Version>0.1.0</Version>   \r\n<!-- civil-only: begin -->\r\n<x/>\r\n<!-- civil-only: end -->\r\nend\r\n";
        var lines = Table.Normalize(civil, stripCivilOnlyBlocks: true);
        Assert.Equal(new[] { "using A;", "<version>", "end" }, lines);

        var kept = Table.Normalize(civil, stripCivilOnlyBlocks: false);
        Assert.Contains("using B;", kept);
    }

    private static string? FirstDifference(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var count = Math.Min(expected.Count, actual.Count);
        for (var i = 0; i < count; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal))
                return $"line {i + 1}: AutoCAD `{expected[i].Trim()}` vs Civil `{actual[i].Trim()}`";
        }

        if (expected.Count != actual.Count)
        {
            var longer = expected.Count > actual.Count ? expected : actual;
            var side = expected.Count > actual.Count ? "AutoCAD has extra" : "Civil has extra";
            return $"{side} line {count + 1}: `{longer[count].Trim()}`";
        }

        return null;
    }
}
