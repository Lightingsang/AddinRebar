namespace HPDynamo.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;

/// <summary>
/// High-fidelity simulator of the HPDynamo MCP Bridge running inside Revit.exe.
/// Enforces dual-lifecycle routing, Revit TransactionGroup rollback, Dynamo UndoRecorder,
/// Roslyn ScriptGuard deny-lists, and the mandatory AI execution opt-in gate.
/// </summary>
public sealed class DynamoE2EFakeBridge
{
    private readonly object _syncLock = new();
    private readonly List<long> _committedRevitElements = new();
    private readonly Stack<List<long>> _transactionStack = new();

    public bool IsExecutionEnabled { get; set; } = false;
    public bool IsLiveCanvasAttached { get; private set; } = false;
    public DynamoGraphSpec ActiveCanvasWorkspace { get; private set; }

    public DynamoE2EFakeBridge()
    {
        ActiveCanvasWorkspace = new DynamoGraphSpec(
            Guid.NewGuid(), "Home", "Active Canvas", new List<DynamoNodeSpec>(), new List<DynamoWireSpec>());
    }

    public void AttachLiveCanvas(string workspaceName = "Active Canvas")
    {
        lock (_syncLock)
        {
            IsLiveCanvasAttached = true;
            ActiveCanvasWorkspace = new DynamoGraphSpec(
                Guid.NewGuid(), workspaceName, "Live Workspace", new List<DynamoNodeSpec>(), new List<DynamoWireSpec>());
        }
    }

    public void DetachLiveCanvas()
    {
        lock (_syncLock)
        {
            IsLiveCanvasAttached = false;
        }
    }

    public DynamoInfoRecord GetContext()
    {
        lock (_syncLock)
        {
            return new DynamoInfoRecord(
                IsConnected: true,
                ActiveWorkspaceName: ActiveCanvasWorkspace.Name,
                ActiveWorkspacePath: null,
                NodesCount: ActiveCanvasWorkspace.Nodes.Count,
                WiresCount: ActiveCanvasWorkspace.Wires.Count,
                DynamoVersion: "3.7.0.11777",
                RevitVersion: "2026.0",
                HasUnsavedChanges: ActiveCanvasWorkspace.HasUnsavedChanges,
                IsExecutionEnabled: IsExecutionEnabled,
                RunType: "Manual",
                IsLiveCanvasAttached: IsLiveCanvasAttached);
        }
    }

    public DynamoScriptQualityResult ValidateScript(string code)
    {
        var trimmed = code.Trim();
        int lineCount = trimmed.Length == 0 ? 0 : trimmed.Split('\n').Length;
        var errors = new List<string>();
        var warnings = new List<string>();

        if (lineCount > 300)
        {
            errors.Add($"Script exceeds 300 lines (got {lineCount})");
        }

        foreach (var ns in DynamoE2EContracts.DeniedNamespaces)
        {
            if (code.Contains(ns) || code.Contains($"global::{ns}"))
            {
                errors.Add($"Forbidden namespace detected: {ns}");
            }
        }

        if (code.Contains("#r ") || code.Contains("#load "))
        {
            errors.Add("Directives #r and #load are strictly forbidden");
        }

        if (Regex.IsMatch(code, @"catch\s*(\([^\)]*\))?\s*\{\s*\}"))
        {
            errors.Add("Empty catch block without reason comment");
        }

        if (Regex.IsMatch(code, @"//\s*(var|int|string|doc\.Create|Rebar)\s+"))
        {
            errors.Add("Commented-out code detected");
        }

        return new DynamoScriptQualityResult(errors.Count == 0, lineCount, errors, warnings);
    }

    public async Task<DynamoExecutionResult> ExecuteAsync(
        DynamoExecutionRequest request, CancellationToken ct = default)
    {
        // 1. Mandatory Security Gate Check
        if (!IsExecutionEnabled)
        {
            return new DynamoExecutionResult(
                Success: false,
                OutputJson: null,
                RolledBack: false,
                CreatedElementIds: Array.Empty<long>(),
                Logs: new[] { "Execution rejected by security gate." },
                DurationMs: 0,
                ErrorMessage: "Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit.",
                ErrorCode: DynamoE2EContracts.OptInDisabledErrorCode);
        }

        // 2. Roslyn ScriptGuard Validation
        var quality = ValidateScript(request.Code);
        if (!quality.IsValid)
        {
            return new DynamoExecutionResult(
                Success: false,
                OutputJson: null,
                RolledBack: false,
                CreatedElementIds: Array.Empty<long>(),
                Logs: quality.Errors,
                DurationMs: 5,
                ErrorMessage: string.Join("; ", quality.Errors),
                ErrorCode: -32002);
        }

        // 3. Execution Simulation with Dual Rollback
        lock (_syncLock)
        {
            var createdDuringRun = new List<long>();
            _transactionStack.Push(createdDuringRun);

            try
            {
                if (ct.IsCancellationRequested)
                {
                    _transactionStack.Pop();
                    return new DynamoExecutionResult(
                        Success: false,
                        OutputJson: null,
                        RolledBack: true,
                        CreatedElementIds: Array.Empty<long>(),
                        Logs: new[] { "Execution cancelled by token." },
                        DurationMs: 10,
                        ErrorMessage: "Execution cancelled.",
                        ErrorCode: -32003);
                }

                // Simulate element creation
                if (request.Code.Contains("CreateRebar") || request.Code.Contains("CreateElement"))
                {
                    long newId = 100000 + _committedRevitElements.Count + createdDuringRun.Count + 1;
                    createdDuringRun.Add(newId);
                }

                if (request.DryRun)
                {
                    // Rollback Revit TransactionGroup and canvas snapshot
                    _transactionStack.Pop();
                    return new DynamoExecutionResult(
                        Success: true,
                        OutputJson: "{\"status\":\"simulated_ok\"}",
                        RolledBack: true,
                        CreatedElementIds: createdDuringRun,
                        Logs: new[] { "TransactionGroup rolled back cleanly." },
                        DurationMs: 25);
                }

                // Commit transaction
                _transactionStack.Pop();
                _committedRevitElements.AddRange(createdDuringRun);
                return new DynamoExecutionResult(
                    Success: true,
                    OutputJson: "{\"status\":\"committed\"}",
                    RolledBack: false,
                    CreatedElementIds: createdDuringRun,
                    Logs: new[] { "Transaction committed successfully." },
                    DurationMs: 30);
            }
            catch (Exception ex)
            {
                // Ensure rollback on exception
                if (_transactionStack.Count > 0) _transactionStack.Pop();
                return new DynamoExecutionResult(
                    Success: false,
                    OutputJson: null,
                    RolledBack: true,
                    CreatedElementIds: Array.Empty<long>(),
                    Logs: new[] { ex.ToString() },
                    DurationMs: 15,
                    ErrorMessage: ex.Message,
                    ErrorCode: -32000);
            }
        }
    }

    public int GetCommittedElementCount()
    {
        lock (_syncLock) return _committedRevitElements.Count;
    }
}
