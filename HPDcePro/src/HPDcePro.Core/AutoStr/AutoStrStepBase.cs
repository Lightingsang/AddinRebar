using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Abstract base class providing common progress, cancellation, error handling, and enablement checks.
    /// </summary>
    public abstract class AutoStrStepBase : IAutoStrStep
    {
        public abstract int StepNumber { get; }
        public abstract string StepName { get; }

        public async Task<AutoStrStepResult> ExecuteAsync(AutoStrStepContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsStepEnabled(context))
            {
                context.LogInfo(StepNumber, $"Bước {StepNumber:D2} '{StepName}' bị tắt trong cấu hình. Bỏ qua.");
                return AutoStrStepResult.Skipped(StepNumber, StepName, "Step disabled by configuration.");
            }

            var sw = Stopwatch.StartNew();
            context.LogInfo(StepNumber, $"Bắt đầu thực hiện Bước {StepNumber:D2}: {StepName}...");
            context.ReportProgress?.Invoke(StepNumber, StepName, (StepNumber - 1) * 100.0 / 24.0);

            try
            {
                var result = await ExecuteStepCoreAsync(context, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                result.Duration = sw.Elapsed;
                context.LogInfo(StepNumber, $"Hoàn thành Bước {StepNumber:D2} trong {sw.ElapsedMilliseconds}ms: {result.Message}");
                context.ReportProgress?.Invoke(StepNumber, StepName, StepNumber * 100.0 / 24.0);
                return result;
            }
            catch (OperationCanceledException)
            {
                // Reason: Re-throw cancellation to notify pipeline coordinator immediately
                sw.Stop();
                context.LogWarning(StepNumber, $"Bước {StepNumber:D2} bị hủy bởi người dùng.");
                throw;
            }
            catch (Exception ex)
            {
                // Reason: Trap step-level failure to return structured AutoStrStepResult.Failed without crashing pipeline
                sw.Stop();
                context.LogError(StepNumber, $"Lỗi thực thi Bước {StepNumber:D2}: {ex.Message}");
                return AutoStrStepResult.Failed(StepNumber, StepName, ex.Message, sw.Elapsed);
            }
        }

        protected abstract Task<AutoStrStepResult> ExecuteStepCoreAsync(AutoStrStepContext context, CancellationToken cancellationToken);

        protected virtual bool IsStepEnabled(AutoStrStepContext context)
        {
            if (context.Config.StepEnabled != null &&
                context.Config.StepEnabled.Count >= StepNumber &&
                !context.Config.StepEnabled[StepNumber - 1])
            {
                return false;
            }
            return true;
        }
    }
}
