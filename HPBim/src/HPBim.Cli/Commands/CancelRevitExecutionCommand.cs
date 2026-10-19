namespace HPBim.Cli.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;

public sealed class CancelRevitExecutionCommand
{
    private readonly ConsoleFormatter _formatter;

    public CancelRevitExecutionCommand(ConsoleFormatter? formatter = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
    }

    public async Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        string pipeName = args.GetOption("pipe", PipeNaming.For(PipeNaming.RevitHost, 2026));
        _formatter.WriteBanner("Cancel Revit Execution", "Hủy bỏ script đang chạy dở dang trong Revit");

        var client = new RevitPipeClient(new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 5000,
            DefaultExecuteTimeoutSeconds = 10
        });

        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            bool ok = await client.CancelAsync(ct).ConfigureAwait(false);
            if (ok)
            {
                _formatter.WriteSuccess("Đã phát lệnh revit.cancel thành công!");
                return 0;
            }
            else
            {
                _formatter.WriteError("Lệnh cancel không thành công.");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _formatter.WriteError($"Lỗi gửi lệnh cancel: {ex.Message}");
            return 1;
        }
        finally
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
