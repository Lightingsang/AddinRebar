namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

/// <summary>
///     Encapsulates a work request dispatched to the Revit UI thread with identity validation.
/// </summary>
internal sealed record McpBridgeWorkRequest(
    string Name,
    Func<UIApplication, CancellationToken, object?> Work,
    CancellationToken CancellationToken,
    string? TargetDocTitle = null,
    string? TargetDocPath = null,
    Guid? TargetDefId = null,
    bool RequireTargetIdentity = false)
{
    public string? TargetDocTitle { get; init; } = string.IsNullOrWhiteSpace(TargetDocTitle) ? null : TargetDocTitle;
    public string? TargetDocPath { get; init; } = string.IsNullOrWhiteSpace(TargetDocPath) ? null : TargetDocPath;

    public TaskCompletionSource<object?> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void ValidateActiveDocument(UIApplication? app)
    {
        var hasTarget = TargetDocTitle != null || TargetDocPath != null ||
                        (TargetDefId.HasValue && TargetDefId.Value != Guid.Empty);

        if (RequireTargetIdentity && !hasTarget)
            throw new InvalidOperationException($"Write operation '{Name}' rejected: Target document or definition identity must be specified to prevent unintended document mutation.");

        if (!hasTarget)
            return;

        if (app != null)
        {
            ValidateRevitDoc(app);
        }
        else if (TargetDocTitle != null || TargetDocPath != null)
        {
            throw new InvalidOperationException($"Document identity mismatch: expected '{TargetDocTitle ?? TargetDocPath}', but no document is open in Revit.");
        }

        if (TargetDefId.HasValue && TargetDefId.Value != Guid.Empty)
        {
            ValidateGrasshopperDef();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ValidateRevitDoc(UIApplication app)
    {
        var currentDoc = app.ActiveUIDocument?.Document;
        if (currentDoc != null)
        {
            if (TargetDocTitle != null && !string.Equals(currentDoc.Title, TargetDocTitle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Document identity mismatch: expected '{TargetDocTitle}', but active document is '{currentDoc.Title}'.");

            if (TargetDocPath != null)
            {
                if (string.IsNullOrWhiteSpace(currentDoc.PathName))
                    throw new InvalidOperationException($"Document path mismatch: expected '{TargetDocPath}', but active document '{currentDoc.Title}' has not been saved to disk (PathName is empty).");

                if (!string.Equals(currentDoc.PathName, TargetDocPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Document path mismatch: expected '{TargetDocPath}', but active document is '{currentDoc.PathName}'.");
            }
        }
        else if (TargetDocTitle != null || TargetDocPath != null)
        {
            throw new InvalidOperationException($"Document identity mismatch: expected '{TargetDocTitle ?? TargetDocPath}', but no document is open in Revit.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ValidateGrasshopperDef()
    {
        var ghDoc = Grasshopper.Instances.ActiveDocument;
        if (ghDoc == null)
            throw new InvalidOperationException($"Grasshopper definition identity mismatch: expected '{TargetDefId!.Value}', but no definition is open in Grasshopper.");

        if (ghDoc.DocumentID != TargetDefId!.Value)
            throw new InvalidOperationException($"Grasshopper definition identity mismatch: expected '{TargetDefId.Value}', but active definition is '{ghDoc.DocumentID}'.");
    }

    public override string ToString() => $"McpBridgeWorkRequest({Name})";
}
