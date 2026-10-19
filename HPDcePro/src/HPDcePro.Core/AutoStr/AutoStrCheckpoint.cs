using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HPDcePro.Core.Serialization;

namespace HPDcePro.Core.AutoStr
{
    public class AutoStrStepCheckpointRecord
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public bool IsSkipped { get; set; }
        public double DurationSeconds { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> OutputFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// Checkpoint state serialized to disk to allow resuming long-running AutoStr pipeline jobs.
    /// </summary>
    public class AutoStrCheckpoint
    {
        public string ProjectName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int LastCompletedStepNumber { get; set; }
        public List<AutoStrStepCheckpointRecord> CompletedSteps { get; set; } = new List<AutoStrStepCheckpointRecord>();
        public Dictionary<string, string> SerializedArtifacts { get; set; } = new Dictionary<string, string>();
        public AutoStrConfig Config { get; set; } = new AutoStrConfig();

        public static void SaveToFile(AutoStrCheckpoint checkpoint, string filePath)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(checkpoint, options);
            File.WriteAllText(filePath, json);
        }

        public static AutoStrCheckpoint LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Checkpoint file not found.", filePath);

            string json = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<AutoStrCheckpoint>(json);
            return result ?? throw new InvalidDataException("Failed to deserialize checkpoint JSON.");
        }
    }
}
