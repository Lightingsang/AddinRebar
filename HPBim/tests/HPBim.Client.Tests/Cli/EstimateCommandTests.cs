namespace HPBim.Client.Tests.Cli;

using System;
using System.IO;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Estimation;
using HPBim.Cli.Formatting;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class EstimateCommandTests
{
    [Fact]
    public void Estimate_MatchesBenchmarkRebarWeight7966_5Kg()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var estimator = new BoqEstimator();

        var report = estimator.Estimate(model);

        Assert.Equal(7966.5, report.TotalRebarKg, 1);
        Assert.Equal(4228.4, report.SmallRebarKg, 1);
        Assert.Equal(3738.1, report.LargeRebarKg, 1);
        Assert.Equal(7966.5, Math.Round(report.SmallRebarKg + report.LargeRebarKg, 1));
    }

    [Fact]
    public void Estimate_ComputesConcreteAndFormworkAccurately()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var estimator = new BoqEstimator();

        var report = estimator.Estimate(model);

        Assert.True(report.TotalConcreteM3 > 0.0);
        Assert.True(report.TotalFormworkM2 > 0.0);
        Assert.True(report.ConcreteByComponent.ContainsKey("Columns"));
        Assert.True(report.ConcreteByComponent.ContainsKey("Beams"));
        Assert.True(report.ConcreteByComponent.ContainsKey("Slabs"));
        Assert.True(report.ConcreteByComponent.ContainsKey("Walls"));
    }

    [Fact]
    public void Estimate_MeasuresMepFixturesAndPipesCorrectly()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var estimator = new BoqEstimator();

        var report = estimator.Estimate(model);

        Assert.True(report.FixturesByCategory.Count > 0);
        Assert.True(report.Pipes.Count >= 3);
    }

    [Fact]
    public async Task Execute_NhaPho5x18mFixture_ReturnsExitCode0()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"est_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            using var sw = new StringWriter();
            var cmd = new EstimateCommand(new ConsoleFormatter(sw));
            var args = CliParser.Parse(["estimate", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("7966.5 kg", sw.ToString());
            Assert.Contains("1. BÊ TÔNG", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_WithOutputFileOption_WritesValidReportFile()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"est_{Guid.NewGuid():N}.json");
        string outPath = Path.Combine(Path.GetTempPath(), $"boq_{Guid.NewGuid():N}.txt");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            using var sw = new StringWriter();
            var cmd = new EstimateCommand(new ConsoleFormatter(sw));
            var args = CliParser.Parse(["estimate", "--input", tempPath, "--output", outPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outPath));
            string content = await File.ReadAllTextAsync(outPath);
            Assert.Contains("BẢNG TỔNG HỢP KHỐI LƯỢNG BOQ", content);
            Assert.Contains("7966.5 kg", content);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    [Fact]
    public async Task Execute_MissingInput_ReturnsExitCode2()
    {
        using var sw = new StringWriter();
        var cmd = new EstimateCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["estimate"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
    }
}
