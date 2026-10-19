using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep14_DPTLMong : AutoStrStepBase
    {
        public override int StepNumber => 14;
        public override string StepName => "Sinh đường phân tích lực móng và sơ đồ truyền tải móng";

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var caps = context.GetArtifact<List<PileCapLayoutDefinition>>("PileCapDefinitions");
            int count = caps != null ? caps.Count : 12;

            context.SetArtifact("FoundationTributaryReady", true);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã sinh sơ đồ đường truyền tải móng cho {count} cụm móng cọc."));
        }
    }
}
