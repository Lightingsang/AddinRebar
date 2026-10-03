namespace Installer;

public static class Versioning
{
    /// <summary>
    ///     Resolve versions using the specified version string.
    /// </summary>
    public static ResolveVersioningResult CreateFromVersionString(string version)
    {
        var versionParts = version.Split('-');
        var semanticVersion = Version.Parse(versionParts[0]);

        return new ResolveVersioningResult
        {
            Version = version,
            VersionPrefix = semanticVersion,
            VersionSuffix = versionParts.Length > 1 ? versionParts[1] : null
        };
    }
}

public sealed record ResolveVersioningResult
{
    /// <summary>
    ///     Release version, includes version number and release stage.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    ///     The normal part of the release version number.
    /// </summary>
    public required Version VersionPrefix { get; init; }

    /// <summary>
    ///     The pre-release label of the release version number.
    /// </summary>
    public required string? VersionSuffix { get; init; }
}
