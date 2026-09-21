using System;
using System.Threading;
using HPRebar.McpBridge.Core.Scripting;
using Tekla.Structures.Model;
using UIModelObjectSelector = Tekla.Structures.Model.UI.ModelObjectSelector;

namespace HPTekla.McpBridge;

/// <summary>
///     Global variables exposed to Roslyn C# scripts executing inside Tekla Structures:
///     - model: The active Tekla Model instance
///     - selector: The UI ModelObjectSelector for querying/picking model objects
///     - ct: Cooperative cancellation token
///     - log: Action to write messages to the execution log stream
///     - progress: Action to report progress steps (current, max, message)
///     - args: Structured parameters passed into the script execution
/// </summary>
public sealed class TeklaScriptGlobals
{
    // ReSharper disable InconsistentNaming — exact names scripts use
    public Model model;
    public UIModelObjectSelector selector;
    public CancellationToken ct;
    public Action<string> log;
    public Action<int, int, string> progress;
    public ScriptArgs args;
    // ReSharper restore InconsistentNaming

    public TeklaScriptGlobals(
        Model model,
        UIModelObjectSelector selector,
        CancellationToken ct,
        Action<string> log,
        Action<int, int, string> progress,
        ScriptArgs args)
    {
        this.model = model;
        this.selector = selector;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
        this.args = args;
    }
}
