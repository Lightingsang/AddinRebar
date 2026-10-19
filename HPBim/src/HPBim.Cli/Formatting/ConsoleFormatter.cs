namespace HPBim.Cli.Formatting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Console presentation and Unicode table formatting engine.
/// Fully decoupled from System.Console via TextWriter injection for unit testing.
/// </summary>
public sealed class ConsoleFormatter
{
    private readonly TextWriter _writer;

    public ConsoleFormatter(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    public void WriteBanner(string title, string? subtitle = null)
    {
        _writer.WriteLine("================================================================================");
        _writer.WriteLine($"  HPBIM CLI -- {title.ToUpperInvariant()}");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            _writer.WriteLine($"  {subtitle}");
        }
        _writer.WriteLine("================================================================================");
    }

    public void WriteSuccess(string message) => _writer.WriteLine($"[PASS] {message}");
    public void WriteError(string message) => _writer.WriteLine($"[ERROR] {message}");
    public void WriteWarning(string message) => _writer.WriteLine($"[WARN] {message}");
    public void WriteInfo(string message) => _writer.WriteLine($"[INFO] {message}");

    public void RenderTable(string[] headers, IReadOnlyList<string[]> rows)
    {
        if (headers == null || headers.Length == 0) return;

        int[] colWidths = CalculateWidths(headers, rows);
        string separator = BuildSeparator(colWidths);

        _writer.WriteLine(separator);
        _writer.WriteLine(BuildRow(headers, colWidths));
        _writer.WriteLine(separator);

        if (rows != null)
        {
            foreach (var row in rows)
            {
                _writer.WriteLine(BuildRow(row, colWidths));
            }
        }
        _writer.WriteLine(separator);
    }

    private static int[] CalculateWidths(string[] headers, IReadOnlyList<string[]>? rows)
    {
        int[] widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            widths[i] = headers[i]?.Length ?? 0;
        }

        if (rows != null)
        {
            foreach (var row in rows)
            {
                for (int i = 0; i < Math.Min(row.Length, headers.Length); i++)
                {
                    widths[i] = Math.Max(widths[i], (row[i] ?? string.Empty).Length);
                }
            }
        }
        return widths;
    }

    private static string BuildSeparator(int[] widths)
    {
        var sb = new StringBuilder("+");
        foreach (int w in widths)
        {
            sb.Append(new string('-', w + 2)).Append('+');
        }
        return sb.ToString();
    }

    private static string BuildRow(string[] cells, int[] widths)
    {
        var sb = new StringBuilder("|");
        for (int i = 0; i < widths.Length; i++)
        {
            string content = i < cells.Length ? (cells[i] ?? string.Empty) : string.Empty;
            sb.Append(' ').Append(content.PadRight(widths[i])).Append(" |");
        }
        return sb.ToString();
    }
}
