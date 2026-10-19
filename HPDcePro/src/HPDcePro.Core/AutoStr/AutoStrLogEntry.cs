using System;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Log entry recorded during AutoStr pipeline execution.
    /// </summary>
    public class AutoStrLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Info";
        public int StepNumber { get; set; }
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] [Step {StepNumber:D2}] {Message}";
        }
    }
}
