using System;
using System.Data;
using HPPowerBi.McpBridge.Tabular;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class DaxResultSerializerTests
{
    [Fact]
    public void ReadFromDataReader_ExtractsColumnsAndFormatsRows()
    {
        using var table = new DataTable();
        table.Columns.Add("CustomerId", typeof(int));
        table.Columns.Add("CustomerName", typeof(string));
        table.Columns.Add("TotalSales", typeof(double));
        table.Columns.Add("JoinedDate", typeof(DateTime));

        var testDate = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        table.Rows.Add(101, "Alice", 1500.50, testDate);
        table.Rows.Add(102, "Bob", 2300.00, testDate.AddDays(1));
        table.Rows.Add(103, "Charlie", DBNull.Value, DBNull.Value);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, durationMs: 42, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Columns.Count);
        Assert.Equal("CustomerId", result.Columns[0].Name);
        Assert.Equal("CustomerName", result.Columns[1].Name);
        Assert.Equal("TotalSales", result.Columns[2].Name);
        Assert.Equal("JoinedDate", result.Columns[3].Name);

        Assert.Equal(3, result.RowCount);
        Assert.False(result.Truncated);
        Assert.Equal(42, result.DurationMs);

        // Row 1 values
        Assert.Equal(101, result.Rows[0][0]);
        Assert.Equal("Alice", result.Rows[0][1]);
        Assert.Equal(1500.50, result.Rows[0][2]);
        Assert.Equal("2026-09-21T12:00:00.0000000", result.Rows[0][3]);

        // Row 3 DBNull values mapped to null
        Assert.Null(result.Rows[2][2]);
        Assert.Null(result.Rows[2][3]);
    }

    [Fact]
    public void ReadFromDataReader_RowLimitExceeded_TruncatesCorrectly()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        for (var i = 1; i <= 20; i++)
            table.Rows.Add(i);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 5, durationMs: 15, ct: TestContext.Current.CancellationToken);

        Assert.Equal(5, result.RowCount);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void FormatAsMarkdown_ProducesCleanMarkdownTable()
    {
        using var table = new DataTable();
        table.Columns.Add("Country", typeof(string));
        table.Columns.Add("Revenue", typeof(double));

        table.Rows.Add("Vietnam", 12345.67);
        table.Rows.Add("Japan", 8910.11);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, durationMs: 25, ct: TestContext.Current.CancellationToken);
        var markdown = PbiDaxExecutor.FormatAsMarkdown(result);

        Assert.Contains("| Country | Revenue |", markdown);
        Assert.Contains("| Vietnam | 12345.67 |", markdown);
        Assert.Contains("Returned 2 row(s) in 25ms", markdown);
    }

    [Fact]
    public void FormatAsJson_ProducesValidJsonString()
    {
        using var table = new DataTable();
        table.Columns.Add("Metric", typeof(string));
        table.Rows.Add("TestMetric");

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, durationMs: 5, ct: TestContext.Current.CancellationToken);
        var json = PbiDaxExecutor.FormatAsJson(result);

        Assert.Contains("\"rowCount\": 1", json);
        Assert.Contains("\"truncated\": false", json);
        Assert.Contains("TestMetric", json);
    }
}
