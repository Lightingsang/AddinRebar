using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using WixSharp;

namespace Installer;

public static partial class Generator
{
    /// <summary>
    ///     Generates Wix directories for the installer, one per Revit version found in the given content directories.
    ///     Each content directory is one add-in published for one version; it becomes its own feature under that version.
    /// </summary>
    public static (int Version, Dir Directory)[] GenerateWixEntities(string[] args)
    {
        var versionStorages = new Dictionary<int, List<WixEntity>>();
        // Two levels only (version → add-in): WixSharp 2.14 writes no Feature at all when the files sit three levels
        // deep, and every component then lands in WiX's hidden default feature. Revisit after a WixSharp upgrade by
        // decompiling the MSI (`wix msi decompile`) and checking its Feature elements.
        var versionFeatures = new Dictionary<int, Feature>();

        foreach (var directory in args)
        {
            var directoryInfo = new DirectoryInfo(directory);
            if (!TryParseVersion(directoryInfo.FullName, out var fileVersion))
            {
                throw new Exception($"Could not parse version from directory name: {directoryInfo.FullName}");
            }

            var version = int.Parse(fileVersion);
            if (!versionFeatures.TryGetValue(version, out var versionFeature))
            {
                versionFeature = new Feature
                {
                    Name = fileVersion,
                    Description = $"Install add-ins for Revit {fileVersion}",
                    Display = FeatureDisplay.expand,
                    ConfigurableDir = $"INSTALL{fileVersion}"
                };

                versionFeatures.Add(version, versionFeature);
            }

            // Add-ins of one version share its Addins folder; a feature each lets the user leave one out.
            var addinName = GetAddinName(directoryInfo);
            var feature = new Feature
            {
                Name = addinName,
                Description = $"Install {addinName} for Revit {fileVersion}"
            };

            versionFeature.Add(feature);

            var files = new Files(feature, $@"{directory}\*.*");
            if (versionStorages.TryGetValue(version, out var storage))
            {
                storage.Add(files);
            }
            else
            {
                versionStorages.Add(version, [files]);
            }

            LogFeatureFiles(directory, fileVersion);
        }

        return versionStorages
            .Select(storage => (storage.Key, new Dir(new Id($"INSTALL{storage.Key}"), storage.Key.ToString(), storage.Value.ToArray())))
            .ToArray();
    }

    /// <summary>
    ///     Parse a version string from the given input.
    /// </summary>
    private static bool TryParseVersion(string input, [NotNullWhen(true)] out string? version)
    {
        version = null;
        var match = VersionRegex().Match(input);
        if (!match.Success) return false;

        switch (match.Value.Length)
        {
            case 4:
                version = match.Value;
                return true;
            case 2:
                version = $"20{match.Value}";
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     The add-in a content directory holds, named after its .addin manifest (falls back to the directory name).
    /// </summary>
    private static string GetAddinName(DirectoryInfo directory)
    {
        var manifest = directory.EnumerateFiles("*.addin").FirstOrDefault();
        return manifest is null ? directory.Name : Path.GetFileNameWithoutExtension(manifest.Name);
    }

    /// <summary>
    ///    Write a list of installer files.
    /// </summary>
    private static void LogFeatureFiles(string directory, string fileVersion)
    {
        var assemblies = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Installer files for version {fileVersion}:");

        foreach (var assembly in assemblies)
        {
            Console.WriteLine($"- {assembly}");
        }
    }

    /// <summary>
    ///     A regular expression to match the last sequence of numeric characters in a string.
    /// </summary>
    [GeneratedRegex(@"(\d+)(?!.*\d)")]
    private static partial Regex VersionRegex();
}