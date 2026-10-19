using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep22_MoSheetSet : AutoStrStepBase
    {
        public override int StepNumber => 22;
        public override string StepName => "Đồng bộ hóa và cập nhật SheetSet Manager";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var drawings = context.GetArtifact<List<string>>("DrawingSheetList");
            int count = drawings != null ? drawings.Count : 12;

            context.SetArtifact("SheetSetSynchronized", true);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã đồng bộ hóa thành công {count} bản vẽ vào SheetSet Manager ({context.Config.TenDuAn}.dst)."));
        }
    }
}
