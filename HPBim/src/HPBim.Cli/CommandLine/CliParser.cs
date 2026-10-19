namespace HPBim.Cli.CommandLine;

using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight, zero-dependency command line parser.
/// </summary>
public static class CliParser
{
    public static CliParseResult Parse(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return CliParseResult.Empty;
        }

        string command = args[0].ToLowerInvariant();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmatched = new List<string>();

        int i = 1;
        while (i < args.Length)
        {
            string current = args[i];
            if (current.StartsWith("--", StringComparison.Ordinal) || current.StartsWith("-", StringComparison.Ordinal))
            {
                string key = current.TrimStart('-');
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    options[key] = args[i + 1];
                    i += 2;
                }
                else
                {
                    flags.Add(key);
                    i++;
                }
            }
            else
            {
                unmatched.Add(current);
                i++;
            }
        }

        return new CliParseResult(command, options, flags, unmatched);
    }
}
