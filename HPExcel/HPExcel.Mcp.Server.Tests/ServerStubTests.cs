using Xunit;

namespace HPExcel.Mcp.Server.Tests;

public class ServerStubTests
{
    [Fact]
    public void ServerProject_CompilesAndLoads()
    {
        Assert.NotNull(typeof(HPExcel.Mcp.Server.Program));
    }
}
