namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class ValidateCommandTests
{
    [Fact]
    public async Task Execute_NhaPho5x18mFixture_ReturnsExitCode0()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"valid_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            using var sw = new StringWriter();
            var cmd = new ValidateCommand(new ConsoleFormatter(sw));
            var args = CliParser.Parse(["validate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("[PASS] TRẠNG THÁI: SẠCH 100%", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_InvalidModelWithCoverDefect_ReturnsExitCode1()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            var defectRebars = new List<RebarDef>(model.Rebars)
            {
                new()
                {
                    Id = "DEFECT-01",
                    BarMark = 999,
                    DiameterMm = 10,
                    LengthMm = 1000,
                    Quantity = 1,
                    CoverMm = 5.0
                }
            };
            var invalidModel = model with { Rebars = defectRebars };
            await BimModelSerializer.SerializeToFileAsync(invalidModel, tempPath);

            using var sw = new StringWriter();
            var cmd = new ValidateCommand(new ConsoleFormatter(sw));
            var args = CliParser.Parse(["validate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("[ERROR] TRẠNG THÁI: KHÔNG HỢP LỆ", sw.ToString());
            Assert.Contains("TCVN-COVER-001", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_NonExistentFile_ReturnsExitCode2()
    {
        using var sw = new StringWriter();
        var cmd = new ValidateCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["validate", "--input", "nonexistent_file_12345.json"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
        Assert.Contains("[ERROR]", sw.ToString());
    }

    [Fact]
    public async Task Execute_MissingInputArg_ReturnsExitCode2()
    {
        using var sw = new StringWriter();
        var cmd = new ValidateCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["validate"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
        Assert.Contains("Tham số --input <path> là bắt buộc", sw.ToString());
    }
}
