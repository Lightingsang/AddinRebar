namespace HPBim.Client.Tests.Cli;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class TruthfulRollbackCliTests
{
    [Fact]
    public async Task GenerateCommand_DryRunWithoutRollback_ReturnsExitCode1AndWarns()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"gen_no_rb_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = req => new ExecuteResult
                {
                    IsError = false,
                    Changed = new ChangedCounts(5, 0, 0),
                    RolledBack = false // Revit failed to rollback!
                }
            };

            using var sw = new StringWriter();
            var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            string output = sw.ToString();
            Assert.Contains("CẢNH BÁO NGUY HIỂM", output);
            Assert.Contains("KHÔNG xác nhận rollback", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateFoundationCommand_DryRunWithoutRollback_ReturnsExitCode1AndWarns()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"fnd_no_rb_{Guid.NewGuid():N}.json");
        try
        {
            var gridData = new
            {
                origin = new { cad_x_mm = 0.0, cad_y_mm = 0.0 },
                grids = new
                {
                    x = new[] { new { name = "1", x = 0.0 } },
                    y = new[] { new { name = "A", y = 0.0 } }
                }
            };
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(gridData));

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = req => new ExecuteResult
                {
                    IsError = false,
                    Changed = new ChangedCounts(2, 0, 0),
                    RolledBack = false // Bridge failed rollback
                }
            };

            using var sw = new StringWriter();
            var cmd = new GenerateFoundationCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate-foundation", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            string output = sw.ToString();
            Assert.Contains("CẢNH BÁO NGUY HIỂM", output);
            Assert.Contains("KHÔNG xác nhận rollback", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateColumnsCommand_DryRunWithoutRollback_ReturnsExitCode5AndWarns()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"cols_no_rb_{Guid.NewGuid():N}.json");
        try
        {
            var colsPayload = new
            {
                typesNeeded = new[] { new { name = "C_300x300", widthMm = 300.0, heightMm = 300.0, count = 1 } },
                columns = new[] { new { index = 1, typeName = "C_300x300", widthMm = 300.0, heightMm = 300.0, xMm = 1000.0, yMm = 2000.0 } }
            };
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(colsPayload));

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = req =>
                {
                    var payload = new { coCotCreated = 72, cotTang1Created = 72, typesCreated = 0 };
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
                    using var doc = JsonDocument.Parse(jsonBytes);

                    return new ExecuteResult
                    {
                        IsError = false,
                        Changed = new ChangedCounts(144, 0, 0),
                        RolledBack = false, // Not rolled back!
                        Value = doc.RootElement.Clone()
                    };
                }
            };

            using var sw = new StringWriter();
            var cmd = new GenerateColumnsCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate-columns", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(5, exitCode);
            string output = sw.ToString();
            Assert.Contains("CẢNH BÁO NGUY HIỂM", output);
            Assert.Contains("KHÔNG xác nhận rollback", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateGradeBeamsCommand_DryRunWithoutRollback_ReturnsExitCode1AndWarns()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"gb_no_rb_{Guid.NewGuid():N}.json");
        try
        {
            var gbPayload = new
            {
                beams = new[]
                {
                    new
                    {
                        name = "DK1",
                        width = 200.0,
                        height = 400.0,
                        handle = "H_DK1",
                        supports = new[] { "COL1", "COL2" },
                        StartX = 0.0, StartY = 0.0, EndX = 4000.0, EndY = 0.0
                    }
                }
            };
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(gbPayload));

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = req =>
                {
                    var payload = new { createdCount = 205, targetLevel = "Tầng Móng", topElevationMm = -1000.0 };
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
                    using var doc = JsonDocument.Parse(jsonBytes);

                    return new ExecuteResult
                    {
                        IsError = false,
                        Changed = new ChangedCounts(205, 0, 0),
                        RolledBack = false, // Not rolled back!
                        Value = doc.RootElement.Clone()
                    };
                }
            };

            using var sw = new StringWriter();
            var cmd = new GenerateGradeBeamsCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate-grade-beams", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            string output = sw.ToString();
            Assert.Contains("CẢNH BÁO NGUY HIỂM", output);
            Assert.Contains("KHÔNG xác nhận rollback", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task GenerateElevatorWallsCommand_DryRunWithoutRollback_ReturnsExitCode5AndWarns()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"walls_no_rb_{Guid.NewGuid():N}.json");
        try
        {
            var wallsPayload = new
            {
                walls = new[]
                {
                    new { x0Mm = 0.0, y0Mm = 0.0, x1Mm = 2000.0, y1Mm = 0.0 }
                }
            };
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(wallsPayload));

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = req =>
                {
                    var payload = new { coVachCreated = 8, vachTang1Created = 8, wallTypeUsed = "2_Wall_BTCT_D200" };
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
                    using var doc = JsonDocument.Parse(jsonBytes);

                    return new ExecuteResult
                    {
                        IsError = false,
                        Changed = new ChangedCounts(16, 0, 0),
                        RolledBack = false, // Not rolled back!
                        Value = doc.RootElement.Clone()
                    };
                }
            };

            using var sw = new StringWriter();
            var cmd = new GenerateElevatorWallsCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate-elevator-walls", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(5, exitCode);
            string output = sw.ToString();
            Assert.Contains("CẢNH BÁO NGUY HIỂM", output);
            Assert.Contains("KHÔNG xác nhận rollback", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
