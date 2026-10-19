namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Text;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 2: Boundary Value Analysis & Corner Cases - Part 1:
/// ScriptGuard Line Limits, AST Directives, Clean Code Rules, and Security Opt-In Boundaries.
/// </summary>
public sealed class Tier2_ScriptGuardAndSecurityBoundariesTests
{
    [Fact]
    public void TC_T2_AST_01_LineLimit_Exactly300Lines_PassesValidation()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 300; i++) sb.AppendLine($"int var_{i} = {i};");
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(sb.ToString());
        Assert.True(quality.IsValid);
        Assert.Equal(300, quality.LineCount);
    }

    [Fact]
    public void TC_T2_AST_02_LineLimit_301Lines_FailsValidation()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 301; i++) sb.AppendLine($"int var_{i} = {i};");
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(sb.ToString());
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("300 lines"));
    }

    [Fact]
    public void TC_T2_AST_03_EmptyCatch_WithoutComment_FailsValidation()
    {
        string script = "try { int x = 1; } catch (Exception)" + " { }";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("Empty catch block"));
    }

    [Fact]
    public void TC_T2_AST_04_EmptyCatch_WithExplanatoryComment_PassesValidation()
    {
        string script = "try { int x = 1; } catch { /* Ignore expected missing port */ }";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.True(quality.IsValid);
    }

    [Fact]
    public void TC_T2_AST_05_CommentedCode_VariableDeclaration_FailsValidation()
    {
        string script = "// var elem = doc.Create.NewWall();";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("Commented-out code"));
    }

    [Fact]
    public void TC_T2_AST_06_Directives_HashR_IsStrictlyDenied()
    {
        string script = "#r \"RevitAPI.dll\"\nint x = 1;";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("#r"));
    }

    [Fact]
    public void TC_T2_AST_07_Directives_HashLoad_IsStrictlyDenied()
    {
        string script = "#load \"helper.csx\"\nint x = 1;";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("#load"));
    }

    [Fact]
    public void TC_T2_AST_08_NamespaceDenial_SystemNet_IsBlocked()
    {
        string script = "System.Net.WebClient wc = null;";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Net"));
    }

    [Fact]
    public void TC_T2_AST_09_NamespaceDenial_SystemReflection_IsBlocked()
    {
        string script = "var asm = System.Reflection.Assembly.GetExecutingAssembly();";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.Reflection"));
    }

    [Fact]
    public void TC_T2_AST_10_NamespaceDenial_GlobalPrefixBypass_IsBlocked()
    {
        string script = "global::System.IO.File.WriteAllText(\"a.txt\", \"b\");";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));
    }

    [Fact]
    public async Task TC_T2_OPT_01_OptInDisabled_ExecuteRequest_ReturnsExactCodeMinus32001()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        var res = await bridge.ExecuteAsync(new DynamoExecutionRequest("int a = 0;"));
        Assert.Equal(-32001, res.ErrorCode);
    }

    [Fact]
    public async Task TC_T2_OPT_02_OptInDisabled_ErrorMessage_ExplicitlyContainsPrompt()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        var res = await bridge.ExecuteAsync(new DynamoExecutionRequest("int a = 0;"));
        Assert.Contains("Allow AI Dynamo execution", res.ErrorMessage);
    }

    [Fact]
    public async Task TC_T2_OPT_03_OptInRapidToggle_FalseTrueFalse_EnforcesLatestDisabledState()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.IsExecutionEnabled = false;
        bridge.IsExecutionEnabled = true;
        bridge.IsExecutionEnabled = false;

        var res = await bridge.ExecuteAsync(new DynamoExecutionRequest("int a = 0;"));
        Assert.False(res.Success);
        Assert.Equal(-32001, res.ErrorCode);
    }

    [Fact]
    public void TC_T2_OPT_04_OptInDisabled_GetContext_IsPermitted()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        var ctx = bridge.GetContext();
        Assert.NotNull(ctx);
        Assert.False(ctx.IsExecutionEnabled);
    }

    [Fact]
    public async Task TC_T2_OPT_05_OptInDisabled_MultipleSequentialRequests_AreAllRejectedConsistently()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = false };
        for (int i = 0; i < 5; i++)
        {
            var res = await bridge.ExecuteAsync(new DynamoExecutionRequest($"int iter = {i};"));
            Assert.False(res.Success);
            Assert.Equal(-32001, res.ErrorCode);
        }
    }

    [Fact]
    public async Task TC_T2_OPT_06_OptInEnabled_DryRun_PermittedWithoutRejection()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var res = await bridge.ExecuteAsync(new DynamoExecutionRequest("int a = 1;", DryRun: true));
        Assert.True(res.Success);
        Assert.True(res.RolledBack);
    }

    [Fact]
    public void TC_T2_AST_11_WhitespaceAndBlankLines_PreserveAccurateLineCount()
    {
        string script = "int a = 1;\n\nint b = 2;\n";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        // Trims leading/trailing whitespace per ScriptQuality rule; internal blank line counts (3 lines total)
        Assert.Equal(3, quality.LineCount);
    }

    [Fact]
    public void TC_T2_AST_12_ValidComments_WithoutCode_DoNotTriggerCommentedCodeError()
    {
        string script = "// This is an explanatory comment describing the algorithm\nint a = 1;";
        var bridge = new DynamoE2EFakeBridge();
        var quality = bridge.ValidateScript(script);
        Assert.True(quality.IsValid);
    }
}
