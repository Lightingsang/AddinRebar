namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Recovery;
using HPBim.Core.Coordination;
using Xunit;

public sealed class CoordinationCommandTests
{
    [Fact]
    public async Task Execute_Init_CreatesLedgerFileWithDefaultValues()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_init_{Guid.NewGuid():N}.json");
        try
        {
            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(new ConsoleFormatter(sw));
            var args = CliParser.Parse(["coord", "init", "--project", "Hospital Project", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));
            var content = await File.ReadAllTextAsync(tempPath);
            Assert.Contains("Hospital Project", content);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_Status_PrintsLifecycleProgress()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_status_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Test Status Project");
            service.SaveToFile(tempPath);

            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(new ConsoleFormatter(sw), service);
            var args = CliParser.Parse(["coord", "status", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            string output = sw.ToString();
            Assert.Contains("Test Status Project", output);
            Assert.Contains("TIẾN ĐỘ 7 BƯỚC VÒNG ĐỜI CẤU KIỆN", output);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_Check_ValidElement_Returns0()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_chk_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Check Project");
            service.RegisterElement(new CoordinationElementRecord
            {
                Id = "COL_01",
                Code = "C1",
                Category = CoordinationCategory.Column,
                CadSourceSpec = new CadSourceSpec
                {
                    Handle = "HD_01",
                    Layer = "COT",
                    Width = 300,
                    Height = 400,
                    BottomElevation = 0,
                    TopElevation = 3900,
                    Supports = new[] { "FND_01" },
                    CrossCheckPassed = true
                }
            });
            service.SaveToFile(tempPath);

            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(new ConsoleFormatter(sw), service);
            var args = CliParser.Parse(["coord", "check", "--id", "COL_01", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("ĐỦ CƠ SỞ KỸ THUẬT", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_Check_InvalidElement_Returns1WithViolations()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_chk_fail_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Check Project");
            service.RegisterElement(new CoordinationElementRecord
            {
                Id = "BEAM_BAD",
                Code = "B1",
                Category = CoordinationCategory.Beam,
                CadSourceSpec = new CadSourceSpec { Handle = "HD_02" } // missing dimensions & elevations
            });
            service.SaveToFile(tempPath);

            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(new ConsoleFormatter(sw), service);
            var args = CliParser.Parse(["coord", "check", "--id", "BEAM_BAD", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("VI PHẠM TIÊU CHUẨN KỸ THUẬT", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_Reconcile_CommittedReceipt_UpdatesLedgerAndSavesToFile()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_rec_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Reconcile Test");
            service.SaveToFile(tempPath);

            var fakeReconcile = new FakeReconcileService();
            var mappings = new Dictionary<string, string>
            {
                ["TestDoc#Column:H_101"] = "UniqueId-Column-101",
                ["TestDoc#Beam:H_102"] = "UniqueId-Beam-102"
            };
            var committedResult = ReconcileResult.Committed(Guid.NewGuid(), "hash_abc", mappings);
            fakeReconcile.AllIntentsResults = new[] { committedResult };

            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(
                new ConsoleFormatter(sw),
                service,
                reconcileService: fakeReconcile);
            var args = CliParser.Parse(["coord", "reconcile", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(0, exitCode);
            Assert.Contains("[COMMITTED]", sw.ToString());
            Assert.Contains("Đã khôi phục 2 mappings", sw.ToString());

            // Reload ledger and check mappings
            var reloaded = service.LoadFromFile(tempPath);
            Assert.Equal(2, reloaded.Mappings.Count);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Execute_Reconcile_StillUnknown_ReturnsExitCode1()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"coord_rec_unk_{Guid.NewGuid():N}.json");
        try
        {
            var service = new BimCoordinationLedgerService();
            service.Initialize("Unknown Test");
            service.SaveToFile(tempPath);

            var fakeReconcile = new FakeReconcileService();
            fakeReconcile.AllIntentsResults = new[]
            {
                ReconcileResult.StillUnknown(Guid.NewGuid(), "hash_xyz", "Revit Named Pipe timeout")
            };

            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(
                new ConsoleFormatter(sw),
                service,
                reconcileService: fakeReconcile);
            var args = CliParser.Parse(["coord", "reconcile", "--ledger", tempPath]);

            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("[UNKNOWN]", sw.ToString());
            Assert.Contains("Revit Named Pipe timeout", sw.ToString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private sealed class FakeReconcileService : IReconcileService
    {
        public IReadOnlyList<ReconcileResult> AllIntentsResults { get; set; } = new[] { ReconcileResult.NoOp() };
        public ReconcileResult SingleResult { get; set; } = ReconcileResult.NoOp();

        public Task<IReadOnlyList<ReconcileResult>> ReconcileAllUnknownIntentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AllIntentsResults);

        public Task<ReconcileResult> ReconcileIntentAsync(Guid operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SingleResult);
    }
}
