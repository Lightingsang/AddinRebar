namespace HPDynamo.McpBridge.Support;

using System;
using System.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.Graph.Workspaces;
using HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Globals available to C# scripts executed via execute_dynamo_code.
/// </summary>
public sealed class DynamoScriptGlobals
{
    public object dynamo { get; }
    public WorkspaceModel? workspace { get; }
    public Document doc { get; }
    public UIDocument uidoc { get; }
    public Application app { get; }
    public UIApplication uiapp { get; }
    public CancellationToken ct { get; }
    public Action<string?> log { get; }
    public Action<int, int, string> progress { get; }
    public ScriptArgs args { get; }
    public bool dryRun { get; }
    public DynamoScriptGlobals globals => this;

    public DynamoScriptGlobals(
        object dynamoModelOrViewModel,
        WorkspaceModel? activeWorkspace,
        Document document,
        UIDocument uiDocument,
        Application application,
        UIApplication uiApplication,
        CancellationToken cancellationToken,
        Action<string?> logAction,
        Action<int, int, string> progressAction,
        ScriptArgs scriptArguments,
        bool isDryRun = false)
    {
        dynamo = dynamoModelOrViewModel;
        workspace = activeWorkspace;
        doc = document;
        uidoc = uiDocument;
        app = application;
        uiapp = uiApplication;
        ct = cancellationToken;
        log = logAction;
        progress = progressAction;
        args = scriptArguments;
        dryRun = isDryRun;
    }

    /// <summary>
    ///     Executes a graph run and awaits evaluation completion.
    /// </summary>
    public bool RunAndWait(Action? runAction = null, TimeSpan? timeout = null)
    {
        var model = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model
                    ?? (dynamo as Dynamo.Models.DynamoModel);
        if (model == null) return false;

        var targetAction = runAction;
        if (targetAction == null)
        {
            var targetWs = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model?.CurrentWorkspace as HomeWorkspaceModel
                           ?? workspace as HomeWorkspaceModel;
            if (targetWs == null) return false;
            targetAction = () => targetWs.Run();
        }

        return Execution.DynamoEvaluationSync.RunAndWait(
            model,
            targetAction,
            timeout ?? TimeSpan.FromSeconds(60),
            ct,
            log);
    }
}
