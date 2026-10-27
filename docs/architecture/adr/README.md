# Architecture Decision Records — RevitAddinAI

One file per decision, `NNNN-kebab-title.md`, never renumbered. Status: **Proposed** (awaiting user approval) → **Accepted** → **Superseded by NNNN**. An Accepted ADR changes only through a new ADR.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-adopt-pragmatic-clean-code-governance.md) | Adopt Pragmatic Clean Code governance | Accepted |
| [0002](0002-feature-sliced-monolith-with-pure-core.md) | Feature-sliced add-in + pure domain library (keep) | Accepted |
| [0003](0003-composition-root-without-container.md) | Command as composition root, constructor injection, no DI container | Accepted |
| [0004](0004-shared-kernel-folders.md) | Shared kernel folders `Shared/` and `HPRebar.Core/Shared/` | Accepted |
| [0005](0005-static-policy.md) | Static policy and mutable-state allowlist | Accepted |
| [0006](0006-kata-feature-boundary.md) | Kata Export / Kata Rebar boundary | Accepted — option A |
| [0007](0007-hp-clean-code-scope.md) | Extend the clean-code governance to every MCP folder and to AI-proposed tools | Accepted |
| [0008](0008-bim-toolkit-webview2-ui.md) | HP BIM Toolkit: separate add-in with a WebView2 + React UI | Accepted |
| [0009](0009-bim-toolkit-uses-hprebar-core.md) | HP BIM Toolkit uses HPRebar.Core for Kata; Kata's Revit adapters are ported | Superseded by 0010 |
| [0010](0010-kata-export-web-ui-in-hprebar.md) | Kata Export: WebView2 + React window inside HPRebar on Revit 2026 | Accepted; decisions 1 and 3 superseded by 0011 |
| [0011](0011-kata-export-web-only-native-drawing.md) | Kata Export: web window only, drawn by the page, Revit 2026 only | Accepted |
| [0012](0012-kata-page-ui-kit-and-viewer-choices.md) | Kata Export page: shadcn/ui kit, VI/EN and light/dark chosen in the page, no drawing export | Accepted |
| [0013](0013-kata-settings-web-page.md) | Kata Settings: a web page on Revit 2026, the WPF dialog removed | Accepted |

Template:

```markdown
# NNNN — Title
- **Status:** Proposed | Accepted (YYYY-MM-DD, by …) | Superseded by NNNN
- **Tags:** [PCC] [REVIT] [PROJECT]
## Context      — forces, with evidence (file:line, audit ids)
## Decision     — what we do, stated as rules
## Consequences — what gets easier/harder, what it costs
## Alternatives — considered and why rejected
## Rules        — PCC / DEPENDENCY_RULES ids this decision applies
```
