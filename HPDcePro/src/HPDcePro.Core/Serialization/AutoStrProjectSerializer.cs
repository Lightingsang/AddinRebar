using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HPDcePro.Core.Serialization
{
    /// <summary>
    /// Resilient serializer and deserializer for AutoStr (.autostr) JSON project files.
    /// Handles leading UTF-8 Byte Order Marks (BOM: 0xEF, 0xBB, 0xBF) seamlessly.
    /// </summary>
    public static class AutoStrProjectSerializer
    {
        private static readonly JsonSerializerOptions s_options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static AutoStrConfig DeserializeFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"AutoStr file not found: {filePath}", filePath);
            }

            byte[] rawBytes = File.ReadAllBytes(filePath);
            return DeserializeFromBytes(rawBytes);
        }

        public static AutoStrConfig DeserializeFromBytes(byte[] rawBytes)
        {
            if (rawBytes == null || rawBytes.Length == 0)
            {
                throw new ArgumentException("Byte array is null or empty.", nameof(rawBytes));
            }

            int offset = 0;
            // Detect and trim 3-byte UTF-8 BOM: 0xEF, 0xBB, 0xBF
            if (rawBytes.Length >= 3 && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF)
            {
                offset = 3;
            }

            string json = Encoding.UTF8.GetString(rawBytes, offset, rawBytes.Length - offset);
            return DeserializeFromString(json);
        }

        public static AutoStrConfig DeserializeFromString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            // Also check for leading BOM character '\uFEFF' in decoded string
            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                json = json.Substring(1);
            }

            var config = JsonSerializer.Deserialize<AutoStrConfig>(json, s_options);
            if (config == null)
            {
                throw new InvalidDataException("Deserialized AutoStrConfig returned null.");
            }
            return config;
        }

        public static string SerializeToString(AutoStrConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            return JsonSerializer.Serialize(config, s_options);
        }

        public static void SerializeToFile(AutoStrConfig config, string filePath, bool writeBom = true)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            string json = SerializeToString(config);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (writeBom)
                {
                    byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
                    stream.Write(bom, 0, bom.Length);
                }
                stream.Write(jsonBytes, 0, jsonBytes.Length);
            }
        }
    }
}
