using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Discovery;
using HPPowerBi.McpBridge.Tabular;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

/// <summary>
///     Empirical stress tests for Milestone 1:
///     1. AnalysisServicesPortFinder edge cases and locking/concurrency.
///     2. PbiDaxExecutor reader edge case types, row limits, and formatting resilience.
/// </summary>
public sealed class Milestone1StressTests : IDisposable
{
    private readonly string _tempDir;

    public Milestone1StressTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_M1_Stress_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    // =========================================================================
    // 1. AnalysisServicesPortFinder Challenges
    // =========================================================================

    [Fact]
    public void ReadPortFile_CorruptedUtf16Le_OddByteCount_ThrowsFormatException()
    {
        var filePath = Path.Combine(_tempDir, "odd_bytes.port.txt");
        // 3 bytes: incomplete UTF-16 character
        File.WriteAllBytes(filePath, new byte[] { 0x35, 0x00, 0x31 });

        Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
    }

    [Fact]
    public void ReadPortFile_NonNumericStrings_ThrowsFormatException()
    {
        var nonNumericInputs = new[]
        {
            "NotANumber",
            "Port 50000",
            "50000abc",
            "NaN",
            "Infinity",
            "50 000",
            "50.123"
        };

        foreach (var input in nonNumericInputs)
        {
            var filePath = Path.Combine(_tempDir, $"non_num_{Guid.NewGuid():N}.port.txt");
            File.WriteAllText(filePath, input, Encoding.Unicode);

            var ex = Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
            Assert.Contains("Cannot parse port number", ex.Message);
        }
    }

