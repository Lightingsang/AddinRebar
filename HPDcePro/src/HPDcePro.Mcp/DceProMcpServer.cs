using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Mcp.Catalog;
using HPDcePro.Mcp.Execution;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp
{
    public sealed class DceProMcpServer
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly DceProMcpToolCatalog _catalog;
        private readonly DceProMcpToolExecutor _executor;

        public DceProMcpServer(DceProMcpToolCatalog? catalog = null, DceProMcpToolExecutor? executor = null)
        {
            _catalog = catalog ?? new DceProMcpToolCatalog();
            _executor = executor ?? new DceProMcpToolExecutor();
        }

        public async Task RunStdioAsync(TextReader input, TextWriter output, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                string? responseJson = await ProcessMessageAsync(line, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(responseJson))
                {
                    await output.WriteLineAsync(responseJson).ConfigureAwait(false);
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<string?> ProcessMessageAsync(string jsonMessage, CancellationToken cancellationToken = default)
        {
            JsonRpcRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<JsonRpcRequest>(jsonMessage, s_jsonOptions);
            }
            catch (Exception ex)
            {
                // Reason: Return JSON-RPC parse error (-32700) when incoming payload is malformed
                var parseError = new JsonRpcResponse
                {
                    Error = new JsonRpcError(-32700, $"Parse error: {ex.Message}")
                };
                return JsonSerializer.Serialize(parseError, s_jsonOptions);
            }

            if (request == null) return null;
            var response = await ProcessRequestAsync(request, cancellationToken).ConfigureAwait(false);
            return response != null ? JsonSerializer.Serialize(response, s_jsonOptions) : null;
        }

        public async Task<JsonRpcResponse?> ProcessRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
        {
            if (string.Equals(request.Method, "notifications/initialized", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "ping" => new JsonRpcResponse { Id = request.Id, Result = new { } },
                "tools/list" => new JsonRpcResponse { Id = request.Id, Result = new { tools = _catalog.GetAllTools() } },
                "tools/call" => await HandleToolCallAsync(request, cancellationToken).ConfigureAwait(false),
                _ => new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError(-32601, $"Method '{request.Method}' not found.")
                }
            };
        }

        private static JsonRpcResponse HandleInitialize(JsonRpcRequest request)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { listChanged = false } },
                    serverInfo = new { name = "hpdcepro-mcp", version = "1.0.0" }
                }
            };
        }

        private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (!request.Params.HasValue)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError(-32602, "Invalid params: Missing parameters object.")
                };
            }

            var callParams = JsonSerializer.Deserialize<McpToolCallParams>(request.Params.Value.GetRawText(), s_jsonOptions);
            if (callParams == null || string.IsNullOrWhiteSpace(callParams.Name))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError(-32602, "Invalid params: Missing tool name.")
                };
            }

            var toolResult = await _executor.ExecuteAsync(callParams.Name, callParams.Arguments, cancellationToken).ConfigureAwait(false);
            object payload = toolResult.IsSuccess
                ? toolResult.Data ?? new { }
                : new { error = toolResult.ErrorMessage };

            string textContent = JsonSerializer.Serialize(payload, s_jsonOptions);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new
                {
                    content = new[] { new { type = "text", text = textContent } },
                    isError = !toolResult.IsSuccess
                }
            };
        }
    }
}
