using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Services;

/// <summary>
///     Opens Revit tools from the dashboard's quick actions. Only ids in <see cref="Tools" /> are accepted; Revit runs a
///     posted command after the current API call returns, so <see cref="Open" /> reports that it was queued.
/// </summary>
public static class ToolLauncher
{
    public static IReadOnlyDictionary<string, PostableCommand> Tools { get; } = new Dictionary<string, PostableCommand>(StringComparer.Ordinal)
    {
        ["revit.default3DView"] = PostableCommand.Default3DView,
        ["revit.reviewWarnings"] = PostableCommand.ReviewWarnings,
        ["revit.projectInformation"] = PostableCommand.ProjectInformation,
        ["revit.projectUnits"] = PostableCommand.ProjectUnits,
        ["revit.visibilityGraphics"] = PostableCommand.VisibilityOrGraphics,
        ["revit.manageLinks"] = PostableCommand.ManageLinks,
        ["revit.worksets"] = PostableCommand.Worksets,
        ["revit.purgeUnused"] = PostableCommand.PurgeUnused,
    };

    public static OpenToolResult Open(UIApplication app, OpenToolRequest request)
    {
        if (string.IsNullOrEmpty(request.ToolId) || !Tools.TryGetValue(request.ToolId, out var postable))
        {
            throw new BridgeException(BridgeErrorCodes.BadRequest,
                $"Unknown tool '{request.ToolId}'. Known tools: {string.Join(", ", Tools.Keys)}.");
        }

        ActiveDocument.Require(app);
        var commandId = RevitCommandId.LookupPostableCommandId(postable);
        if (!app.CanPostCommand(commandId))
        {
            throw new BridgeException(BridgeErrorCodes.RevitApiError, $"Revit cannot open '{request.ToolId}' right now.");
        }

        app.PostCommand(commandId);
        return new OpenToolResult(true);
    }
}
