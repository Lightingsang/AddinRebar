using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Serialization;

namespace HPDcePro.Core.AutoStr
{
    /// <summary>
    /// Shared blackboard context carrying configuration, project artifacts, topology, and execution state.
    /// </summary>
    public class AutoStrStepContext
    {
        public AutoStrConfig Config { get; set; }
        public string ProjectRootPath { get; set; }
        public FramingTopology Topology { get; set; }
        public Dictionary<string, object> StepArtifacts { get; set; } = new Dictionary<string, object>();
        public List<AutoStrLogEntry> Logs { get; set; } = new List<AutoStrLogEntry>();
        public Action<int, string, double>? ReportProgress { get; set; }
        public int CurrentIteration { get; set; } = 0;

        public AutoStrStepContext(
            AutoStrConfig config,
            string projectRootPath,
            FramingTopology? topology = null,
            Action<int, string, double>? reportProgress = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ProjectRootPath = projectRootPath ?? string.Empty;
            Topology = topology ?? new FramingTopology();
            ReportProgress = reportProgress;
        }

        public void Log(int stepNumber, string level, string message)
        {
            var entry = new AutoStrLogEntry
            {
                StepNumber = stepNumber,
                Level = level,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            Logs.Add(entry);
        }

        public void LogInfo(int stepNumber, string message) => Log(stepNumber, "Info", message);
        public void LogWarning(int stepNumber, string message) => Log(stepNumber, "Warning", message);
        public void LogError(int stepNumber, string message) => Log(stepNumber, "Error", message);

        public void SetArtifact(string key, object value)
        {
            StepArtifacts[key] = value;
        }

        public T? GetArtifact<T>(string key) where T : class
        {
            if (StepArtifacts.TryGetValue(key, out var val) && val is T typed)
            {
                return typed;
            }
            return null;
        }
    }
}
