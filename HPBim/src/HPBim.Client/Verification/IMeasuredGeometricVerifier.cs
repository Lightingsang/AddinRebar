namespace HPBim.Client.Verification;

using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Verification;

public interface IMeasuredGeometricVerifier
{
    Task<GeometricAuditReport> VerifyPlanGeometryAsync(
        BimExecutionPlan plan,
        CoordinationTolerances? tolerances = null,
        CancellationToken cancellationToken = default);
}
