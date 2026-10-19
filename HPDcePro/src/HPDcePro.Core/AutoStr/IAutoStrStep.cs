using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Contract for an automated structural engineering step in the 24-step AutoStr pipeline.
    /// </summary>
    public interface IAutoStrStep
    {
        int StepNumber { get; }
        string StepName { get; }
        Task<AutoStrStepResult> ExecuteAsync(AutoStrStepContext context, CancellationToken cancellationToken);
    }
}
