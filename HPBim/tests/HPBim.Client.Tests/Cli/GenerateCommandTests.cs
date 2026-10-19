namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class GenerateCommandTests
{
    [Fact]
    public async Task Execute_DryRun_SendsDryRunFlagAndReturnsExitCode0()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"gen_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate", "--input", tempPath, "--dry-run"]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("DRY-RUN THÀNH CÔNG", sw.ToString());
            Assert.NotEmpty(fakeClient.ExecutedRequests);
            Assert.All(fakeClient.ExecutedRequests, r => Assert.True(r.DryRun));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_FullGeneration_ReportsAllStagesAndElementCounts()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"gen_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("DỰNG HÌNH HOÀN TẤT", sw.ToString());
            Assert.Contains("Stage 1", sw.ToString());
            Assert.Contains("Stage 8", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_InvalidModel_AbortsBeforeConnectingToPipe()
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

            var fakeClient = new FakeRevitPipeClient();
            using var sw = new StringWriter();
            var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("Pre-flight QC thất bại", sw.ToString());
            Assert.Empty(fakeClient.ExecutedRequests);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_SimulatedPipeFailure_ReturnsExitCode1()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"gen_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var fakeClient = new FakeRevitPipeClient
            {
                CustomExecuteHandler = _ => new() { IsError = true, Message = "Revit internal failure." }
            };
            using var sw = new StringWriter();
            var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakeClient);
            var args = CliParser.Parse(["generate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("DỰNG HÌNH THẤT BẠI", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_MissingInput_ReturnsExitCode2()
    {
        using var sw = new StringWriter();
        var cmd = new GenerateCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["generate"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
    }
}
