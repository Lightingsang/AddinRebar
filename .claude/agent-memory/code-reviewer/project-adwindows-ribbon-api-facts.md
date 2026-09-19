---
name: adwindows-ribbon-api-facts
description: AutoCAD AdWindows 25.1.0 Ribbon facts verified from metadata (RibbonItem is a plain INotifyPropertyChanged, RibbonControl is a WPF Control) plus the WPF Geometry.Parse fill-rule gotcha — needed whenever HPAutoCad Ribbon code is reviewed
metadata:
  type: project
---

Verified 2026-09-14 with `System.Reflection.Metadata` over `~/.nuget/packages/autocad.net/25.1.0/lib/net8.0/AdWindows.dll` (script pattern: PEReader → GetMetadataReader → TypeDefinitions, filter namespace `Autodesk.Windows`):

- `RibbonItem : System.Object` (INotifyPropertyChanged, **not** `DependencyObject`) → setting `RibbonLabel.Text` off the UI thread does not throw; WPF bindings marshal `PropertyChanged`. `RibbonCommandItem : RibbonItem` (props `CommandHandler`, `IsCheckable`, `IsChecked`), `RibbonButton : RibbonCommandItem`, `RibbonToggleButton : RibbonButton` (`CheckState`, `IsThreeState`), `RibbonLabel`/`RibbonRowBreak : RibbonItem`, `RibbonTab`/`RibbonPanel`/`RibbonPanelSource : Object`.
- `RibbonControl : System.Windows.Controls.Control` → `ComponentManager.Ribbon.Dispatcher` is the main-thread WPF dispatcher. `RibbonControl.FindTab(id)` / `FindItem(...)` exist. `ComponentManager` events: `ItemInitialized`, `ItemExecuted`, `PreviewExecute`, `UIElementActivated`, `ToolTipOpened/Closed` — no "RibbonCreated"; the standard pattern is `ItemInitialized` + `Ribbon != null` check.
- `RibbonToolTip.Command` is a plain string ("command string displayed in the tooltip").
- WPF `Geometry.Parse` returns a **frozen `StreamGeometry`** with `FillRule.EvenOdd` by default (`F1` prefix = Nonzero); an `is PathGeometry` check on its result is dead code.
- Live-proven (ribbon-live-check 2026-09-14): a `WSCURRENT` change drops code-added tabs; `SystemVariableChanged → Application.Idle → EnsureCreated` restores exactly one. UIA exposes a tab header as a `Button` with `AutomationId = RibbonTab.Id`.

**Why:** the ribbon review (`plans/260914-2204-autocad-ribbon-tab/reports/ribbon-code-review.md`) needed these to judge dispatcher/thread safety and the icon fill-rule bug; none are in the repo or the XML docs.

**How to apply:** when reviewing `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/*`, check thread claims against these base types instead of assuming DependencyObject semantics; re-verify with the metadata script if the `AutoCAD.NET` package version changes. Related: [[autocad-net-assembly-ownership-and-casing]].
