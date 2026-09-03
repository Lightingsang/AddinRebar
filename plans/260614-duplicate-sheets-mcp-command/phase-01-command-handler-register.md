# Phase 01 — C# `duplicate_sheets` command + handler + register

**Priority:** P0 · **Status:** ☐ · **Depends on:** none (all in one assembly)

## Overview

Add the C# half of the triad inside the merged add-in, following the existing in-process MCP pattern
(`Get Document Info` reference triad). The handler runs on the Revit UI thread via `ExternalEvent`, resolves
source sheets by number, builds a `NamingRule` (mirror), and drives the **existing** `NamingRuleEngine` +
`SheetDuplicator` (same assembly — Serilog already wired) inside ONE atomic transaction.

## Files to create

- `RevitAIApp/MyRevitAIApp/MCP Server/Duplicate Sheets/DuplicateSheetsCommand.cs`
- `RevitAIApp/MyRevitAIApp/MCP Server/Duplicate Sheets/DuplicateSheetsEventHandler.cs`

Feature-folder convention: folder `Duplicate Sheets/` (spaces); namespace `MyRevitAIApp.McpServer.DuplicateSheets`
(strip spaces). Reuses `MCP Server/Models/Prediction3D/DuplicateSheetsParameters.cs` (`MyRevitAIApp.McpServer.Models`).

## File to edit

- `RevitAIApp/MyRevitAIApp/MCP Server/McpSocketService.cs` — add the `using` + one `RegisterCommand` line.

## DuplicateSheetsCommand.cs (sketch — mirrors GetDocumentInfoCommand)

```csharp
using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using MyRevitAIApp.McpServer.Models;
using MyRevitAIApp.McpServer.Sdk;

namespace MyRevitAIApp.McpServer.DuplicateSheets
{
    public sealed class DuplicateSheetsCommand : ExternalEventCommandBase
    {
        private DuplicateSheetsEventHandler _handler => (DuplicateSheetsEventHandler)Handler;
        public override string CommandName => "duplicate_sheets";

        public DuplicateSheetsCommand(UIApplication uiApp)
            : base(new DuplicateSheetsEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var data = parameters.ToObject<DuplicateSheetsParameters>()
                           ?? throw new ArgumentException("Duplicate-sheets params were null");
                if (data.SourceSheetNumbers == null || data.SourceSheetNumbers.Count == 0)
                    throw new ArgumentException("sourceSheetNumbers must contain at least one sheet number");

                _handler.SetParameters(data);
                if (RaiseAndWaitForCompletion(120000))   // whole batch, no cap; matches TCP ceiling
                    return _handler.Result;
                throw new TimeoutException("Duplicate sheets operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to duplicate sheets: {ex.Message}");
            }
        }
    }
}
```

## DuplicateSheetsEventHandler.cs (sketch)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MyRevitAIApp.McpServer.Models;
using MyRevitAIApp.McpServer.Sdk;
using MyRevitAIApp.DuplicateSheet.Models;     // NamingRule, SheetContentOptions, DuplicateMode
using MyRevitAIApp.DuplicateSheet.Services;   // INamingRuleEngine, ISheetDuplicator + impls

namespace MyRevitAIApp.McpServer.DuplicateSheets
{
    public sealed class DuplicateSheetsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new(false);
        private readonly INamingRuleEngine _engine = new NamingRuleEngine();
        private readonly ISheetDuplicator _duplicator = new SheetDuplicator();   // Serilog inside — OK now
        private DuplicateSheetsParameters _params = new();
        public object Result { get; private set; }

