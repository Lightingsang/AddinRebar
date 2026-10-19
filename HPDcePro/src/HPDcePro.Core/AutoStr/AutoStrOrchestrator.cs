using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.AutoStr.Steps;
using HPDcePro.Core.Calculations.Takeoff;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Serialization;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Master orchestrator executing the 24-step automated structural pipeline with iterative auto-resizing.
    /// </summary>
    public class AutoStrOrchestrator
    {
        public event EventHandler<AutoStrProgress>? ProgressChanged;

        public AutoStrConfig Config { get; }
        public FramingTopology Topology { get; }
        public List<IAutoStrStep> Steps { get; } = new List<IAutoStrStep>();

        public AutoStrOrchestrator(AutoStrConfig config, FramingTopology? topology = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Topology = topology ?? new FramingTopology();
            InitializeDefaultSteps();
        }

        private void InitializeDefaultSteps()
        {
            Steps.Add(new AutoStrStep01_TaoSheetSet());
            Steps.Add(new AutoStrStep02_LuoiTruc());
            Steps.Add(new AutoStrStep03_DinhViCot());
            Steps.Add(new AutoStrStep04_MBKC());
            Steps.Add(new AutoStrStep05_DuongPhanTichLuc());
            Steps.Add(new AutoStrStep06_TaiTuong());
            Steps.Add(new AutoStrStep07_BuildEtabsModel());
            Steps.Add(new AutoStrStep08_TinhToanDamExcel());
            Steps.Add(new AutoStrStep09_VeChiTietDam());
            Steps.Add(new AutoStrStep10_TinhToanCotExcel());
            Steps.Add(new AutoStrStep11_VeChiTietCotVach());
            Steps.Add(new AutoStrStep12_VeChiTietSan());
            Steps.Add(new AutoStrStep13_MBMongCoc());
            Steps.Add(new AutoStrStep14_DPTLMong());
            Steps.Add(new AutoStrStep15_EtabsMong());
            Steps.Add(new AutoStrStep16_TTMong());
            Steps.Add(new AutoStrStep17_VeMong());
            Steps.Add(new AutoStrStep18_TTGiangM());
            Steps.Add(new AutoStrStep19_VeGiangM());
            Steps.Add(new AutoStrStep20_CKPhu());
            Steps.Add(new AutoStrStep21_DanhMuc());
            Steps.Add(new AutoStrStep22_MoSheetSet());
            Steps.Add(new AutoStrStep23_InAn());
            Steps.Add(new AutoStrStep24_TMTT());
        }

        public async Task<AutoStrPipelineResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var context = CreateContext();
            return await ExecuteInternalAsync(context, startStepIndex: 0, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AutoStrPipelineResult> ResumeFromCheckpointAsync(
            string checkpointPath, CancellationToken cancellationToken = default)
        {
            var checkpoint = AutoStrCheckpoint.LoadFromFile(checkpointPath);
            var context = new AutoStrStepContext(checkpoint.Config, checkpoint.Config.ProjectRootPath, Topology, OnReportProgress);

            int startStepIndex = checkpoint.LastCompletedStepNumber; // 0-based index for next step
            return await ExecuteInternalAsync(context, startStepIndex, cancellationToken).ConfigureAwait(false);
        }

        private AutoStrStepContext CreateContext()
        {
            return new AutoStrStepContext(Config, Config.ProjectRootPath, Topology, OnReportProgress);
        }

        private void OnReportProgress(int stepNum, string stepName, double pct)
        {
            ProgressChanged?.Invoke(this, new AutoStrProgress(stepNum, stepName, pct, $"Đang xử lý bước {stepNum}: {stepName}"));
        }

        private async Task<AutoStrPipelineResult> ExecuteInternalAsync(
            AutoStrStepContext context, int startStepIndex, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var stepResults = new List<AutoStrStepResult>();
            string checkpointPath = GetCheckpointPath(context);

            for (int i = startStepIndex; i < Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = Steps[i];
                var res = await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                stepResults.Add(res);

                SaveCheckpoint(context, stepResults, checkpointPath, step.StepNumber);

                if (!res.IsSuccess)
                {
                    sw.Stop();
                    return BuildPipelineResult(false, stepResults, sw.Elapsed, context, checkpointPath);
                }

                // Check resizing loop after column analysis (step 10) or foundation analysis (step 15)
                if (step.StepNumber == 10 || step.StepNumber == 15)
                {
                    await CheckAndRunResizingLoopAsync(context, step.StepNumber, cancellationToken).ConfigureAwait(false);
                }
            }

            sw.Stop();
            return BuildPipelineResult(true, stepResults, sw.Elapsed, context, checkpointPath);
        }

        private async Task CheckAndRunResizingLoopAsync(
            AutoStrStepContext context, int triggerStep, CancellationToken cancellationToken)
        {
            int maxIterations = 5;
            while (context.CurrentIteration < maxIterations)
            {
                bool columnResized = false;
                bool pileResized = false;

                if (triggerStep == 10 && Config.CotChoTangTietDien)
                {
                    columnResized = TryResizeOverstressedColumns(context);
                }
                else if (triggerStep == 15 && Config.CocTuTangKhiKhongDat)
                {
                    pileResized = TryResizeOverloadedPiles(context);
                }

                if (!columnResized && !pileResized)
                {
                    break;
                }

                context.CurrentIteration++;
                context.LogInfo(triggerStep, $"Lặp điều chỉnh tiết diện lần {context.CurrentIteration}: Đang tính toán lại...");

                // Re-evaluate the trigger step
                var step = Steps.First(s => s.StepNumber == triggerStep);
                await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        public bool TryResizeOverstressedColumns(AutoStrStepContext context)
        {
            var overstressed = context.GetArtifact<List<ColumnDesignCheckResult>>("OverstressedColumns");
            if (overstressed == null || overstressed.Count == 0) return false;

            var colDefs = context.GetArtifact<List<ColumnSectionDefinition>>("ColumnDefinitions");
            if (colDefs == null) return false;

            bool anyResized = false;
            double step = Config.CotBuocTang > 0 ? Config.CotBuocTang : 50.0;
            double maxFactor = Config.CotHeSoTangToiDa > 1.0 ? Config.CotHeSoTangToiDa : 2.0;

            foreach (var col in overstressed)
            {
                var def = colDefs.FirstOrDefault(c => c.Mark == col.Mark);
                if (def == null) continue;

                double initialB = 300.0;
                double maxDim = initialB * maxFactor;

                if (def.WidthMm + step <= maxDim && def.HeightMm + step <= maxDim)
                {
                    def.WidthMm += step;
                    def.HeightMm += step;
                    def.UtilizationRatio = Math.Round(def.UtilizationRatio * (initialB * initialB) / (def.WidthMm * def.HeightMm), 2);
                    context.LogInfo(10, $"Cột {def.Mark} tự động tăng tiết diện lên {def.WidthMm}x{def.HeightMm}mm (UR mới={def.UtilizationRatio:F2}).");
                    anyResized = true;
                }
                else
                {
                    context.LogWarning(10, $"Cột {def.Mark} đã đạt giới hạn phóng to tối đa ({maxFactor:F1}x = {maxDim}mm).");
                }
            }
            return anyResized;
        }

        public bool TryResizeOverloadedPiles(AutoStrStepContext context)
        {
            var overloaded = context.GetArtifact<List<PileCapLayoutDefinition>>("OverloadedPileCaps");
            if (overloaded == null || overloaded.Count == 0) return false;

            bool anyResized = false;
            foreach (var cap in overloaded)
            {
                int oldPiles = cap.PileCount;
                cap.PileCount++;
                cap.MaxPileReactionKn = Math.Round(cap.MaxPileReactionKn * oldPiles / cap.PileCount, 2);
                context.LogInfo(15, $"Đài {cap.Mark} tự động tăng số lượng cọc từ {oldPiles} lên {cap.PileCount} (P_max mới={cap.MaxPileReactionKn:F1} kN).");
                anyResized = true;
            }
            return anyResized;
        }

        private static string GetCheckpointPath(AutoStrStepContext context)
        {
            return !string.IsNullOrWhiteSpace(context.ProjectRootPath)
                ? Path.Combine(context.ProjectRootPath, "checkpoint.json")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "checkpoint.json");
        }

        private void SaveCheckpoint(
            AutoStrStepContext context, List<AutoStrStepResult> results, string path, int lastStep)
        {
            try
            {
                var cp = new AutoStrCheckpoint
                {
                    ProjectName = Config.TenDuAn,
                    Timestamp = DateTime.UtcNow,
                    LastCompletedStepNumber = lastStep,
                    Config = Config
                };
                foreach (var r in results)
                {
                    cp.CompletedSteps.Add(new AutoStrStepCheckpointRecord
                    {
                        StepNumber = r.StepNumber,
                        StepName = r.StepName,
                        IsSuccess = r.IsSuccess,
                        IsSkipped = r.IsSkipped,
                        DurationSeconds = r.Duration.TotalSeconds,
                        Message = r.Message,
                        OutputFiles = r.OutputFiles
                    });
                }
                AutoStrCheckpoint.SaveToFile(cp, path);
            }
            catch (Exception ex)
            {
                // Reason: Checkpoint writing failure should not abort pipeline execution.
                context.LogWarning(lastStep, $"Không thể ghi checkpoint: {ex.Message}");
            }
        }

        private static AutoStrPipelineResult BuildPipelineResult(
            bool isSuccess, List<AutoStrStepResult> stepResults, TimeSpan duration,
            AutoStrStepContext context, string checkpointPath)
        {
            return new AutoStrPipelineResult
            {
                IsSuccess = isSuccess,
                CompletedStepsCount = stepResults.Count(r => r.IsSuccess),
                StepResults = stepResults,
                TotalDuration = duration,
                Logs = context.Logs,
                TakeoffResult = context.GetArtifact<QuantityTakeoffResult>("TakeoffResult"),
                ResizingIterationsCount = context.CurrentIteration,
                CheckpointFilePath = File.Exists(checkpointPath) ? checkpointPath : null,
                Context = context
            };
        }
    }
}
