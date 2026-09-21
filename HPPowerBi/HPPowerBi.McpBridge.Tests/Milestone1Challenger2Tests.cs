using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Safety;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.AnalysisServices.Tabular;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

/// <summary>
///     Empirical stress tests by challenger_m1_2:
///     1. PbiSafetyGuard: Adversarial DAX evasion, filter bypasses, mutation gating, and script reflection.
///     2. PbiSnapshotManager: Path sanitization, 60+ pruning threshold, directory failures, and corrupt restore.
///     3. PowerBiCloudClient: HTTP error status code resilience (401, 403, 404, 429, 500) and payload edge cases.
/// </summary>
public sealed class Milestone1Challenger2Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly PbiSnapshotManager _snapshotManager;

    public Milestone1Challenger2Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPPowerBi_Challenger2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _snapshotManager = new PbiSnapshotManager(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }

    // =========================================================================
    // Category 1: PbiSafetyGuard — Adversarial DAX & Evasion Testing
    // =========================================================================

    [Fact]
    public void ValidateDaxQuery_EmptyOrWhitespace_FailsCleanly()
    {
        Assert.False(PbiSafetyGuard.ValidateDaxQuery(null, out var err1));
        Assert.Equal("DAX query cannot be empty.", err1);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("", out var err2));
        Assert.Equal("DAX query cannot be empty.", err2);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("   \t\r\n  ", out var err3));
        Assert.Equal("DAX query cannot be empty.", err3);
    }

    [Fact]
    public void ValidateDaxQuery_RawXmla_Blocked()
    {
        var rawXmla = "<Batch xmlns=\"http://schemas.microsoft.com/analysisservices/2003/engine\"><ClearCache /></Batch>";
        Assert.False(PbiSafetyGuard.ValidateDaxQuery(rawXmla, out var error));
        Assert.NotNull(error);
        Assert.Contains("XMLA", error);
    }

    [Theory]
    [InlineData("/* harmless */ <Batch xmlns=\"...\"><ClearCache /></Batch>")]
    [InlineData("// comment\r\n<Batch><ClearCache /></Batch>")]
    [InlineData("-- sql comment\n<Batch><ClearCache /></Batch>")]
    [InlineData("\r\n  /* xml comment */\r\n  <Alter ObjectDefinition=\"...\"></Alter>")]
    public void ValidateDaxQuery_AdversarialCommentPrependedXmla_BlockedAfterHardening(string adversarialXmla)
    {
        // Verified: When an attacker prepends comments to an XMLA command,
        // StripLeadingComments removes them, and stripped.StartsWith("<") blocks the XMLA command.
        var passed = PbiSafetyGuard.ValidateDaxQuery(adversarialXmla, out var error);

        Assert.False(passed);
        Assert.NotNull(error);
        Assert.Contains("XMLA", error);
    }

    [Theory]
    [InlineData("{ \"createOrReplace\": { \"object\": { \"database\": \"Model\" } } }")]
    [InlineData("{ \"refresh\": { \"type\": \"full\", \"objects\": [ { \"database\": \"Model\" } ] } }")]
    [InlineData("/* comment */ { \"createOrReplace\": { \"object\": { \"database\": \"Model\" } } }")]
    [InlineData("-- sql comment\r\n{ \"refresh\": { \"type\": \"full\" } }")]
    public void ValidateDaxQuery_RawTmslJsonCommands_BlockedAfterHardening(string tmsl)
    {
        // Verified: Raw TMSL JSON commands (even with leading comments) are blocked by ValidateDaxQuery.
        var passed = PbiSafetyGuard.ValidateDaxQuery(tmsl, out var error);
        Assert.False(passed);
        Assert.NotNull(error);
        Assert.Contains("TMSL", error);
    }

    [Fact]
    public void ValidateDaxQuery_KillSpid_CommentInterspersedEvasion_PassesThrough()
    {
        // EMPIRICAL CHALLENGE: \bKILL\s+SPID\b requires whitespace between KILL and SPID.
        // If an attacker writes KILL/**/SPID 42, does it bypass the regex?
        var bypassed = PbiSafetyGuard.ValidateDaxQuery("KILL/**/SPID 42", out var error);
        Assert.True(bypassed, "SECURITY FINDING: Comment-interspersed KILL/**/SPID bypasses the regex \\bKILL\\s+SPID\\b.");
        Assert.Null(error);
    }

    [Theory]
    [InlineData("DISCOVER_CSDL_METADATA")]
    [InlineData("DISCOVER_XML_METADATA")]
    [InlineData("DISCOVER_SCHEMA_ROWSETS")]
    [InlineData("DISCOVER_INSTANCES")]
    [InlineData("DISCOVER_SESSIONS")]
    [InlineData("DISCOVER_CONNECTIONS")]
    [InlineData("SYSTEM$DISCOVER_SESSIONS")]
    public void ValidateDaxQuery_DiscoverSchemaCommands_PassesThrough(string discoverCmd)
    {
        // EMPIRICAL CHALLENGE: ValidateDaxQuery only explicitly checks \bDISCOVER_TRACE\b.
        // Other schema discovery commands pass through.
        var passed = PbiSafetyGuard.ValidateDaxQuery(discoverCmd, out var error);
        Assert.True(passed);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateDaxQuery_DiscoverTrace_BlockedCaseInsensitive()
    {
        Assert.False(PbiSafetyGuard.ValidateDaxQuery("discover_trace", out var err1));
        Assert.Contains("trace", err1, StringComparison.OrdinalIgnoreCase);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("  DISCOVER_TRACE  ", out var err2));
        Assert.Contains("trace", err2, StringComparison.OrdinalIgnoreCase);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("select * from $SYSTEM.DISCOVER_TRACE", out var err3));
        Assert.Contains("trace", err3, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDaxQuery_KillSpid_BlockedCaseInsensitive()
    {
        Assert.False(PbiSafetyGuard.ValidateDaxQuery("kill spid 42", out var err1));
        Assert.Contains("KILL", err1);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("KILL\t\tSPID 100", out var err2));
        Assert.Contains("KILL", err2);

        Assert.False(PbiSafetyGuard.ValidateDaxQuery("kIlL   sPiD   999", out var err3));
        Assert.Contains("KILL", err3);
    }

    [Theory]
    [InlineData("ALTER TABLE [Sales] DROP COLUMN [Revenue]")]
    [InlineData("DROP TABLE [Customers]")]
    [InlineData("CREATE TABLE [Hacked] (ID INT)")]
    [InlineData("DROP DATABASE [Model]")]
    [InlineData("/* benign */ ALTER DATABASE [Model]")]
    public void ValidateDaxQuery_DdlStatements_NotExplicitlyBlockedInDaxValidator(string ddl)
    {
        // EMPIRICAL CHALLENGE: ValidateDaxQuery does not contain DDL regex filters for ALTER/DROP/CREATE.
        // It relies on ADOMD.NET execution layer or TOM API constraints.
        var passed = PbiSafetyGuard.ValidateDaxQuery(ddl, out var error);
        Assert.True(passed, "Empirical observation: DDL statements are not rejected by ValidateDaxQuery regex.");
        Assert.Null(error);
    }

    [Fact]
    public void ValidateDaxQuery_ValidDaxQueries_PassCleanly()
    {
        Assert.True(PbiSafetyGuard.ValidateDaxQuery("EVALUATE Customers", out var err1));
        Assert.Null(err1);

        Assert.True(PbiSafetyGuard.ValidateDaxQuery("DEFINE MEASURE Sales[Rate] = DIVIDE(1, 2) EVALUATE Sales", out var err2));
        Assert.Null(err2);

        Assert.True(PbiSafetyGuard.ValidateDaxQuery("EVALUATE FILTER('Orders', 'Orders'[Amount] > 100 && 'Orders'[Amount] < 500)", out var err3));
        Assert.Null(err3);
    }

    // =========================================================================
    // Category 2: PbiSafetyGuard — Mutation Gating & C# Script Inspection
    // =========================================================================

    [Fact]
    public void SafetyGuard_InitialState_StrictlyDisallowsAll()
    {
        var guard = new PbiSafetyGuard();

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsMutationEnabled);

        var exExec = Assert.Throws<BridgeRequestException>(() => guard.EnsureExecutionAllowed());
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exExec.Code);
        Assert.Equal(PbiSafetyGuard.ExecutionDisabledMessage, exExec.Message);

        var exMut = Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exMut.Code);
        Assert.Equal(PbiSafetyGuard.ExecutionDisabledMessage, exMut.Message);
    }

    [Fact]
    public void SafetyGuard_SetMutationWithoutExecution_CoercedToFalse()
    {
        var guard = new PbiSafetyGuard();

        // Attempt to enable mutation while execution is disabled
        guard.IsMutationEnabled = true;

        Assert.False(guard.IsMutationEnabled, "Mutation must not be enabled when execution is disabled.");
        Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
    }

    [Fact]
    public void SafetyGuard_EnableExecutionThenMutation_GatingTransitionsCorrectly()
    {
        var guard = new PbiSafetyGuard();
        var stateChangeCount = 0;
        guard.StateChanged += () => stateChangeCount++;

        // Step 1: Enable execution
        guard.IsExecutionEnabled = true;
        Assert.Equal(1, stateChangeCount);
        guard.EnsureExecutionAllowed(); // Must not throw

        var exMut = Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
        Assert.Equal(PbiSafetyGuard.MutationDisabledMessage, exMut.Message);

        // Step 2: Enable mutation
        guard.IsMutationEnabled = true;
        Assert.Equal(2, stateChangeCount);
        guard.EnsureExecutionAllowed(); // Must not throw
        guard.EnsureMutationAllowed();  // Must not throw

        // Step 3: Turn off execution -> automatically turns off mutation
        guard.IsExecutionEnabled = false;
        Assert.False(guard.IsMutationEnabled);
        Assert.Throws<BridgeRequestException>(() => guard.EnsureExecutionAllowed());
        Assert.Throws<BridgeRequestException>(() => guard.EnsureMutationAllowed());
    }

    [Fact]
    public void IsMutationScript_DetectsStandardMutations()
    {
        Assert.True(PbiSafetyGuard.IsMutationScript("model.SaveChanges();"));
        Assert.True(PbiSafetyGuard.IsMutationScript("t.Measures.Add(new Measure());"));
        Assert.True(PbiSafetyGuard.IsMutationScript("t.Measures.Remove(m);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("m.Relationships.Add(rel);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("m.Relationships.Remove(rel);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("m.Tables.Add(tbl);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("m.Tables.Remove(tbl);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("tbl.Columns.Add(col);"));
        Assert.True(PbiSafetyGuard.IsMutationScript("tbl.Columns.Remove(col);"));

        Assert.False(PbiSafetyGuard.IsMutationScript("var count = model.Tables.Count; return count;"));
        Assert.False(PbiSafetyGuard.IsMutationScript(""));
        Assert.False(PbiSafetyGuard.IsMutationScript("   "));
    }

    [Theory]
    [InlineData("table.Measures.Clear();")]
    [InlineData("model.Relationships.Clear();")]
    [InlineData("measure.Expression = \"CALCULATE(SUM(Sales[Amount]))\";")]
    [InlineData("table.Columns.RemoveAt(0);")]
    public void IsMutationScript_AdversarialEvasionPatterns_EmpiricalBehavior(string script)
    {
        // EMPIRICAL CHALLENGE: Scripts that mutate collections via .Clear(), .RemoveAt(),
        // or direct property assignment (Expression = ...) are not matched by the regex.
        var detected = PbiSafetyGuard.IsMutationScript(script);

        // We record this evasion as an empirical finding:
        Assert.False(detected, $"EMPIRICAL FINDING: Pattern '{script}' is not detected by IsMutationScript.");
    }

    // =========================================================================
    // Category 3: PbiSnapshotManager — Pruning, Sanitization & Failure Modes
    // =========================================================================

    [Theory]
    [InlineData("Model<>:\"/\\|?*", "Model_________")]
    [InlineData("Sales\t\r\nModel", "Sales___Model")]
    [InlineData("   ", "snapshot")]
    [InlineData("", "snapshot")]
    [InlineData("../../traversal/model", ".._.._traversal_model")]
    [InlineData("MôHình_TiếngViệt_2026", "MôHình_TiếngViệt_2026")]
    [InlineData("Model_日本語_データ", "Model_日本語_データ")]
    [InlineData("CON", "CON")]
    public void SanitizeFileName_StressScenarios(string input, string expected)
    {
        var sanitized = PbiSnapshotManager.SanitizeFileName(input);
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    public void PruneOldSnapshots_Over60Snapshots_RetainsExactly50MostRecent()
    {
        Directory.CreateDirectory(_tempDir);
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Generate 65 synthetic snapshot files
        for (var i = 0; i < 65; i++)
        {
            var fileName = $"Snapshot_Model_{i:D3}.json";
            var filePath = Path.Combine(_tempDir, fileName);
            File.WriteAllText(filePath, "{}", Encoding.UTF8);

            // Stagger creation times so index 64 is the newest and index 0 is oldest
            File.SetCreationTimeUtc(filePath, baseTime.AddMinutes(i));
        }

        var filesBefore = Directory.GetFiles(_tempDir, "Snapshot_*.json");
        Assert.Equal(65, filesBefore.Length);

        // Execute prune with default threshold 50
        var pruned = _snapshotManager.PruneOldSnapshots(maxRetained: 50);

        Assert.Equal(15, pruned);

        var remainingFiles = Directory.GetFiles(_tempDir, "Snapshot_*.json");
        Assert.Equal(50, remainingFiles.Length);

        // Verify that the 15 oldest files (000 to 014) were deleted
        for (var i = 0; i < 15; i++)
        {
            var oldPath = Path.Combine(_tempDir, $"Snapshot_Model_{i:D3}.json");
            Assert.False(File.Exists(oldPath), $"Old snapshot {oldPath} should have been pruned.");
        }

        // Verify that the 50 newest files (015 to 064) were retained
        for (var i = 15; i < 65; i++)
        {
            var keptPath = Path.Combine(_tempDir, $"Snapshot_Model_{i:D3}.json");
            Assert.True(File.Exists(keptPath), $"New snapshot {keptPath} should have been retained.");
        }
    }

    [Fact]
    public void PruneOldSnapshots_NonExistentDirectory_ReturnsZero()
    {
        var nonExistentDir = Path.Combine(_tempDir, "SubDir_" + Guid.NewGuid().ToString("N"));
        var manager = new PbiSnapshotManager(nonExistentDir);

        var pruned = manager.PruneOldSnapshots(50);
        Assert.Equal(0, pruned);
    }

    [Fact]
    public void PruneOldSnapshots_BelowThreshold_PrunesNothing()
    {
        Directory.CreateDirectory(_tempDir);
        for (var i = 0; i < 10; i++)
        {
            var filePath = Path.Combine(_tempDir, $"Snapshot_Test_{i}.json");
            File.WriteAllText(filePath, "{}", Encoding.UTF8);
        }

        var pruned = _snapshotManager.PruneOldSnapshots(maxRetained: 50);
        Assert.Equal(0, pruned);
        Assert.Equal(10, Directory.GetFiles(_tempDir, "Snapshot_*.json").Length);
    }

    [Fact]
    public void PruneOldSnapshots_LockedFileDuringPruning_GracefullySkipsAndDeletesRemaining()
    {
        Directory.CreateDirectory(_tempDir);
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create 6 files to prune to 2 (should delete 4)
        var filePaths = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            var path = Path.Combine(_tempDir, $"Snapshot_LockTest_{i}.json");
            File.WriteAllText(path, "{}", Encoding.UTF8);
            File.SetCreationTimeUtc(path, baseTime.AddMinutes(i));
            filePaths.Add(path);
        }

        // Lock file 0 (one of the files to be deleted) exclusively
        using (var lockStream = new FileStream(filePaths[0], FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // Prune to retain 2
            var pruned = _snapshotManager.PruneOldSnapshots(maxRetained: 2);

            // File 0 could not be deleted due to lock, but files 1, 2, 3 should be deleted
            Assert.Equal(3, pruned);
            Assert.True(File.Exists(filePaths[0]), "Locked file should still exist.");
        }

        // After lock release, another prune should clean up file 0
        var secondPrune = _snapshotManager.PruneOldSnapshots(maxRetained: 2);
        Assert.Equal(1, secondPrune);
        Assert.False(File.Exists(filePaths[0]));
    }

    [Fact]
    public void RestoreSnapshot_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _snapshotManager.RestoreSnapshot("NonExistent_Snapshot_999999.json"));
    }

    [Fact]
    public void RestoreSnapshot_CorruptedJson_ThrowsJsonException()
    {
        var corruptFile = Path.Combine(_tempDir, "Snapshot_Corrupt.json");
        File.WriteAllText(corruptFile, "{ NOT_VALID_JSON_AT_ALL :::", Encoding.UTF8);

        Assert.ThrowsAny<Exception>(() => _snapshotManager.RestoreSnapshot(corruptFile));
    }

    [Fact]
    public void RestoreSnapshot_EmptyFile_ThrowsException()
    {
        var emptyFile = Path.Combine(_tempDir, "Snapshot_Empty.json");
        File.WriteAllText(emptyFile, "", Encoding.UTF8);

        Assert.ThrowsAny<Exception>(() => _snapshotManager.RestoreSnapshot(emptyFile));
    }

    [Fact]
    public void CreateSnapshot_NullDatabase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _snapshotManager.CreateSnapshot(null!));
    }

    [Fact]
    public void CreateSnapshot_DirectoryCreationFailure_ThrowsIOException()
    {
        // Point snapshot manager to a path where a file already exists with the same name as directory
        var blockedFile = Path.Combine(_tempDir, "BlockedAsFile.txt");
        File.WriteAllText(blockedFile, "block");

        var badManager = new PbiSnapshotManager(blockedFile);
        var db = new Database("TestDb");

        Assert.Throws<IOException>(() => badManager.CreateSnapshot(db));
    }

    // =========================================================================
    // Category 4: PowerBiCloudClient — HTTP Error Resilience
    // =========================================================================

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "NotFound")]
    [InlineData((HttpStatusCode)429, "TooManyRequests")]
    [InlineData(HttpStatusCode.InternalServerError, "InternalServerError")]
    public async Task ListWorkspacesAsync_HttpErrors_ThrowsHttpRequestException(HttpStatusCode statusCode, string expectedStatusText)
    {
        using var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent($"{{\"error\": \"{expectedStatusText}\"}}")
            });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_bearer_token");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ListWorkspacesAsync(ct: TestContext.Current.CancellationToken));

        Assert.Contains(statusCode.ToString(), ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "NotFound")]
    [InlineData((HttpStatusCode)429, "TooManyRequests")]
    [InlineData(HttpStatusCode.InternalServerError, "InternalServerError")]
    public async Task ListDatasetsAsync_HttpErrors_ThrowsHttpRequestException(HttpStatusCode statusCode, string expectedStatusText)
    {
        using var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent($"{{\"error\": \"{expectedStatusText}\"}}")
            });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_bearer_token");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ListDatasetsAsync("workspace-123", ct: TestContext.Current.CancellationToken));

        Assert.Contains(statusCode.ToString(), ex.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "NotFound")]
    [InlineData((HttpStatusCode)429, "429")]
    [InlineData(HttpStatusCode.InternalServerError, "InternalServerError")]
    public async Task TriggerRefreshAsync_HttpErrors_ReturnsFailureResultGracefully(HttpStatusCode statusCode, string statusSnippet)
    {
        var errorBody = $"{{\"error\": {{\"code\": \"{statusSnippet}\", \"message\": \"Failed\"}}}}";

        using var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorBody)
            });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_bearer_token");

        // Must NOT throw; must return a graceful CloudRefreshResultDto
        var result = await client.TriggerRefreshAsync("dataset-xyz", "workspace-abc", ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("dataset-xyz", result.DatasetId);
        Assert.Null(result.RequestId);
        Assert.Equal(statusCode.ToString(), result.Status);
        Assert.Equal(errorBody, result.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData((HttpStatusCode)429)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ExecuteDaxAsync_HttpErrors_ReturnsFailureResultGracefully(HttpStatusCode statusCode)
    {
        var errorBody = $"{{\"error\": \"API call failed with status {statusCode}\"}}";

        using var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorBody)
            });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_bearer_token");

        // Must NOT throw; must return a graceful CloudDaxResultDto
        var result = await client.ExecuteDaxAsync("dataset-xyz", "EVALUATE Customers", "workspace-abc", ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Null(result.Results);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(statusCode.ToString(), result.ErrorMessage);
    }

    [Fact]
    public async Task CloudClient_InvalidInputArguments_ThrowsArgumentException()
    {
        using var client = new PowerBiCloudClient();
        client.SetToken("token");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.TriggerRefreshAsync("", ct: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.ExecuteDaxAsync("", "EVALUATE 1", ct: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.ExecuteDaxAsync("dataset-id", "", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CloudClient_MissingMsalCredentials_ThrowsInvalidOperationException()
    {
        using var client = new PowerBiCloudClient(); // No TenantId, ClientId, ClientSecret

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AcquireTokenAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TenantId and ClientId must be configured", ex.Message);

        client.TenantId = "tenant";
        client.ClientId = "client";
        // Still missing ClientSecret
        var exSecret = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AcquireTokenAsync(TestContext.Current.CancellationToken));

        Assert.Contains("ClientSecret must be configured", exSecret.Message);
    }

    [Fact]
    public async Task ListWorkspacesAsync_MalformedJsonResponseBody_ThrowsJsonException()
    {
        using var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body>502 Bad Gateway</body></html>", Encoding.UTF8, "text/html")
            });

        using var httpClient = new HttpClient(handler);
        using var client = new PowerBiCloudClient(httpClient: httpClient);
        client.SetToken("mock_token");

        await Assert.ThrowsAnyAsync<JsonException>(() =>
            client.ListWorkspacesAsync(ct: TestContext.Current.CancellationToken));
    }
}
