using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Takeoff;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Consolidated outcome of the 24-step AutoStr engineering workflow.
    /// </summary>
    public class AutoStrPipelineResult
    {
        public bool IsSuccess { get; set; }
        public int CompletedStepsCount { get; set; }
        public List<AutoStrStepResult> StepResults { get; set; } = new List<AutoStrStepResult>();
        public TimeSpan TotalDuration { get; set; }
        public List<AutoStrLogEntry> Logs { get; set; } = new List<AutoStrLogEntry>();
        public QuantityTakeoffResult? TakeoffResult { get; set; }
        public int ResizingIterationsCount { get; set; }
        public string? CheckpointFilePath { get; set; }
        public AutoStrStepContext? Context { get; set; }
    }
}
