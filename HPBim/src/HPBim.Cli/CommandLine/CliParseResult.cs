namespace HPBim.Cli.CommandLine;

using System;
using System.Collections.Generic;

/// <summary>
/// Encapsulates parsed CLI command, options, flags, and positional arguments.
/// </summary>
public sealed record CliParseResult(
    string Command,
    IReadOnlyDictionary<string, string> Options,
    IReadOnlySet<string> Flags,
    IReadOnlyList<string> UnmatchedArguments)
{
    public string? GetOption(string name) =>
        Options.TryGetValue(name, out var val) ? val : null;

    public string GetOption(string name, string fallback) =>
        Options.TryGetValue(name, out var val) ? val : fallback;

    public bool HasFlag(string name) => Flags.Contains(name);

    public static CliParseResult Empty => new(
        string.Empty,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        Array.Empty<string>());
}
