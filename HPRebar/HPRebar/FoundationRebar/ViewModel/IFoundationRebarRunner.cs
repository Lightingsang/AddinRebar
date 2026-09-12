using System.Threading.Tasks;
using HPRebar.FoundationRebar.Model;

namespace HPRebar.FoundationRebar.ViewModel;

/// <summary>
///     Whatever actually builds the mat. The window only needs to know it finishes or throws, which keeps
///     the transaction and external event plumbing out of the view model.
/// </summary>
public interface IFoundationRebarRunner
{
    /// <summary>
    ///     Builds both mats for the session's spec. Completes once Revit has run it, which is not the call
    ///     that started it; throws when nothing was left behind.
    /// </summary>
    Task RunAsync(FoundationSession session);
}
