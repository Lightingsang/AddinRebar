namespace HPBim.Client.Tests.Cli;

using System;
using System.IO;
using HPBim.Cli.Formatting;
using Xunit;

public sealed class ConsoleFormatterTests
{
    [Fact]
    public void WriteBanner_ProducesBorderedHeader()
    {
        using var sw = new StringWriter();
        var formatter = new ConsoleFormatter(sw);

        formatter.WriteBanner("Test Title", "Sub text");
        string output = sw.ToString();

        Assert.Contains("HPBIM CLI -- TEST TITLE", output);
        Assert.Contains("Sub text", output);
        Assert.Contains("====", output);
    }

    [Fact]
    public void WriteBadges_FormatsPrefixesCorrectly()
    {
        using var sw = new StringWriter();
        var formatter = new ConsoleFormatter(sw);

        formatter.WriteSuccess("All good");
        formatter.WriteError("Failed");
        formatter.WriteWarning("Careful");
        formatter.WriteInfo("Note");

        string output = sw.ToString();
        Assert.Contains("[PASS] All good", output);
        Assert.Contains("[ERROR] Failed", output);
        Assert.Contains("[WARN] Careful", output);
        Assert.Contains("[INFO] Note", output);
    }

    [Fact]
    public void RenderTable_WithHeadersAndRows_OutputsAlignedTable()
    {
        using var sw = new StringWriter();
        var formatter = new ConsoleFormatter(sw);

        string[] headers = ["Col A", "Column B"];
        string[][] rows =
        [
            ["val1", "val2"],
            ["long_value", "123"]
        ];

        formatter.RenderTable(headers, rows);
        string output = sw.ToString();

        Assert.Contains("+", output);
        Assert.Contains("| Col A", output);
        Assert.Contains("| Column B", output);
        Assert.Contains("long_value", output);
    }

    [Fact]
    public void RenderTable_EmptyHeaders_DoesNotThrow()
    {
        using var sw = new StringWriter();
        var formatter = new ConsoleFormatter(sw);

        formatter.RenderTable(Array.Empty<string>(), Array.Empty<string[]>());
        Assert.Empty(sw.ToString());
    }
}
