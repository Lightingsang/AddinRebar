using System;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Mcp
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                Console.WriteLine("HPDcePro Model Context Protocol (MCP) Server");
                Console.WriteLine("Stdio transport for structural design and detailing tools.");
                Console.WriteLine("Usage: HPDcePro.Mcp.exe");
                return 0;
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var server = new DceProMcpServer();
            try
            {
                await server.RunStdioAsync(Console.In, Console.Out, cts.Token).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException)
            {
                // Reason: Graceful shutdown on cancellation request
                return 0;
            }
            catch (Exception ex)
            {
                // Reason: Write unexpected fatal errors to stderr and return error exit code
                await Console.Error.WriteLineAsync($"Fatal MCP server error: {ex.Message}").ConfigureAwait(false);
                return 1;
            }
        }
    }
}
