using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.McpBridge.Model;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcMgdApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     Phase-1 spike (ADR-02 / ADR-05 open questions). Steps 1–3 run on the command thread; steps 4–6
///     run from a background thread because that is where the pipe listener will call from. Every step
///     records a verdict; the report goes to the bridge log folder so an unattended `acad.exe /b` run
///     leaves evidence behind. It appends geometry to the active drawing and, with quit, discards it —
///     so it only runs when <see cref="EnableVariable"/> is set in acad.exe's environment. Deleted once
///     phase 2 replaces it with the real executor.
/// </summary>
public sealed class SpikeRunner
{
    /// <summary>Set to "1" before starting AutoCAD to allow the spike commands; anything else makes them a no-op.</summary>
    public const string EnableVariable = "HPAUTOCAD_MCP_SPIKE";

    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(15);

    private readonly ScriptCompiler _compiler;
    private readonly string _logDirectory;
    private readonly object _gate = new();
    private readonly List<string> _lines = [];
    private int _mainThreadId;

    public SpikeRunner(ScriptCompiler compiler, string logDirectory)
    {
        _compiler = compiler;
        _logDirectory = logDirectory;
    }

    public static bool Enabled => Environment.GetEnvironmentVariable(EnableVariable) == "1";

    public string ReportPath => Path.Combine(_logDirectory, "spike-report.md");

    /// <summary>Called on AutoCAD's command thread. Returns immediately; the background part finishes on its own.</summary>
    public string Start(bool quitWhenDone)
    {
        if (!Enabled)
            return $"[HPAutoCad MCP] spike disabled: it draws into the active drawing{(quitWhenDone ? " and discards it" : "")}. Set {EnableVariable}=1 before starting AutoCAD to run it.";

        lock (_gate)
        {
            _mainThreadId = Environment.CurrentManagedThreadId;
            _lines.Clear();
        }

        Record($"# HPAutoCad MCP bridge — phase 1 spike ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
        Record($"main thread {_mainThreadId}; AutoCAD {AcadApp.Version}; quitWhenDone={quitWhenDone}");

        Step("1 load context", CheckLoadContexts);
        Step("2 roslyn with document", CompileAndRunWithDocument);
        Step("3 wpf modeless window", ShowModelessWindow);

        _ = Task.Run(() => BackgroundAsync(quitWhenDone));

        return $"[HPAutoCad MCP] spike steps 1-3 done; 4-6 running in the background, report → {ReportPath}";
    }

    private async Task BackgroundAsync(bool quitWhenDone)
    {
        try
        {
            await StepAsync("4a idle from background thread", IdleFromBackgroundAsync);
            // 4b ExecuteInApplicationContext from a background thread: rejected by the first run (callback on the
            // calling thread, 12.9 s block) and it blocked the spike for good on the second — not repeated.
            await StepAsync("5 busy: command in progress", BusyCommandAsync);
            Record("6 modal dialog while idle: NOT TESTED (unattended run cannot dismiss a dialog)");
            WriteReport();

            if (quitWhenDone) await QuitAsync();
        }
        catch (Exception exception)
        {
            // Nothing awaits this task; a throw here would otherwise vanish as an unobserved exception.
            Log.Error(exception, "spike background part failed");
        }
    }

    /// <summary>
    ///     Both calls need the application context, hence the Idle ticks. CloseAndDiscard [undocumented, AcMgd]
    ///     drops the drawing without the save prompt a command-line `_.CLOSE` would raise; Quit on the next
    ///     tick ends acad.exe. The harness still kills the process if this does not.
    /// </summary>
    private async Task QuitAsync()
    {
        Record(await OnIdleAsync(() => Document().CloseAndDiscard(), "close and discard"));
        Record(await OnIdleAsync(AcadApp.Quit, "quit"));
    }

    // ---- steps -----------------------------------------------------------------------------------

    private void CheckLoadContexts()
    {
        var roslyn = typeof(Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript).Assembly;
        var immutable = typeof(System.Collections.Immutable.ImmutableArray).Assembly;
        var acdb = typeof(Database).Assembly;
        var acmgd = typeof(AcMgdApp).Assembly;
        var self = typeof(SpikeRunner).Assembly;

        Record($"bridge '{ScriptingSelfCheck.ContextOf(self)}' · Roslyn {roslyn.GetName().Version} '{ScriptingSelfCheck.ContextOf(roslyn)}' · Immutable {immutable.GetName().Version} '{ScriptingSelfCheck.ContextOf(immutable)}' · AcDbMgd {acdb.GetName().Version} '{ScriptingSelfCheck.ContextOf(acdb)}' · AcMgd {acmgd.GetName().Version} '{ScriptingSelfCheck.ContextOf(acmgd)}'");

        if (ScriptingSelfCheck.ContextOf(roslyn) != "HPAutoCad.McpBridge") throw new InvalidOperationException("Roslyn resolved outside the bridge load context");
        if (roslyn.GetName().Version?.Major != 5) throw new InvalidOperationException("Roslyn is not the 5.x the bridge ships (AutoCAD's 4.10 won)");
        if (ScriptingSelfCheck.ContextOf(acdb) == "HPAutoCad.McpBridge") throw new InvalidOperationException("AcDbMgd loaded twice — AutoCAD API must come from the default context");
        if (ScriptingSelfCheck.ContextOf(acmgd) == "HPAutoCad.McpBridge") throw new InvalidOperationException("AcMgd loaded twice — AutoCAD API must come from the default context");
    }

    private void CompileAndRunWithDocument()
    {
        var doc = Document();
        // `Application.DocumentManager` is the AcMgd idiom every AutoCAD sample uses; it must resolve in a script.
        var compiled = _compiler.GetOrCompile("return db.Filename + \" | layer \" + tr.GetObject(db.Clayer, OpenMode.ForRead).GetType().Name + \" | \" + units.ToDrawing(25.4) + \" | docs \" + Application.DocumentManager.Count;");
        if (!compiled.Succeeded) throw new InvalidOperationException(string.Join("; ", compiled.Diagnostics.Select(d => $"{d.Line}:{d.Column} {d.Message}")));

        using var transaction = doc.Database.TransactionManager.StartTransaction();
        var globals = new AutocadScriptGlobals(doc, doc.Database, doc.Editor, AcadApp.DocumentManager, transaction, ScriptUnits.Millimeters,
            CancellationToken.None, _ => { }, (_, _, _) => { });
        var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;
        transaction.Abort();

        Record($"script returned: {value}");
    }

    private void ShowModelessWindow()
    {
        var window = new Window
        {
            Title = "HPAutoCad MCP spike",
            Width = 360, Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new TextBlock { Text = "WPF window from the isolated load context. Close me.", Margin = new Thickness(16) },
        };
        AcadApp.ShowModelessWindow(window);
        Record($"window shown, IsVisible={window.IsVisible}, dispatcher thread {window.Dispatcher.Thread.ManagedThreadId}");
        window.Close();
    }

    private async Task IdleFromBackgroundAsync()
    {
        Record($"background thread {Environment.CurrentManagedThreadId}");
        var subscribed = Stopwatch.StartNew();
        var result = await OnIdleAsync(() =>
        {
            var doc = Document();
            var quiescent = AcadApp.IsQuiescent;
            using var docLock = doc.LockDocument();
            using var transaction = doc.Database.TransactionManager.StartTransaction();
            var blockTable = (BlockTable)transaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var line = new Line(Point3d.Origin, new Point3d(100, 50, 0));
            modelSpace.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
            transaction.Commit();
            return $"idle handler on thread {Environment.CurrentManagedThreadId} (main={Environment.CurrentManagedThreadId == _mainThreadId}), quiescent={quiescent}, line {line.Handle} committed after {subscribed.ElapsedMilliseconds} ms";
        }, "line");
        Record(result);
    }

    private async Task BusyCommandAsync()
    {
        // Start an interactive command from the main thread, then observe what a request would see.
        await OnIdleAsync(() => Document().SendStringToExecute("_.LINE ", true, false, false), "start LINE");
        await Task.Delay(1500);

        var seen = await OnIdleAsync(() => $"during LINE: quiescent={AcadApp.IsQuiescent}", "observe");
        Record(seen);

        await OnIdleAsync(() => Document().SendStringToExecute("\x1B\x1B", true, false, false), "escape");
        await Task.Delay(1000);

        var after = await OnIdleAsync(() => $"after ESC: quiescent={AcadApp.IsQuiescent}", "observe after");
        Record(after);
    }

    // ---- helpers ---------------------------------------------------------------------------------

    /// <summary>
    ///     Runs <paramref name="work"/> on the next Idle tick; the subscribe itself happens on the calling
    ///     (background) thread. A timeout completes the request first and then unsubscribes, so a tick that
    ///     arrives late finds the request finished and never touches the document — the contract the
    ///     bridge's main-thread executor keeps as well.
    /// </summary>
    private async Task<string> OnIdleAsync(Func<string> work, string label)
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            AcadApp.Idle -= handler;
            if (completion.Task.IsCompleted) return;

            try { completion.TrySetResult(work()); }
            catch (Exception exception) { completion.TrySetResult($"{label}: idle handler threw {exception.GetType().Name}: {exception.Message}"); }
        };

