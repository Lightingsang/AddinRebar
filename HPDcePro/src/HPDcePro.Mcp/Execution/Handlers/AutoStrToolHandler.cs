using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.AutoStr;
using HPDcePro.Core.Calculations.Takeoff;
using HPDcePro.Core.Common;
using HPDcePro.Core.Serialization;
using HPDcePro.Excel;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class AutoStrToolHandler
    {
        public async Task<DceProToolResult> ExecuteAsync(JsonElement? args, CancellationToken ct = default)
        {
            string op = JsonArgHelper.GetString(args, "operation", "load_config").ToLowerInvariant();
            return op switch
            {
                "load_config" => LoadConfig(args),
                "run_pipeline" => await RunPipelineAsync(args, ct).ConfigureAwait(false),
                "export_boq" => ExportBoq(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_run_autostr_pipeline.")
            };
        }

        private static AutoStrConfig ResolveConfig(JsonElement? args)
        {
            string filePath = JsonArgHelper.GetString(args, "autostrFilePath", string.Empty);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = JsonArgHelper.GetString(args, "configPath", string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"AutoStr configuration file not found: {filePath}", filePath);
                }
                return AutoStrProjectSerializer.DeserializeFromFile(filePath);
            }

            string defaultPath = AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");
            if (File.Exists(defaultPath))
            {
                return AutoStrProjectSerializer.DeserializeFromFile(defaultPath);
            }

            var cfg = new AutoStrConfig
            {
                TenDuAn = JsonArgHelper.GetString(args, "projectName", "Dự án mới AutoStr"),
                MacBtCotVach = "B25",
                MacBtDamSan = "B25",
                MacThep = "CB400-V"
            };
            cfg.Floors.Add(new AutoStrFloorInfo { StoryIndex = 1, TenTang = "TẦNG 1", CaoDoTang = 0.0, ChieuCaoTang = 3900.0 });
            cfg.Floors.Add(new AutoStrFloorInfo { StoryIndex = 2, TenTang = "TẦNG 2", CaoDoTang = 3900.0, ChieuCaoTang = 3600.0 });
            cfg.Floors.Add(new AutoStrFloorInfo { StoryIndex = 3, TenTang = "TẦNG MÁI", CaoDoTang = 7500.0, ChieuCaoTang = 3300.0 });
            return cfg;
        }

        private static DceProToolResult LoadConfig(JsonElement? args)
        {
            var config = ResolveConfig(args);
            return DceProToolResult.Success(new
            {
                projectName = config.TenDuAn,
                concreteGradeColumns = config.MacBtCotVach,
                concreteGradeBeamsSlabs = config.MacBtDamSan,
                steelGrade = config.MacThep,
                floorsCount = config.Floors.Count,
                typicalBeamB = config.BeRongDamDienHinh,
                typicalBeamH = config.ChieuCaoDamDienHinh,
                typicalSlabH = config.ChieuDaySanDienHinh
            });
        }

        private static async Task<DceProToolResult> RunPipelineAsync(JsonElement? args, CancellationToken ct)
        {
            var config = ResolveConfig(args);
            var orch = new AutoStrOrchestrator(config);

            string chkPath = JsonArgHelper.GetString(args, "checkpointPath", string.Empty);
            if (!string.IsNullOrWhiteSpace(chkPath) && File.Exists(chkPath))
            {
                var chkResult = await orch.ResumeFromCheckpointAsync(chkPath, ct).ConfigureAwait(false);
                return DceProToolResult.Success(chkResult);
            }

            int maxSteps = JsonArgHelper.GetInt(args, "maxSteps", 24);
            if (maxSteps > 0 && maxSteps < orch.Steps.Count)
            {
                orch.Steps.RemoveRange(maxSteps, orch.Steps.Count - maxSteps);
            }

            var result = await orch.ExecuteAsync(ct).ConfigureAwait(false);
            return DceProToolResult.Success(new
            {
                isSuccess = result.IsSuccess,
                totalStepsExecuted = result.StepResults.Count,
                elapsedSeconds = result.TotalDuration.TotalSeconds,
                checkpointPath = result.CheckpointFilePath
            });
        }

        private static DceProToolResult ExportBoq(JsonElement? args)
        {
            string outPath = JsonArgHelper.GetString(args, "outputBoqPath", string.Empty);
            if (string.IsNullOrWhiteSpace(outPath))
            {
                outPath = Path.Combine(Path.GetTempPath(), $"HPDcePro_BOQ_{Guid.NewGuid():N}.xlsx");
            }

            string project = JsonArgHelper.GetString(args, "projectName", "Dự án mẫu HPDcePro");
            var boq = CreateSampleTakeoff();
            ClosedXmlBoqExporter.ExportToFile(boq, outPath, project);

            var fileInfo = new FileInfo(outPath);
            return DceProToolResult.Success(new
            {
                outputFilePath = outPath,
                fileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                totalConcreteVolumeM3 = boq.TotalConcreteVolumeM3,
                totalFormworkAreaM2 = boq.TotalFormworkAreaM2,
                totalRebarWeightKg = boq.TotalRebarKg,
                sheetsExported = new[] { "TongHop", "BeTong", "VanKhuon", "CotThep", "ChiTietTheoTang" }
            });
        }

        private static QuantityTakeoffResult CreateSampleTakeoff()
        {
            var elements = new List<ElementTakeoffItem>
            {
                new ElementTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Column, Mark = "C1", ConcreteVolumeM3 = 12.5, FormworkAreaM2 = 96.0 },
                new ElementTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Beam, Mark = "D1", ConcreteVolumeM3 = 18.2, FormworkAreaM2 = 145.0 },
                new ElementTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Slab, Mark = "S1", ConcreteVolumeM3 = 24.0, FormworkAreaM2 = 200.0 }
            };

            var rebars = new List<RebarTakeoffItem>
            {
                new RebarTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Column, Mark = "C1", DiameterMm = 20, TotalLengthM = 350.0 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Beam, Mark = "D1", DiameterMm = 18, TotalLengthM = 480.0 },
                new RebarTakeoffItem { StoryName = "TẦNG 1", ElementType = TakeoffElementType.Slab, Mark = "S1", DiameterMm = 10, TotalLengthM = 820.0 }
            };

            return QuantityTakeoffCalculator.CalculateFromElements(elements, rebars);
        }
    }
}
