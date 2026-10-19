namespace HPBim.Client.Verification;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

public sealed class MeasuredGeometricVerifier : IMeasuredGeometricVerifier
{
    private readonly IRevitPipeClient _pipeClient;
    private readonly MeasuredGeometricEngine _engine;

    public MeasuredGeometricVerifier(IRevitPipeClient pipeClient, MeasuredGeometricEngine? engine = null)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
        _engine = engine ?? new MeasuredGeometricEngine();
    }

    public async Task<GeometricAuditReport> VerifyPlanGeometryAsync(
        BimExecutionPlan plan,
        CoordinationTolerances? tolerances = null,
        CancellationToken cancellationToken = default)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var request = CreateGeometryQueryRequest(plan);
        var execResult = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        if (execResult.IsError || execResult.Value == null)
        {
            throw new RevitPipeException($"Measured geometric query script failed: {execResult.Message}");
        }

        var actuals = MeasuredGeometryJsonParser.ParseActuals(execResult.Value.Value);
        var engine = tolerances != null ? new MeasuredGeometricEngine(tolerances) : _engine;

        return engine.AuditPlan(plan, actuals);
    }

    private static ExecuteRequest CreateGeometryQueryRequest(BimExecutionPlan plan)
    {
        var planSummary = new
        {
            planId = plan.PlanId.ToString(),
            planHash = plan.PlanHash,
            items = plan.Items
        };

        return new ExecuteRequest(
            Code: RevitGeometryQueryScriptTemplate.GetScriptCode(),
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: Math.Max(30, plan.Options.TimeoutSeconds),
            Label: "Measured Geometric Query vs BimExecutionPlan",
            Args: BridgeJson.ToElement(planSummary));
    }
}
