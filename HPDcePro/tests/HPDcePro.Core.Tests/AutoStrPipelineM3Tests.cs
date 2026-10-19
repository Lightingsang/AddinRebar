using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.AutoStr.Steps;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Takeoff;
using HPDcePro.Core.Serialization;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class AutoStrPipelineM3Tests
    {
        [Fact]
        public async Task AutoStrPipeline_Executes24StepsWithRealCalculations()
        {
            var config = new AutoStrConfig
            {
                TenDuAn = "DuAnM3Test",
                MacBtCotVach = "B25",
                MacBtDamSan = "B25",
                MacThep = "CB400-V",
                ProjectRootPath = Path.Combine(Path.GetTempPath(), $"AutoStrM3_{Guid.NewGuid():N}")
            };

            var orch = new AutoStrOrchestrator(config);
            var result = await orch.ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(24, result.CompletedStepsCount);
            Assert.Equal(24, result.StepResults.Count);

            // Step 7: Etabs Model Build
            Assert.NotNull(result.Context);
            var etabsBuilt = result.Context.StepArtifacts.TryGetValue("EtabsModelInitialized", out var eb) && (bool)eb;
            Assert.True(etabsBuilt);

            // Step 8: Beam calculations
            var flexureResults = result.Context.GetArtifact<Dictionary<string, BeamFlexureResult>>("BeamFlexureResults");
            var shearResults = result.Context.GetArtifact<Dictionary<string, BeamShearResult>>("BeamShearResults");
            Assert.NotNull(flexureResults);
            Assert.NotEmpty(flexureResults!);
            Assert.NotNull(shearResults);
            Assert.NotEmpty(shearResults!);

            // Step 10: Column PMM
            var columnChecks = result.Context.GetArtifact<List<ColumnDesignCheckResult>>("ColumnCheckResults");
            Assert.NotNull(columnChecks);
            Assert.NotEmpty(columnChecks!);

            // Step 15: Foundation pile reactions
            var maxReaction = result.Context.StepArtifacts.TryGetValue("MaxPileReactionGlobalKn", out var mr) ? (double)mr : 0.0;
            Assert.True(maxReaction > 0.0);

            // Step 16: Pile caps
            var pileCapResults = result.Context.GetArtifact<Dictionary<string, PileCapResult>>("PileCapCalculationResults");
            Assert.NotNull(pileCapResults);
            Assert.NotEmpty(pileCapResults!);

            // Step 18: Foundation beams
            var foundationBeams = result.Context.GetArtifact<Dictionary<string, FoundationBeamResult>>("TieBeamResults");
            Assert.NotNull(foundationBeams);
            Assert.NotEmpty(foundationBeams!);

            // Step 21: BOQ Takeoff
            var boq = result.Context.GetArtifact<QuantityTakeoffResult>("TakeoffResult");
            Assert.NotNull(boq);
            Assert.True(boq!.TotalConcreteVolumeM3 > 0);
            Assert.True(boq.TotalRebarTons > 0);

            // Step 23: Batch Plot Sheets
            var plotSheets = result.Context.GetArtifact<List<string>>("BatchPlotSheets");
            Assert.NotNull(plotSheets);
            Assert.NotEmpty(plotSheets!);

            // Step 24: TMTT Report
            var reportReady = result.Context.StepArtifacts.TryGetValue("ReportReady", out var rr) && (bool)rr;
            var reportSummary = result.Context.GetArtifact<string>("CalculationReportSummary");
            Assert.True(reportReady);
            Assert.False(string.IsNullOrWhiteSpace(reportSummary));
            Assert.Contains("TCVN 5574:2018", reportSummary);
            Assert.Contains("TỔNG HỢP KHỐI LƯỢNG VẬT TƯ (BOQ)", reportSummary);

            if (Directory.Exists(config.ProjectRootPath))
            {
                // Reason: Clean up temporary test output folder
                Directory.Delete(config.ProjectRootPath, true);
            }
        }
    }
}
