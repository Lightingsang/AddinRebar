---
name: project-navis-bim-coordinator-review-checks
description: Recurring checks for HPNavis.BIMCoordinator (clash matrix → search sets → clash tests → canary) reviews, slice 1 2026-10-10
metadata:
  type: project
---

Defect classes found in slice 1 (report plans/261010-1110-bim-coordinator-navis/reports/code-review-slice1.md, 6.5/10):
- Upsert identity that embeds a mutable attribute (priority in the test name) → duplicate + orphan on a matrix edit. Check identity = parsed (ruleId, lod).
- Count caps that only guard one input path (canary `count` capped, explicit `ruleIds` not) — check every list arg of a heavy seed.
- `ToDictionary(name)` over the whole document's tests/sets: user duplicates throw ArgumentException after writes → rollback + stability-excluded error.
- `doc.Models[i].RootItem.Children` are levels for an NWC model, not files; plain `List<T>` envelopes escape the 200-item Navis-collection cap; over 64 KB the bridge returns a truncated string.
- Heavy fence for engine code must be IL/semantic and single-sourced from `NavisHeavyGate.HeavyMembers` — the gate only scans seed text.
- New engine assembly → PluginAssemblyResolver allow-list + ScriptingSelfCheck line (NI1).

Search-set registry v2 (report code-review-searchsets.md, 7/10):
- Clash tests bind to sets by PATH (live), but the planner compares only the resolved DisplayName → after a folder rename (ARC→Architecture) tests stay on the old sets and read Unchanged. Check identity = full path.
- Set identity "<code> <name>" in one folder → a registry rename/move creates a second set; no orphan report under the owned folder.
- Inventory tools: cap entries ≠ cap bytes; 37 sets = 1 219 SearchConditions (roles × categories DNF) → ListSelectionSets > 64 KB. Count conditions with python over the JSON.
- Validator overlap = ModelItem equality only; PruneBelowMatch per set hides ancestor/descendant overlap (curtain wall vs panels).
- Wildcard `*-{role}-????*` vs IsoFileName regex = two role rules (K1); single-letter roles match any `-X-` field.
- Registry Validate: detail parent optional but validator indexes `members[p.Parent!]`; values parsed only at run time.

Colour by search set (report code-review-colors.md, 7.5/10):
- Paint/verify tools: success must be false when no set resolved (all "not in document" skips → success:true today).
- Positional codes (`C{order}` from sheet row) feed the allowUpdate approval gate — a row insert retargets approvals. Check codes are stable data.
- Expected-colour/read-back by item-exact `Contains` ignores nesting under PruneBelowMatch (parent in set X, nested family in later set Y).
- Reset scope = current members only → stale colours after model/sheet change; Default sets resolved (full search) in every mode vs 120 s cap; `*` category should not be "category first".
- Alternatives (values) OR-expand: notEquals/bool alternatives = tautology; no group cap. Verify generator regen == committed JSON (python --out scratch + diff).

**Why:** engine code referenced by the script compiler is outside the guard and the heavy gate; only tests fence it.
**How to apply:** rerun these checks on slice 2 (issue state, reports beside the NWD) and any other Navis engine. Related: [[project-aec-mcp-review-recurring-checks]].
