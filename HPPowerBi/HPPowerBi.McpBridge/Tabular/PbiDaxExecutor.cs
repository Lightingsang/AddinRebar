using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using Serilog;

namespace HPPowerBi.McpBridge.Tabular;

public sealed record DaxColumnHeader(string Name, string DataType);

public sealed record DaxExecutionResult(
    IReadOnlyList<DaxColumnHeader> Columns,
    IReadOnlyList<object?[]> Rows,
    int RowCount,
    bool Truncated,
    long DurationMs,
    string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorMessage == null;
}

/// <summary>
///     Executes DAX queries against an active ADOMD.NET connection or IDataReader, formatting results
///     with rowcount, column metadata, and execution timing.
/// </summary>
public static class PbiDaxExecutor
{
    /// <summary>
    ///     Executes a DAX query on the provided ADOMD connection.
    /// </summary>
    public static async Task<DaxExecutionResult> ExecuteDaxAsync(
        AdomdConnection connection,
        string daxQuery,
        int maxRows = 100,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (string.IsNullOrWhiteSpace(daxQuery))
            return new DaxExecutionResult(Array.Empty<DaxColumnHeader>(), Array.Empty<object?[]>(), 0, false, 0, "DAX query cannot be empty.");

        maxRows = Math.Clamp(maxRows, 1, 10000);
        var sw = Stopwatch.StartNew();

        try
        {
            using var cmd = new AdomdCommand(daxQuery, connection);
            using var reader = await Task.Run(() => (AdomdDataReader)cmd.ExecuteReader(), ct).ConfigureAwait(false);

            sw.Stop();
            var durationMs = sw.ElapsedMilliseconds;

            return ReadFromDataReader(reader, maxRows, durationMs, ct);
        }
        catch (AdomdErrorResponseException ex)
        {
            sw.Stop();
            Log.Warning(ex, "DAX syntax or evaluation error: {Message}", ex.Message);
            return new DaxExecutionResult(
                Array.Empty<DaxColumnHeader>(),
                Array.Empty<object?[]>(),
                0,
                false,
                sw.ElapsedMilliseconds,
                $"DAX Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(ex, "Failed to execute DAX query");
            return new DaxExecutionResult(
                Array.Empty<DaxColumnHeader>(),
                Array.Empty<object?[]>(),
                0,
                false,
                sw.ElapsedMilliseconds,
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Reads from any IDataReader (real ADOMD or in-memory mock for unit tests),
    ///     serializing rows into object arrays with ISO-8601 formatting for DateTimes.
    /// </summary>
    public static DaxExecutionResult ReadFromDataReader(
        IDataReader reader,
        int maxRows = 100,
        long durationMs = 0,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var columns = new List<DaxColumnHeader>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var fieldType = reader.GetFieldType(i)?.Name ?? "Object";
            columns.Add(new DaxColumnHeader(reader.GetName(i), fieldType));
        }

        var rows = new List<object?[]>();
        var truncated = false;

        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();

            if (rows.Count >= maxRows)
            {
                truncated = true;
                break;
            }

            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                {
                    row[i] = null;
                }
                else
                {
                    var val = reader.GetValue(i);
                    row[i] = val switch
                    {
                        DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
                        DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
                        decimal d => Convert.ToDouble(d),
                        _ => val
                    };
                }
            }
            rows.Add(row);
        }

        return new DaxExecutionResult(columns, rows, rows.Count, truncated, durationMs);
    }

    /// <summary>
    ///     Formats DAX result as a clean Markdown table.
    /// </summary>
    public static string FormatAsMarkdown(DaxExecutionResult result)
    {
        if (!result.IsSuccess)
            return $"**Error executing DAX query:**\n```\n{result.ErrorMessage}\n```";

        if (result.Columns.Count == 0)
            return $"Query executed in {result.DurationMs}ms (0 columns returned).";

        var sb = new StringBuilder();

        // Header
        sb.Append("| ");
        foreach (var col in result.Columns)
            sb.Append(col.Name.Replace("|", "\\|")).Append(" | ");
        sb.AppendLine();

        // Separator
        sb.Append("| ");
        foreach (var _ in result.Columns)
            sb.Append("--- | ");
        sb.AppendLine();

        // Rows
        foreach (var row in result.Rows)
        {
            sb.Append("| ");
            foreach (var cell in row)
            {
                var text = cell switch
                {
                    null => "*(null)*",
                    string s => s.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|"),
                    _ => cell.ToString()?.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? string.Empty
                };
                sb.Append(text).Append(" | ");
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.Append($"*Returned {result.RowCount} row(s) in {result.DurationMs}ms*");
        if (result.Truncated)
            sb.Append(" *(truncated to max rows)*");

        return sb.ToString();
    }

    /// <summary>
    ///     Formats DAX result as JSON string.
    /// </summary>
    public static string FormatAsJson(DaxExecutionResult result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        });
    }
}
