using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Xunit;

namespace HPAutoCad.Aec.Tests.VnConvert;

/// <summary>
///     ThirdParty/VNConvert must stay byte-for-byte what NOTICE.md says was copied: an edit to a vendored file without a
///     matching NOTICE entry, or an extra file (such as the dropped NativeExports.cs) slipping in, fails here.
/// </summary>
public sealed partial class VendoredVnConvertTests
{
    [Fact]
    public void Every_vendored_file_matches_the_hash_recorded_in_NOTICE()
    {
        string folder = VendoredFolder();
        var recorded = RecordedHashes(File.ReadAllText(Path.Combine(folder, "NOTICE.md")));
        var onDisk = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(folder, path))
            .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), HashWithoutCarriageReturns);

        Assert.Equal(recorded.Keys.Order(), onDisk.Keys.Order());
        foreach (var (file, hash) in recorded)
            Assert.True(hash == onDisk[file], $"{file} differs from NOTICE.md — log the edit there and update its hash");
        Assert.DoesNotContain("NativeExports.cs", onDisk.Keys);
    }

    private static Dictionary<string, string> RecordedHashes(string notice) =>
        HashRow().Matches(notice).ToDictionary(match => match.Groups["file"].Value, match => match.Groups["hash"].Value);

    /// <summary>Git may check files out with CRLF or LF; the recorded hash is of the text with CR removed.</summary>
    private static string HashWithoutCarriageReturns(string path)
    {
        byte[] bytes = File.ReadAllBytes(path).Where(b => b != (byte)'\r').ToArray();
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static bool IsBuildOutput(string folder, string path)
    {
        string relative = Path.GetRelativePath(folder, path);
        return relative.StartsWith("bin" + Path.DirectorySeparatorChar) || relative.StartsWith("obj" + Path.DirectorySeparatorChar);
    }

    private static string VendoredFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "ThirdParty", "VNConvert");
            if (File.Exists(Path.Combine(candidate, "NOTICE.md")))
                return candidate;
        }

        throw new DirectoryNotFoundException("ThirdParty/VNConvert not found above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"^\| `(?<file>[^`]+\.cs)` \| `(?<hash>[0-9a-f]{64})` \|", RegexOptions.Multiline)]
    private static partial Regex HashRow();
}