    [Fact]
    public void ReadPortFile_Port0_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempDir, "port_zero.port.txt");
        File.WriteAllText(filePath, "0", Encoding.Unicode);

        var ex = Assert.Throws<InvalidDataException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
        Assert.Contains("outside valid range", ex.Message);
    }

    [Fact]
    public void ReadPortFile_NegativePort_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempDir, "port_neg.port.txt");
        File.WriteAllText(filePath, "-1234", Encoding.Unicode);

        var ex = Assert.Throws<InvalidDataException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
        Assert.Contains("outside valid range", ex.Message);
    }

    [Fact]
    public void ReadPortFile_PortAbove65535_ThrowsInvalidDataException()
    {
        var filePath = Path.Combine(_tempDir, "port_high.port.txt");
        File.WriteAllText(filePath, "65536", Encoding.Unicode);

        var ex = Assert.Throws<InvalidDataException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
        Assert.Contains("outside valid range", ex.Message);
    }

    [Fact]
    public void ReadPortFile_PortInt32Overflow_ThrowsFormatException()
    {
        var filePath = Path.Combine(_tempDir, "port_overflow.port.txt");
        File.WriteAllText(filePath, "999999999999999999999999", Encoding.Unicode);

        Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePath));
    }

    [Fact]
    public void ReadPortFile_PortBoundaries_ExactMinAndMax_Succeed()
    {
        var minPath = Path.Combine(_tempDir, "port_min.port.txt");
        File.WriteAllText(minPath, "1024", Encoding.Unicode);
        Assert.Equal(1024, AnalysisServicesPortFinder.ReadPortFile(minPath));

        var maxPath = Path.Combine(_tempDir, "port_max.port.txt");
        File.WriteAllText(maxPath, "65535", Encoding.Unicode);
        Assert.Equal(65535, AnalysisServicesPortFinder.ReadPortFile(maxPath));
    }

    [Fact]
    public void ReadPortFile_PortJustBelowMin_ThrowsInvalidDataException()
    {
        var path = Path.Combine(_tempDir, "port_1023.port.txt");
        File.WriteAllText(path, "1023", Encoding.Unicode);
        var ex = Assert.Throws<InvalidDataException>(() => AnalysisServicesPortFinder.ReadPortFile(path));
        Assert.Contains("outside valid range", ex.Message);
    }

    [Fact]
    public void ReadPortFile_Utf16LeWithBom_ParsesCorrectly()
    {
        var filePath = Path.Combine(_tempDir, "port_utf16_bom.port.txt");
        var bytes = new List<byte> { 0xFF, 0xFE };
        bytes.AddRange(Encoding.Unicode.GetBytes("54321\r\n"));
        File.WriteAllBytes(filePath, bytes.ToArray());

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath);
        Assert.Equal(54321, port);
    }

    [Fact]
    public void ReadPortFile_Utf8WithBom_ParsesCorrectly()
    {
        var filePath = Path.Combine(_tempDir, "port_utf8_bom.port.txt");
        var bytes = new List<byte> { 0xEF, 0xBB, 0xBF };
        bytes.AddRange(Encoding.UTF8.GetBytes("54321"));
        File.WriteAllBytes(filePath, bytes.ToArray());

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath);
        Assert.Equal(54321, port);
    }

    [Fact]
    public void ReadPortFile_Utf16BeWithBom_ParsesCorrectly()
    {
        var filePath = Path.Combine(_tempDir, "port_utf16_be_bom.port.txt");
        // UTF-16BE BOM (0xFE, 0xFF) followed by "54321" in UTF-16BE
        var bytes = new List<byte> { 0xFE, 0xFF };
        bytes.AddRange(Encoding.BigEndianUnicode.GetBytes("54321\r\n"));
        File.WriteAllBytes(filePath, bytes.ToArray());

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath);
        Assert.Equal(54321, port);
    }

    [Fact]
    public void ReadPortFile_ArgumentValidation_ThrowsExpectedExceptions()
    {
        Assert.Throws<ArgumentException>(() => AnalysisServicesPortFinder.ReadPortFile(null!));
        Assert.Throws<ArgumentException>(() => AnalysisServicesPortFinder.ReadPortFile(""));
        Assert.Throws<ArgumentException>(() => AnalysisServicesPortFinder.ReadPortFile("   "));

        var nonExistent = Path.Combine(_tempDir, "does_not_exist.port.txt");
        Assert.Throws<FileNotFoundException>(() => AnalysisServicesPortFinder.ReadPortFile(nonExistent));
    }

    [Fact]
    public void ReadPortFile_EmptyOrWhitespaceFile_ThrowsFormatException()
    {
        var filePathEmpty = Path.Combine(_tempDir, "port_empty.port.txt");
        File.WriteAllBytes(filePathEmpty, Array.Empty<byte>());
        Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePathEmpty));

        var filePathWs = Path.Combine(_tempDir, "port_ws.port.txt");
        File.WriteAllText(filePathWs, "   \r\n\t  \0\0  ", Encoding.Unicode);
        Assert.Throws<FormatException>(() => AnalysisServicesPortFinder.ReadPortFile(filePathWs));
    }

    [Fact]
    public void ReadPortFile_LockedByAnotherProcess_Exclusive_ExceedsRetries_ThrowsIOException()
    {
        var filePath = Path.Combine(_tempDir, "locked_exclusive.port.txt");
        File.WriteAllText(filePath, "50000", Encoding.Unicode);

        // Acquire exclusive lock with FileShare.None
        using var lockStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // AnalysisServicesPortFinder catches IOException during attempts 1..maxRetries-1,
        // and on the last attempt re-throws the underlying IOException.
        var ex = Assert.Throws<IOException>(() =>
            AnalysisServicesPortFinder.ReadPortFile(filePath, maxRetries: 3, retryDelayMs: 20));

        Assert.NotNull(ex);
    }

    [Fact]
    public void ReadPortFile_LockedInitially_ReleasedDuringRetry_RecoversAndSucceeds()
    {
        var filePath = Path.Combine(_tempDir, "lock_recovers.port.txt");
        File.WriteAllText(filePath, "52345", Encoding.Unicode);

        // Lock file with FileShare.None initially
        var lockStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Release the lock after 60ms in background
        _ = Task.Run(async () =>
        {
            await Task.Delay(60, TestContext.Current.CancellationToken);
            lockStream.Dispose();
        }, TestContext.Current.CancellationToken);

        // Read with 5 retries at 40ms intervals (~200ms total window)
        var port = AnalysisServicesPortFinder.ReadPortFile(filePath, maxRetries: 5, retryDelayMs: 40);

        Assert.Equal(52345, port);
    }

    [Fact]
    public void ReadPortFile_ConcurrentReadWriteShare_SucceedsImmediately()
    {
        var filePath = Path.Combine(_tempDir, "shared_readwrite.port.txt");
        File.WriteAllText(filePath, "53456", Encoding.Unicode);

        // Open with FileShare.ReadWrite (as SSAS does)
        using var sharedStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var port = AnalysisServicesPortFinder.ReadPortFile(filePath, maxRetries: 1, retryDelayMs: 10);
        Assert.Equal(53456, port);
    }

    // =========================================================================
    // 2. PbiDaxExecutor Challenges
    // =========================================================================

    [Fact]
    public void ReadFromDataReader_NullsAndDbNulls_AllMappedToNull()
    {
        using var table = new DataTable();
        table.Columns.Add("StringNull", typeof(string));
        table.Columns.Add("IntNull", typeof(int));
        table.Columns.Add("DateNull", typeof(DateTime));
        table.Columns.Add("DoubleNull", typeof(double));

        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        Assert.Single(result.Rows);
        var row = result.Rows[0];
        Assert.Null(row[0]);
        Assert.Null(row[1]);
        Assert.Null(row[2]);
        Assert.Null(row[3]);
    }

    [Fact]
    public void ReadFromDataReader_BinaryByteArrays_ProcessedAndFormatted()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("BinaryData", typeof(byte[]));

        var rawBytes = new byte[] { 0x00, 0x48, 0x50, 0xFF, 0xAA, 0x55 };
        table.Rows.Add(1, rawBytes);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        Assert.Single(result.Rows);
        Assert.Equal(rawBytes, result.Rows[0][1]);

        // Verify Markdown formatting doesn't throw
        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        Assert.Contains("BinaryData", md);

        // Verify JSON formatting serializes binary as base64 without throwing
        var json = PbiDaxExecutor.FormatAsJson(result);
        Assert.Contains(Convert.ToBase64String(rawBytes), json);
    }

    [Fact]
    public void ReadFromDataReader_DateTimeBoundaries_Iso8601Formatted()
    {
        using var table = new DataTable();
        table.Columns.Add("Label", typeof(string));
        // Set DateTimeMode to Utc to test UTC preserving
        var col = new DataColumn("Timestamp", typeof(DateTime)) { DateTimeMode = DataSetDateTime.Utc };
        table.Columns.Add(col);

        var minDate = DateTime.MinValue;
        var maxDate = DateTime.MaxValue;
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var leap = new DateTime(2024, 2, 29, 23, 59, 59, DateTimeKind.Utc);

        table.Rows.Add("Min", minDate);
        table.Rows.Add("Max", maxDate);
        table.Rows.Add("Epoch", epoch);
        table.Rows.Add("Leap", leap);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        Assert.Equal(4, result.RowCount);
        Assert.StartsWith("0001-01-01T00:00:00.0000000", (string)result.Rows[0][1]!);
        Assert.StartsWith("9999-12-31T23:59:59.9999999", (string)result.Rows[1][1]!);
        Assert.Equal("1970-01-01T00:00:00.0000000Z", result.Rows[2][1]);
        Assert.Equal("2024-02-29T23:59:59.0000000Z", result.Rows[3][1]);
    }

    [Fact]
    public void ReadFromDataReader_HugeStringsAndMemory_NoCrash()
    {
        using var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("LargeText", typeof(string));

        var hugeString = new string('A', 500_000); // 500 KB string
        table.Rows.Add(1, hugeString);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        Assert.Single(result.Rows);
        Assert.Equal(hugeString, result.Rows[0][1]);

        var json = PbiDaxExecutor.FormatAsJson(result);
        Assert.True(json.Length >= 500_000);
    }

    [Fact]
    public void ReadFromDataReader_ZeroRows_ProducesCleanEmptyResult()
    {
        using var table = new DataTable();
        table.Columns.Add("ColA", typeof(string));
        table.Columns.Add("ColB", typeof(int));

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, durationMs: 12, ct: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Columns.Count);
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.RowCount);
        Assert.False(result.Truncated);
        Assert.Equal(12, result.DurationMs);

        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        Assert.Contains("| ColA | ColB |", md);
        Assert.Contains("Returned 0 row(s) in 12ms", md);

        var json = PbiDaxExecutor.FormatAsJson(result);
        Assert.Contains("\"rowCount\": 0", json);
        Assert.Contains("\"rows\": []", json);
    }

    [Fact]
    public void ReadFromDataReader_Over10000Rows_TruncationEnforced()
    {
        using var table = new DataTable();
        table.Columns.Add("Seq", typeof(int));

        // Generate 12,000 rows
        for (var i = 1; i <= 12_000; i++)
        {
            table.Rows.Add(i);
        }

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10_000, durationMs: 150, ct: TestContext.Current.CancellationToken);

        Assert.Equal(10_000, result.RowCount);
        Assert.True(result.Truncated);
        Assert.Equal(1, result.Rows[0][0]);
        Assert.Equal(10_000, result.Rows[9_999][0]);

        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        Assert.Contains("*(truncated to max rows)*", md);
    }

    [Fact]
    public void Formatting_SpecialCharacters_Quotes_Newlines_Unicode()
    {
        using var table = new DataTable();
        table.Columns.Add("Scenario", typeof(string));
        table.Columns.Add("Content", typeof(string));

        table.Rows.Add("Quotes", "He said: \"Hello 'World'\" and «Bonjour»");
        table.Rows.Add("Pipes", "Column|Split|With|Pipes");
        table.Rows.Add("Newlines", "Line 1\r\nLine 2\nLine 3\rLine 4");
        table.Rows.Add("Vietnamese", "Cột bê tông cốt thép đài móng D25 a150");
        table.Rows.Add("CJK & Math", "日本語のテスト ∑(x^2) ≠ 0 ≤ ∞");
        table.Rows.Add("Emoji", "🚀📊🔥💡🏗️");
        table.Rows.Add("Backslashes", @"C:\Program Files\Microsoft Power BI Desktop\bin");

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        // 1. JSON Verification: Round-trip parse to guarantee semantic validity
        var json = PbiDaxExecutor.FormatAsJson(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(7, root.GetProperty("rowCount").GetInt32());

        var rowsElement = root.GetProperty("rows");
        Assert.Equal(7, rowsElement.GetArrayLength());

        // Verify round-tripped content from parsed JSON matches original verbatim
        var row0Content = rowsElement[0][1].GetString();
        Assert.Equal("He said: \"Hello 'World'\" and «Bonjour»", row0Content);

        var row1Content = rowsElement[1][1].GetString();
        Assert.Equal("Column|Split|With|Pipes", row1Content);

        var row2Content = rowsElement[2][1].GetString();
        Assert.Equal("Line 1\r\nLine 2\nLine 3\rLine 4", row2Content);

        var row3Content = rowsElement[3][1].GetString();
        Assert.Equal("Cột bê tông cốt thép đài móng D25 a150", row3Content);

        var row4Content = rowsElement[4][1].GetString();
        Assert.Equal("日本語のテスト ∑(x^2) ≠ 0 ≤ ∞", row4Content);

        var row5Content = rowsElement[5][1].GetString();
        Assert.Equal("🚀📊🔥💡🏗️", row5Content);

        var row6Content = rowsElement[6][1].GetString();
        Assert.Equal(@"C:\Program Files\Microsoft Power BI Desktop\bin", row6Content);

        // 2. Markdown Verification
        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        // Pipes should be escaped with \|
        Assert.Contains("Column\\|Split\\|With\\|Pipes", md);
        // Unicode text is preserved
        Assert.Contains("Cột bê tông cốt thép đài móng D25 a150", md);
        Assert.Contains("日本語のテスト", md);
        Assert.Contains("🚀📊🔥💡🏗️", md);
    }

    [Fact]
    public void ReadFromDataReader_CancellationTokenTriggered_ThrowsOperationCanceledException()
    {
        using var table = new DataTable();
        table.Columns.Add("Num", typeof(int));
        for (var i = 0; i < 100; i++)
            table.Rows.Add(i);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel token

        using var reader = table.CreateDataReader();
        Assert.Throws<OperationCanceledException>(() =>
            PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 50, ct: cts.Token));
    }

    [Fact]
    public void ReadFromDataReader_DecimalTypes_ConvertedToDouble()
    {
        using var table = new DataTable();
        table.Columns.Add("Amount", typeof(decimal));

        decimal expectedVal = 1234567.89m;
        table.Rows.Add(expectedVal);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        Assert.Single(result.Rows);
        Assert.IsType<double>(result.Rows[0][0]);
        Assert.Equal(1234567.89, (double)result.Rows[0][0]!, 2);
    }

    [Fact]
    public void FormatAsJson_FloatingPointNaNAndInfinity_SerializesCleanlyWithNamedLiterals()
    {
        using var table = new DataTable();
        table.Columns.Add("ValNaN", typeof(double));
        table.Columns.Add("ValPosInf", typeof(double));
        table.Columns.Add("ValNegInf", typeof(double));

        table.Rows.Add(double.NaN, double.PositiveInfinity, double.NegativeInfinity);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        // AllowNamedFloatingPointLiterals serializes NaN and Infinity cleanly as string literals
        var json = PbiDaxExecutor.FormatAsJson(result);
        Assert.NotNull(json);
        Assert.Contains("NaN", json);
        Assert.Contains("Infinity", json);
        Assert.Contains("-Infinity", json);
    }

    [Fact]
    public void FormatAsMarkdown_ColumnNameWithPipe_EscapedInHeader()
    {
        using var table = new DataTable();
        table.Columns.Add("Col|With|Pipes", typeof(int));
        table.Rows.Add(42);

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        // Column name in header has escaped pipe
        Assert.Contains("| Col\\|With\\|Pipes |", md);
    }

    [Fact]
    public void FormatAsMarkdown_MultilineCell_NormalizesNewlinesToSpaces()
    {
        using var table = new DataTable();
        table.Columns.Add("Description", typeof(string));
        table.Rows.Add("Line 1\r\nLine 2\nLine 3\rLine 4");

        using var reader = table.CreateDataReader();
        var result = PbiDaxExecutor.ReadFromDataReader(reader, maxRows: 10, ct: TestContext.Current.CancellationToken);

        var md = PbiDaxExecutor.FormatAsMarkdown(result);
        Assert.Contains("| Line 1 Line 2 Line 3 Line 4 |", md);
    }

    [Fact]
    public void ReadPortFile_EmptyInitially_PopulatedDuringRetry_RecoversAndSucceeds()
    {
        var filePath = Path.Combine(_tempDir, "port_transient.port.txt");
        // Initially empty (0 bytes) during file creation
        File.WriteAllBytes(filePath, Array.Empty<byte>());

        // Populate with valid port in background after 50ms
        _ = Task.Run(async () =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            File.WriteAllText(filePath, "54321", Encoding.Unicode);
        }, TestContext.Current.CancellationToken);

        // Read with 5 retries at 40ms intervals (~200ms window)
        var port = AnalysisServicesPortFinder.ReadPortFile(filePath, maxRetries: 5, retryDelayMs: 40);

        Assert.Equal(54321, port);
    }
}
