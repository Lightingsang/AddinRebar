using Autodesk.Revit.DB;

namespace HPRebar.JoinElements.Service;

/// <summary>
/// Suppresses and resolves Revit join warnings such as 'Highlighted elements are joined but do not intersect'
/// so they do not interrupt execution or clutter the document's warnings list.
/// </summary>
public sealed class JoinWarningFailuresPreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        var failureMessages = failuresAccessor.GetFailureMessages();
        foreach (var failure in failureMessages)
        {
            var severity = failure.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                // Delete harmless warnings like 'Highlighted elements are joined but do not intersect'
                failuresAccessor.DeleteWarning(failure);
            }
            else if (severity == FailureSeverity.Error && failure.HasResolutions())
            {
                failuresAccessor.ResolveFailure(failure);
            }
        }

        return FailureProcessingResult.Continue;
    }
}
