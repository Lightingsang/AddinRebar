using System;
using System.Collections.Generic;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Result payload produced by executing an individual AutoStr step.
    /// </summary>
    public class AutoStrStepResult
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsSkipped { get; set; }
        public TimeSpan Duration { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> OutputFiles { get; set; } = new List<string>();
        public Dictionary<string, object> Artifacts { get; set; } = new Dictionary<string, object>();

        public static AutoStrStepResult Success(
            int stepNumber,
            string stepName,
            string message,
            TimeSpan duration = default,
            List<string>? outputFiles = null,
            Dictionary<string, object>? artifacts = null)
        {
            return new AutoStrStepResult
            {
                StepNumber = stepNumber,
                StepName = stepName,
                IsSuccess = true,
                IsSkipped = false,
                Message = message,
                Duration = duration,
                OutputFiles = outputFiles ?? new List<string>(),
                Artifacts = artifacts ?? new Dictionary<string, object>()
            };
        }

        public static AutoStrStepResult Failed(
            int stepNumber,
            string stepName,
            string errorMessage,
            TimeSpan duration = default)
        {
            return new AutoStrStepResult
            {
                StepNumber = stepNumber,
                StepName = stepName,
                IsSuccess = false,
                IsSkipped = false,
                Message = errorMessage,
                Duration = duration
            };
        }

        public static AutoStrStepResult Skipped(
            int stepNumber,
            string stepName,
            string reason)
        {
            return new AutoStrStepResult
            {
                StepNumber = stepNumber,
                StepName = stepName,
                IsSuccess = true,
                IsSkipped = true,
                Message = reason,
                Duration = TimeSpan.Zero
            };
        }
    }
}
