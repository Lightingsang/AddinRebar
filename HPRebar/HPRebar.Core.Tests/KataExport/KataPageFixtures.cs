using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// The JSON files the Kata pages' tests read (HPBimToolkit.Frontend/src/&lt;page&gt;/__fixtures__), kept in step with the
/// C# records that produce them. Run `dotnet test` with HPREBAR_UPDATE_KATA_FIXTURES=1 to rewrite them.
/// </summary>
internal static class KataPageFixtures
{
    public const string UpdateVariable = "HPREBAR_UPDATE_KATA_FIXTURES";

    /// <summary>The pages' wire format: HPWebHost's BridgeJson options (web defaults, camelCase enums).</summary>
    public static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <param name="page">The page's source folder under src ("kata", "kataSettings").</param>
    /// <param name="producer">Named in the failure message: what the fixtures must match.</param>
    public static void AssertUpToDate(string page, IReadOnlyDictionary<string, object> fixtures, string producer)
    {
        bool update = Environment.GetEnvironmentVariable(UpdateVariable) == "1";
        var stale = new List<string>();
        foreach (var (name, value) in fixtures)
        {
            var path = Path.Combine(Folder(page), name);
            var expected = JsonSerializer.Serialize(value, Wire).ReplaceLineEndings("\n") + "\n";
            var actual = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;
            if (actual == expected) continue;

            stale.Add(name);
            if (!update) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // A rewrite fails once too, so a run that left the variable set never hides drift.
        Assert.True(stale.Count == 0, update
            ? $"Rewritten from {producer}, review and commit: {string.Join(", ", stale)}."
            : $"Missing or out of date with {producer}: {string.Join(", ", stale)}. Run `dotnet test` with {UpdateVariable}=1 and commit them.");
    }

    /// <summary>The page's fixtures, beside this test project under HPRebar/.</summary>
    private static string Folder(string page)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var frontend = Path.Combine(directory.FullName, "HPBimToolkit.Frontend");
            if (Directory.Exists(frontend)) return Path.Combine(frontend, "src", page, "__fixtures__");
        }

        throw new DirectoryNotFoundException($"HPBimToolkit.Frontend not found above {AppContext.BaseDirectory}.");
    }
}
