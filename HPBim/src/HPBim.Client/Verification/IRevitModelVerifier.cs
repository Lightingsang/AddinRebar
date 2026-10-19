namespace HPBim.Client.Verification;

using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Models;

/// <summary>
///     Contract for verifying element counts and categories in an active Revit model.
/// </summary>
public interface IRevitModelVerifier
{
    /// <summary>
    ///     Executes a read-only query against Revit Document and verifies counts against the expected model.
    /// </summary>
    Task<ModelVerificationReport> VerifyAsync(BimModel expectedModel, CancellationToken cancellationToken = default);
}
