using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

return await McpServerHost.RunAsync(args, ExcelHostProfile.Instance);

namespace HPExcel.Mcp.Server
{
    public class Program { }
}