        try
        {
            AcadApp.Idle += handler;
        }
        catch (Exception exception)
        {
            return $"{label}: Idle += from thread {Environment.CurrentManagedThreadId} threw {exception.GetType().Name}: {exception.Message}";
        }

        using var timeout = new CancellationTokenSource();
        var finished = await Task.WhenAny(completion.Task, Task.Delay(StepTimeout, timeout.Token));
        if (finished == completion.Task)
        {
            timeout.Cancel();
            return completion.Task.Result;
        }

        var timedOut = $"{label}: no Idle tick within {StepTimeout.TotalSeconds}s (subscribed from thread {Environment.CurrentManagedThreadId})";
        completion.TrySetResult(timedOut);
        AcadApp.Idle -= handler;
        return timedOut;
    }

    private Task<string> OnIdleAsync(Action work, string label) => OnIdleAsync(() => { work(); return label + ": ok"; }, label);

    private static Document Document() =>
        AcadApp.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("No drawing is open");

    private void Step(string name, Action action)
    {
        try { action(); Record($"PASS {name}"); }
        catch (Exception exception) { Record($"FAIL {name}: {exception.GetType().Name}: {exception.Message}"); }
    }

    private async Task StepAsync(string name, Func<Task> action)
    {
        try { await action(); Record($"PASS {name}"); }
        catch (Exception exception) { Record($"FAIL {name}: {exception.GetType().Name}: {exception.Message}"); }
    }

    private void Record(string line)
    {
        lock (_gate) _lines.Add(line);
        Log.Information("spike: {Line}", line);
    }

    private void WriteReport()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            string report;
            lock (_gate) report = string.Join(Environment.NewLine, _lines) + Environment.NewLine;
            File.WriteAllText(ReportPath, report, new UTF8Encoding(false));
            Log.Information("spike report written to {Path}", ReportPath);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "spike report could not be written");
        }
    }
}
