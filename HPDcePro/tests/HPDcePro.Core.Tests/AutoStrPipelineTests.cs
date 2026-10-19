using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class AutoStrPipelineTests
    {
        [Fact]
        public void Pipeline_InitializesAll24Steps_InOrder()
        {
            var config = new AutoStrConfig();
            var orch = new AutoStrOrchestrator(config);

            Assert.Equal(24, orch.Steps.Count);
            for (int i = 0; i < 24; i++)
            {
                Assert.Equal(i + 1, orch.Steps[i].StepNumber);
                Assert.False(string.IsNullOrWhiteSpace(orch.Steps[i].StepName));
            }
        }

        [Fact]
        public async Task Pipeline_ExecutesEndToEnd_Successfully()
        {
            var config = new AutoStrConfig
            {
                TenDuAn = "DuAnTestE2E",
                ProjectRootPath = Path.Combine(Path.GetTempPath(), $"AutoStr_{Guid.NewGuid():N}")
            };
            var orch = new AutoStrOrchestrator(config);

            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(24, result.CompletedStepsCount);
            Assert.Equal(24, result.StepResults.Count);
            Assert.NotNull(result.TakeoffResult);

            if (Directory.Exists(config.ProjectRootPath))
            {
                Directory.Delete(config.ProjectRootPath, true);
            }
        }

        [Fact]
        public async Task Pipeline_RespectsDisabledSteps()
        {
            var config = new AutoStrConfig();
            config.StepEnabled = new List<bool>();
            for (int i = 1; i <= 24; i++)
            {
                // Disable steps 5 and 6
                config.StepEnabled.Add(i != 5 && i != 6);
            }

            var orch = new AutoStrOrchestrator(config);
            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            var step05 = result.StepResults.First(r => r.StepNumber == 5);
            var step06 = result.StepResults.First(r => r.StepNumber == 6);
            var step07 = result.StepResults.First(r => r.StepNumber == 7);

            Assert.True(step05.IsSkipped);
            Assert.True(step06.IsSkipped);
            Assert.False(step07.IsSkipped);
        }

        [Fact]
        public async Task Pipeline_SharesBlackboardArtifacts_BetweenSteps()
        {
            var config = new AutoStrConfig();
            var topo = new FramingTopology();
            topo.ColumnNodes.Add(new Point2D(0, 0));
            topo.ColumnNodes.Add(new Point2D(4000, 0));

            var orch = new AutoStrOrchestrator(config, topo);
            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            var step03 = result.StepResults.First(r => r.StepNumber == 3);
            var step04 = result.StepResults.First(r => r.StepNumber == 4);
            var step21 = result.StepResults.First(r => r.StepNumber == 21);

            Assert.True(step03.IsSuccess);
            Assert.True(step04.IsSuccess);
            Assert.True(step21.IsSuccess);
        }

        [Fact]
        public async Task Pipeline_SavesCheckpoint_OnCompletion()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"AutoStrCP_{Guid.NewGuid():N}");
            var config = new AutoStrConfig
            {
                TenDuAn = "DuAnCheckpoint",
                ProjectRootPath = tempDir
            };
            var orch = new AutoStrOrchestrator(config);

            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            string cpFile = Path.Combine(tempDir, "checkpoint.json");
            Assert.True(File.Exists(cpFile));

            var loaded = AutoStrCheckpoint.LoadFromFile(cpFile);
            Assert.Equal("DuAnCheckpoint", loaded.ProjectName);
            Assert.Equal(24, loaded.LastCompletedStepNumber);
            Assert.Equal(24, loaded.CompletedSteps.Count);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Pipeline_ResumesFromCheckpoint_Successfully()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"AutoStrResume_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string cpFile = Path.Combine(tempDir, "checkpoint.json");

            var config = new AutoStrConfig
            {
                TenDuAn = "DuAnResume",
                ProjectRootPath = tempDir
            };
            var cp = new AutoStrCheckpoint
            {
                ProjectName = config.TenDuAn,
                LastCompletedStepNumber = 12,
                Config = config
            };
            for (int i = 1; i <= 12; i++)
            {
                cp.CompletedSteps.Add(new AutoStrStepCheckpointRecord
                {
                    StepNumber = i,
                    StepName = $"Step {i}",
                    IsSuccess = true
                });
            }
            AutoStrCheckpoint.SaveToFile(cp, cpFile);

            var orch = new AutoStrOrchestrator(config);
            var result = await orch.ResumeFromCheckpointAsync(cpFile);

            Assert.True(result.IsSuccess);
            Assert.Equal(12, result.StepResults.Count); // Steps 13 to 24 executed
            Assert.Equal(13, result.StepResults[0].StepNumber);
            Assert.Equal(24, result.StepResults[11].StepNumber);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task Pipeline_HandlesCancellation_Gracefully()
        {
            var config = new AutoStrConfig();
            var orch = new AutoStrOrchestrator(config);
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await orch.ExecuteAsync(cts.Token);
            });
        }

        [Fact]
        public async Task Pipeline_ReportsProgress_ToSubscriber()
        {
            var config = new AutoStrConfig();
            var orch = new AutoStrOrchestrator(config);
            var progressEvents = new List<AutoStrProgress>();

            orch.ProgressChanged += (s, e) => progressEvents.Add(e);
            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.True(progressEvents.Count >= 24);
            Assert.Contains(progressEvents, p => p.StepNumber == 1);
            Assert.Contains(progressEvents, p => p.StepNumber == 24);
        }

        [Fact]
        public async Task Pipeline_LogsExecutionDetails()
        {
            var config = new AutoStrConfig();
            var orch = new AutoStrOrchestrator(config);

            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Logs);
            Assert.Contains(result.Logs, l => l.StepNumber == 1 && l.Message.Contains("Bắt đầu"));
            Assert.Contains(result.Logs, l => l.StepNumber == 24 && l.Message.Contains("Hoàn thành"));
        }
    }
}
