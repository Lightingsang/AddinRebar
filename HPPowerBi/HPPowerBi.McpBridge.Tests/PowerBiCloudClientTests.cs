using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Cloud;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class PowerBiCloudClientTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }

    [Fact]
    public async Task ListWorkspacesAsync_ParsesWorkspaces()
    {
        var mockJson = @"{
            ""value"": [
                {
                    ""id"": ""ws-123"",
                    ""name"": ""Sales Analytics"",
                    ""isReadOnly"": false,
                    ""type"": ""Workspace""
                }
            ]
        }";

        using var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Contains("/groups", req.RequestUri!.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockJson)
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_token");

        var workspaces = await client.ListWorkspacesAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(workspaces);
        Assert.Equal("ws-123", workspaces[0].Id);
        Assert.Equal("Sales Analytics", workspaces[0].Name);
        Assert.False(workspaces[0].IsReadOnly);
    }

    [Fact]
    public async Task TriggerRefreshAsync_Success_ReturnsAccepted()
    {
        using var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Contains("/refreshes", req.RequestUri!.ToString());
            var res = new HttpResponseMessage(HttpStatusCode.Accepted);
            res.Headers.Add("requestId", "req-abc-999");
            return res;
        });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_token");

        var result = await client.TriggerRefreshAsync("dataset-456", "workspace-123", ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("Accepted", result.Status);
        Assert.Equal("req-abc-999", result.RequestId);
    }

    [Fact]
    public async Task ExecuteDaxAsync_Success_ReturnsResults()
    {
        var mockJson = @"{
            ""results"": [
                {
                    ""tables"": [
                        {
                            ""rows"": [
                                { ""[Total Sales]"": 9999.99 }
                            ]
                        }
                    ]
                }
            ]
        }";

        using var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Contains("/executeQueries", req.RequestUri!.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockJson)
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_token");

        var result = await client.ExecuteDaxAsync("dataset-789", "EVALUATE ROW(\"Total Sales\", 9999.99)", ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.Results);
    }
}
