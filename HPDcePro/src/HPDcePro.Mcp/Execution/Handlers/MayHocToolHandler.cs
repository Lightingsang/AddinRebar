using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.MachineLearning;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class MayHocToolHandler
    {
        public async Task<DceProToolResult> ExecuteAsync(JsonElement? args, CancellationToken ct = default)
        {
            string op = JsonArgHelper.GetString(args, "operation", "tokenize_text").ToLowerInvariant();
            return op switch
            {
                "tokenize_text" => TokenizeText(args),
                "query_database" => await QueryDatabaseAsync(args, ct).ConfigureAwait(false),
                "query_history" => await QueryHistoryAsync(args, ct).ConfigureAwait(false),
                "model_info" => await QueryModelInfoAsync(args, ct).ConfigureAwait(false),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_query_mayhoc_rebar.")
            };
        }

        private static DceProToolResult TokenizeText(JsonElement? args)
        {
            string rawText = JsonArgHelper.GetString(args, "text", "2d18 + 1d20 L=3500 a150");
            string normalized = RebarTextTokenizer.Normalize(rawText);
            var tokens = RebarTextTokenizer.Tokenize(rawText);

            return DceProToolResult.Success(new
            {
                originalText = rawText,
                normalizedText = normalized,
                tokenCount = tokens.Count,
                tokens = tokens
            });
        }

        private static string ResolveDbPath(JsonElement? args)
        {
            string customPath = JsonArgHelper.GetString(args, "dbPath", string.Empty);
            return string.IsNullOrWhiteSpace(customPath)
                ? MauHocDatabaseReader.DefaultDatabasePath
                : customPath;
        }

        private static async Task<DceProToolResult> QueryDatabaseAsync(JsonElement? args, CancellationToken ct)
        {
            string dbPath = ResolveDbPath(args);
            if (!File.Exists(dbPath))
            {
                return DceProToolResult.Fail($"Database file not found at '{dbPath}'. MauHoc features require local SQLite database.");
            }

            int limit = JsonArgHelper.GetInt(args, "limit", 20);
            int offset = JsonArgHelper.GetInt(args, "offset", 0);

            using var reader = new MauHocDatabaseReader(dbPath);
            var samples = await reader.GetSamplesAsync(limit, offset, ct).ConfigureAwait(false);
            int total = await reader.GetSampleCountAsync(ct).ConfigureAwait(false);

            return DceProToolResult.Success(new
            {
                dbPath = dbPath,
                totalSamples = total,
                returnedSamples = samples.Count,
                samples = samples
            });
        }

        private static async Task<DceProToolResult> QueryHistoryAsync(JsonElement? args, CancellationToken ct)
        {
            string dbPath = ResolveDbPath(args);
            if (!File.Exists(dbPath))
            {
                return DceProToolResult.Fail($"Database file not found at '{dbPath}'. MauHoc features require local SQLite database.");
            }

            using var reader = new MauHocDatabaseReader(dbPath);
            var history = await reader.GetLichSuSuaAsync(ct).ConfigureAwait(false);

            return DceProToolResult.Success(new
            {
                dbPath = dbPath,
                historyCount = history.Count,
                history = history
            });
        }

        private static async Task<DceProToolResult> QueryModelInfoAsync(JsonElement? args, CancellationToken ct)
        {
            string dbPath = ResolveDbPath(args);
            if (!File.Exists(dbPath))
            {
                return DceProToolResult.Fail($"Database file not found at '{dbPath}'. MauHoc features require local SQLite database.");
            }

            using var reader = new MauHocDatabaseReader(dbPath);
            int total = await reader.GetSampleCountAsync(ct).ConfigureAwait(false);
            var labels = await reader.GetDistinctLabelsAsync(ct).ConfigureAwait(false);

            return DceProToolResult.Success(new
            {
                dbPath = dbPath,
                totalSamples = total,
                distinctLabelsCount = labels.Count,
                labels = labels
            });
        }
    }
}
