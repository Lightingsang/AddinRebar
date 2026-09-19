---
name: navisworks-ribbon-api-and-harness-facts
description: Reflection-verified Navisworks 2026 ribbon attribute defaults, the PowerShell continue-in-switch trap, and the UIA "buttons only while tab displayed" tautology to check in ribbon harnesses
metadata:
  type: project
---

Verified 2026-09-16 (review of plan 260916-0005-navisworks-ribbon-tab):

- `CommandAttribute.CallCanExecute`, `RibbonTabAttribute.CallCanExecute` and `AddInPluginAttribute.CallCanExecute` all default to `CallCanExecute.Always` (enum value 0; `DocumentNotClear` = 1) — reflection on `Autodesk.Navisworks.Api.dll` 23.0. A comment claiming "the default greys the button on a clear document" is wrong; setting `Always` explicitly is harmless.
- `Autodesk.Navisworks.Api.dll` loads fine via `Assembly.LoadFrom` in an xUnit net48 process (no native deps needed just to read plugin attributes) — `NavisworksApiProbe` + `Private=false` reference is enough.
- PowerShell (5.1 and 7): `continue` inside a `switch` case leaves only the switch, not the enclosing `foreach` — execution falls to the line after the switch. Flag any `switch { ... else { continue } }` used as "try next pattern".
- AdWindows ribbon (Navisworks and AutoCAD): a tab's buttons enter the UIA tree only while that tab is displayed, so "button X absent" checks made while another tab is selected are tautological; check the tab header instead (`Button` with `AutomationId` = tab id). Navisworks greys the whole ribbon on its start page (no document) — not a plugin state.
- The foreign `NavisworksMCPPlugin` on the dev machine uses `x:Uid="CustomRibbonTab"` (SDK default) and panel Uids `RibbonPanel_MCP`/`RibbonPanelSource_MCP` and still loads beside ours — Uid uniqueness across plugins is evidently not enforced, but rename ours anyway.

**Why:** these took a reflection probe, a PS experiment and a screenshot read to establish; the code and docs asserted the opposite or nothing.

**How to apply:** when reviewing any `[Command]`/`[RibbonTab]` comment about defaults, UIA harness "absence" checks, or `switch`-driven pattern fallbacks in the harness scripts.