        public void SetParameters(DuplicateSheetsParameters p) => _params = p;
        public bool WaitForCompletion(int timeoutMs) { _resetEvent.Reset(); return _resetEvent.WaitOne(timeoutMs); }
        public string GetName() => "Duplicate Sheets (MCP)";

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null) { Result = new { success = false, message = "No active document." }; return; }

                var all = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
                var byNumber = all.GroupBy(s => s.SheetNumber, StringComparer.OrdinalIgnoreCase)
                                  .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var sources = new List<ViewSheet>(); var missing = new List<string>();
                foreach (var n in _params.SourceSheetNumbers)
                    if (byNumber.TryGetValue(n, out var s)) sources.Add(s); else missing.Add(n);
                if (sources.Count == 0)
                { Result = new { success = false, message = "None of the requested sheet numbers were found.", missingSheetNumbers = missing }; return; }

                // MIRROR: prefix/suffix/find/replace → number AND name; sequence → number only
                int incStart = _params.UseSequence ? Math.Max(1, _params.SequenceStart) : 0;
                var rule = new NamingRule(
                    _params.Prefix, _params.Suffix, _params.FindText, _params.ReplaceText, incStart, _params.SequencePadding,
                    _params.Prefix, _params.Suffix, _params.FindText, _params.ReplaceText);
                var mode = MapMode(_params.Mode);
                var content = new SheetContentOptions(_params.CopyTitleBlock, _params.IncludeLegends,
                                                      _params.IncludeSchedules, _params.CopySheetAnnotations);
                var taken = new HashSet<string>(all.Select(s => s.SheetNumber), StringComparer.OrdinalIgnoreCase);

                var rows = new List<object>();
                using var t = new Transaction(doc, "Duplicate Sheets (MCP)");
                t.Start();
                for (int i = 0; i < sources.Count; i++)
                {
                    var src = sources[i];
                    var (newNumber, newName, _) = _engine.Apply(src.SheetNumber, src.Name, i, rule, taken);
                    var outcome = _duplicator.DuplicateOne(doc, src, newNumber, newName, mode, content);
                    if (!outcome.Success)
                    {
                        t.RollBack();
                        Result = new { success = false, message = $"Failed on sheet {src.SheetNumber}: {outcome.ErrorMessage}",
                                       failedSourceNumber = src.SheetNumber };
                        return;
                    }
                    taken.Add(newNumber);
                    rows.Add(new { sourceNumber = src.SheetNumber, newNumber = outcome.NewNumber, newName = outcome.NewName,
                                   viewportsCopied = outcome.ViewportsCopied, legendsPlaced = outcome.LegendsPlaced,
                                   schedulesPlaced = outcome.SchedulesPlaced, annotationsCopied = outcome.AnnotationsCopied });
                }
                t.Commit();

                Result = new { success = true, duplicatedCount = rows.Count, sheets = rows, missingSheetNumbers = missing };
            }
            catch (Exception ex) { Result = new { success = false, message = ex.Message }; }
            finally { _resetEvent.Set(); }
        }

        private static DuplicateMode MapMode(string? mode) => (mode ?? "").Trim().ToLowerInvariant() switch
        {
            "withdetailing" => DuplicateMode.WithDetailing,
            "asdependent"   => DuplicateMode.AsDependent,
            _               => DuplicateMode.Duplicate,   // "justsheet" / "duplicate" / null
        };
    }
}
```

## Register (McpSocketService.cs)

Add `using MyRevitAIApp.McpServer.DuplicateSheets;` near the other command usings, then one line in
`RegisterAllCommands()` (e.g. after `GetDocumentInfoCommand` at line 74, or a new `// Sheets` group):

```csharp
        // Sheets
        _registry.RegisterCommand(new DuplicateSheetsCommand(uiApp));
```

## Behavior baked in

- **Atomic batch:** one transaction → fail mid-way rolls back everything; one Ctrl+Z. Matches ribbon `ExecuteBatch`.
- **Collision:** `NamingRuleEngine` appends ` (2)`, ` (3)`… vs `taken` (existing + batch-so-far), case-insensitive.
- **Missing numbers:** reported, non-fatal unless ALL missing.
- **Mode aliases:** `JustSheet`/`Duplicate`/null → `Duplicate`.

## Todo

- [ ] Create `Duplicate Sheets/DuplicateSheetsCommand.cs`
- [ ] Create `Duplicate Sheets/DuplicateSheetsEventHandler.cs`
- [ ] Register in `McpSocketService.cs` (+ using)
- [ ] `dotnet build "RevitAIApp/MyRevitAIApp/MyRevitAIApp.csproj" -c Debug.R27` → 0 err (HARD-GATE)

## Success criteria

- Compiles on the newest config; `CommandName == "duplicate_sheets"` matches the TS tool (Phase 02).
- Result JSON is self-describing for the agent.

## Risks / notes

- **`Result` nullable warning:** `object Result { get; private set; }` matches the reference handler (which has the
  same pattern). Keep consistent; project nullable warnings on this are pre-existing.
- **Timeout:** 120 s ≈ TCP SocketClient ceiling. Huge/WithDetailing batches that exceed it → raise SocketClient
  timeout (shared) rather than capping batch (user chose no cap). Note in TS description.
- **`McpSocketService` usings:** verify whether it uses per-namespace `using`s or a GlobalUsings; add the
  `DuplicateSheets` namespace accordingly.

## Open questions

- None blocking. Result could later be a typed `AIResult<T>`; anon object kept for parity with `get_document_info`.
