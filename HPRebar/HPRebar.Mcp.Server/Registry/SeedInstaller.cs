using System.Reflection;
using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Logging;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     Copies the tools embedded in the server (`SeedLibrary/&lt;Category&gt;/&lt;name&gt;/…`) into the user's
///     library. The checksum of what was installed is remembered in <c>_seeds.json</c>: a seed the user
///     never touched is upgraded when a newer server ships a fix; a seed the user edited belongs to the
///     user and is left alone.
/// </summary>
public static class SeedInstaller
{
    public const string ResourcePrefix = "SeedLibrary/";
    public const string ManifestFile = "_seeds.json";

    public sealed record SeedContent(string Category, string Name, string ToolJson, string Code, string ExamplesJson)
    {
        public string Checksum => RegistryJson.Sha256(ToolJson + "\n" + Code + "\n" + ExamplesJson);
    }

    public static IReadOnlyList<(string Category, string Name)> ListSeeds(Assembly? assembly = null) =>
        LoadSeeds(assembly).Select(s => (s.Category, s.Name)).ToList();

    public static IReadOnlyList<SeedContent> LoadSeeds(Assembly? assembly = null)
    {
        assembly ??= typeof(SeedInstaller).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).ToArray();
        string Read(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return names
            .Select(n => n[ResourcePrefix.Length..].Replace('\\', '/').Split('/'))
            .Where(parts => parts.Length == 3)
            .GroupBy(parts => (parts[0], parts[1]))
            .Select(g =>
            {
                string Of(string file) => names.FirstOrDefault(n => n.Replace('\\', '/') == $"{ResourcePrefix}{g.Key.Item1}/{g.Key.Item2}/{file}") is { } r ? Read(r) : "";
                return new SeedContent(g.Key.Item1, g.Key.Item2, Of(ToolLibraryStore.ToolFile), Of(ToolLibraryStore.CodeFile), Of(ToolLibraryStore.ExamplesFile));
            })
            .Where(s => s.ToolJson.Length > 0 && s.Code.Length > 0)
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToList();
    }

    /// <returns>Names of the tools written (new installs and upgrades of untouched seeds).</returns>
    public static IReadOnlyList<string> Install(ToolLibraryStore store, ILogger logger, Assembly? assembly = null)
    {
        var manifestPath = Path.Combine(store.Root, ManifestFile);
        var manifest = ReadManifest(manifestPath);
        var written = new List<string>();
        var upgraded = 0;
        var adopted = 0;

        foreach (var seed in LoadSeeds(assembly))
        {
            var existingFolder = store.FindFolder(seed.Name);
            if (existingFolder is not null)
            {
                var onDisk = store.TryRead(existingFolder)?.Checksum;
                var installed = manifest.GetValueOrDefault(seed.Name);
                if (installed is null)
                {
                    // library predates the manifest: adopt folders that still equal the shipped seed, leave the rest alone
                    if (onDisk == seed.Checksum) { manifest[seed.Name] = seed.Checksum; adopted++; }
                    continue;
                }

                // upgrade only when the folder is byte-for-byte what we installed and the shipped seed changed
                if (onDisk != installed || installed == seed.Checksum) continue;
                WriteSeed(existingFolder, seed);
                upgraded++;
            }
            else
            {
                WriteSeed(store.FolderFor(seed.Category, seed.Name), seed);
            }

            manifest[seed.Name] = seed.Checksum;
            written.Add(seed.Name);
        }

        if (written.Count > 0 || adopted > 0)
        {
            store.WriteAux(ManifestFile, JsonSerializer.Serialize(manifest, RegistryJson.Options) + "\n");
            logger.LogInformation("Seed tools: {New} installed, {Upgraded} upgraded, {Adopted} adopted in {Root}", written.Count - upgraded, upgraded, adopted, store.Root);
        }

        return written;
    }

    private static void WriteSeed(string folder, SeedContent seed)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ToolLibraryStore.ToolFile), seed.ToolJson, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, ToolLibraryStore.CodeFile), seed.Code, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(folder, ToolLibraryStore.ExamplesFile), seed.ExamplesJson, new UTF8Encoding(false));
    }

    private static Dictionary<string, string> ReadManifest(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), RegistryJson.Options) ?? [] : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
