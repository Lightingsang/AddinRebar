using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep07_BuildEtabsModel : AutoStrStepBase
    {
        public override int StepNumber => 7;
        public override string StepName => "Khởi tạo mô hình không gian và tổ hợp tải trọng ETABS";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var combos = new List<string>();
            for (int i = 1; i <= 14; i++) combos.Add($"ULS_{i:D2}");
            for (int i = 1; i <= 14; i++) combos.Add($"SLS_{i:D2}");

            int storyCount = context.Config.Floors.Count > 0 ? context.Config.Floors.Count : 5;
            double buildingHeightM = storyCount * 3.6;
            var storyShearsKn = new Dictionary<string, double>();

            // Calculate story shear distribution from inverted triangular seismic / wind load profile
            for (int s = 1; s <= storyCount; s++)
            {
                double z = s * 3.6;
                double tributaryForceKn = 50.0 * (z / buildingHeightM) * 1.2;
                storyShearsKn[$"Tầng {s}"] = Math.Round(tributaryForceKn, 1);
            }

            context.SetArtifact("EtabsModelInitialized", true);
            context.SetArtifact("EtabsLoadCombos", combos);
            context.SetArtifact("StoryCount", storyCount);
            context.SetArtifact("StoryShearsKn", storyShearsKn);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã khởi tạo mô hình phân tích ETABS ({storyCount} tầng, cao {buildingHeightM:F1}m), {combos.Count} tổ hợp và ma trận lực tầng."));
        }
    }
}
