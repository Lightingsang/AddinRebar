using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace HPKata.Installer.Services
{
    /// <summary>
    /// Pure C# JSON-RPC 2.0 stdio client for HPAutoCad.Mcp.Server.exe.
    /// </summary>
    public sealed class AutocadMcpStdioClient : IDisposable
    {
        private readonly Process _process;
        private int _nextId = 1;
        private bool _disposed;

        public AutocadMcpStdioClient(string serverExePath)
        {
            if (!File.Exists(serverExePath))
                throw new FileNotFoundException("HPAutoCad MCP Server executable not found.", serverExePath);

            var psi = new ProcessStartInfo
            {
                FileName = serverExePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start HPAutoCad.Mcp.Server.exe.");
            _process.BeginErrorReadLine();
            InitializeHandshake();
        }

        private void InitializeHandshake()
        {
            SendRpc("initialize", new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "HPKata.CSharpMcpClient", version = "1.0.0" }
            });
            SendNotification("notifications/initialized", new { });
        }

        public string CallToolRaw(string toolName, object arguments)
        {
            using JsonDocument doc = SendRpc("tools/call", new { name = toolName, arguments });
            if (doc.RootElement.TryGetProperty("result", out JsonElement res) &&
                res.TryGetProperty("content", out JsonElement content) &&
                content.ValueKind == JsonValueKind.Array &&
                content.GetArrayLength() > 0 &&
                content[0].TryGetProperty("text", out JsonElement textEl))
            {
                return textEl.GetString() ?? doc.RootElement.GetRawText();
            }

            return doc.RootElement.GetRawText();
        }

        private JsonDocument SendRpc(string method, object parameters)
        {
            int id = _nextId++;
            string payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id,
                method,
                @params = parameters
            });

            _process.StandardInput.WriteLine(payload);
            _process.StandardInput.Flush();

            while (!_process.StandardOutput.EndOfStream)
            {
                string? line = _process.StandardOutput.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    JsonDocument doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("id", out JsonElement idEl) &&
                        idEl.ValueKind == JsonValueKind.Number &&
                        idEl.GetInt32() == id)
                    {
                        return doc;
                    }
                    doc.Dispose();
                }
                catch (JsonException)
                {
                    // Skip non-JSON diagnostic lines if any
                }
            }

            throw new IOException($"MCP Server closed stdout before replying to '{method}'.");
        }

        private void SendNotification(string method, object parameters)
        {
            string payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method,
                @params = parameters
            });
            _process.StandardInput.WriteLine(payload);
            _process.StandardInput.Flush();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _process.StandardInput.Close();
                if (!_process.WaitForExit(3000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore cleanup exceptions
            }
            finally
            {
                _process.Dispose();
            }
        }
    }
}
