namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Verification;
using HPBim.Core.Execution;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPBim.Core.Verification;
using Xunit;

public sealed class VerifyCommandTests
{
    private sealed class FakeVerifier : IRevitModelVerifier
    {
        public bool ReturnMatch { get; set; } = true;

        public Task<ModelVerificationReport> VerifyAsync(BimModel expectedModel, CancellationToken cancellationToken = default)
        {
            var report = ReturnMatch
                ? new ModelVerificationReport
                {
                    IsMatch = true,
                    ExpectedLevels = 5, ActualLevels = 5,
                    ExpectedGrids = 7, ActualGrids = 7,
                    ExpectedColumns = 10, ActualColumns = 10,
                    ExpectedWalls = 26, ActualWalls = 26,
                    ExpectedDoors = 7, ActualDoors = 7,
                    ExpectedWindows = 16, ActualWindows = 16,
                    ExpectedRooms = 16, ActualRooms = 16,
                    ExpectedBeams = 24, ActualBeams = 24,
                    ExpectedSlabs = 12, ActualSlabs = 12,
                    ExpectedPipes = 3, ActualPipes = 3,
                    ExpectedRebars = 33, ActualRebars = 33,
                    Discrepancies = Array.Empty<string>()
                }
                : new ModelVerificationReport
                {
                    IsMatch = false,
                    ExpectedLevels = 5, ActualLevels = 4,
                    ExpectedColumns = 10, ActualColumns = 8,
                    Discrepancies = ["Levels mismatch: expected 5, found 4.", "Columns mismatch: expected 10, found 8."]
                };

            return Task.FromResult(report);
        }
    }

    [Fact]
    public async Task Execute_MatchingModel_ReturnsExitCode0AndMatchReport()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"ver_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var verifier = new FakeVerifier { ReturnMatch = true };
            using var sw = new StringWriter();
            var cmd = new VerifyCommand(new ConsoleFormatter(sw), verifier);
            var args = CliParser.Parse(["verify", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("ĐỐI SOÁT HOÀN HẢO", sw.ToString());
            Assert.Contains("Levels", sw.ToString());
            Assert.Contains("Columns", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_MismatchedModel_ReturnsExitCode1AndDiscrepancies()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"ver_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var verifier = new FakeVerifier { ReturnMatch = false };
            using var sw = new StringWriter();
            var cmd = new VerifyCommand(new ConsoleFormatter(sw), verifier);
            var args = CliParser.Parse(["verify", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("PHÁT HIỆN SAI LỆCH", sw.ToString());
            Assert.Contains("Levels mismatch", sw.ToString());
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
        var cmd = new VerifyCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["verify", "--input", "missing_file_567.json"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
        Assert.Contains("[ERROR]", sw.ToString());
    }

    [Fact]
    public async Task Execute_MissingInput_ReturnsExitCode2()
    {
        using var sw = new StringWriter();
        var cmd = new VerifyCommand(new ConsoleFormatter(sw));
        var args = CliParser.Parse(["verify"]);

        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(2, exitCode);
    }

    private sealed class FakeGeometricVerifier : IMeasuredGeometricVerifier
    {
        public bool ReturnPass { get; set; } = true;
        public GeometricVerificationStatus ReturnStatus { get; set; } = GeometricVerificationStatus.Verified;

        public Task<GeometricAuditReport> VerifyPlanGeometryAsync(
            BimExecutionPlan plan,
            HPBim.Core.Coordination.CoordinationTolerances? tolerances = null,
            CancellationToken cancellationToken = default)
        {
            var status = ReturnStatus != GeometricVerificationStatus.Verified
                ? ReturnStatus
                : (ReturnPass ? GeometricVerificationStatus.Verified : GeometricVerificationStatus.Defect);
            var reason = status switch
            {
                GeometricVerificationStatus.Defect => "Elevation error exceeds tolerance",
                GeometricVerificationStatus.Inconclusive => "Rule 0: Revit element exists but physical geometry was unmeasurable or degenerated.",
                _ => null
            };
            var discrepancies = reason != null ? new[] { reason } : Array.Empty<string>();
            var audit = new ElementGeometricAudit
            {
                Status = status,
                DefectReason = reason,
                Discrepancies = discrepancies
            };
            return Task.FromResult(GeometricAuditReport.Create(plan.PlanId, plan.PlanHash, new[] { audit }));
        }
    }

    [Fact]
    public async Task Execute_SmokeMatches_ButGeometricDefect_ReturnsExitCode1()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"ver_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var verifier = new FakeVerifier { ReturnMatch = true };
            var geomVerifier = new FakeGeometricVerifier { ReturnPass = false };
            using var sw = new StringWriter();
            var cmd = new VerifyCommand(new ConsoleFormatter(sw), verifier, geomVerifier);
            var args = CliParser.Parse(["verify", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("NGHIỆM THU HÌNH HỌC THẤT BẠI", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_SmokeMatches_ButGeometricInconclusive_ReturnsExitCode1()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"ver_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var verifier = new FakeVerifier { ReturnMatch = true };
            var geomVerifier = new FakeGeometricVerifier { ReturnStatus = GeometricVerificationStatus.Inconclusive };
            using var sw = new StringWriter();
            var cmd = new VerifyCommand(new ConsoleFormatter(sw), verifier, geomVerifier);
            var args = CliParser.Parse(["verify", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            string output = sw.ToString();
            Assert.Contains("NGHIỆM THU HÌNH HỌC THẤT BẠI", output);
            Assert.Contains("chưa rõ", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_SmokeAndGeometricBothPass_ReturnsExitCode0()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"ver_{Guid.NewGuid():N}.json");
        try
        {
            var model = NhaPho5x18mFixture.CreateModel();
            await BimModelSerializer.SerializeToFileAsync(model, tempPath);

            var verifier = new FakeVerifier { ReturnMatch = true };
            var geomVerifier = new FakeGeometricVerifier { ReturnPass = true };
            using var sw = new StringWriter();
            var cmd = new VerifyCommand(new ConsoleFormatter(sw), verifier, geomVerifier);
            var args = CliParser.Parse(["verify", "--input", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("ĐỐI SOÁT HOÀN HẢO", sw.ToString());
            Assert.Contains("100% ĐẠT TIÊU CHÍ HÌNH HỌC", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
