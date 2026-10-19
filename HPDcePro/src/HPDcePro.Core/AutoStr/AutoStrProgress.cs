using System;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Event arguments for reporting real-time progress during pipeline execution.
    /// </summary>
    public class AutoStrProgress : EventArgs
    {
        public int StepNumber { get; }
        public string StepName { get; }
        public double Percent { get; }
        public string StatusMessage { get; }

        public AutoStrProgress(int stepNumber, string stepName, double percent, string statusMessage)
        {
            StepNumber = stepNumber;
            StepName = stepName;
            Percent = percent;
            StatusMessage = statusMessage;
        }
    }
}
