# Code review — Civil 3D MCP phase 0 (McpShared additive contracts + guard/analyzer profile + tests)

**Date:** 2026-09-17 · **Reviewer:** code-reviewer · **Scope:** read-only, verified by scratch probe against the bridge csproj + `MetadataLoadContext` sweep of the installed AutoCAD/Civil 3D 2026 API.

## Scope

| | |
|---|---|
| Files changed | 6 `McpShared/*.cs` (additive) + 1 new test file |
| Diff size | `+93 / -0` (numstat: 26/1/23/8/5/30, 0 deletions) |
| Tests | `HPRebar.Mcp.Server.Core.Tests` **192/192 pass, 0 skip** (164 baseline + 28 new), verified this run |
| Probe | guard verdicts over 51 samples + `?.` matrix over all 5 profiles + reflection sweep of `AeccDbMgd`/`AeccDataShortcutMgd`/acdbmgd |

## Acceptance criteria

| # | Criterion | Verdict | Evidence |
|---|---|---|---|
| a | Rows #1–#6 implemented; nothing else in McpShared changed | ✅ | `git diff --numstat` = 6 files, 0 deletions; each row matches the table exactly |
| b | 4 hosts byte-identical; base guard untouched | ✅ | `cmp` on all 4 before/after `tools-list` JSON = **IDENTICAL** (revit 63677 / autocad 224773 / navis 40507 / etabs 37506 B); Core.dll SHA changed (recompiled) → `3A3A…` vs `086D…`; `ScriptGuard` base list + Revit/Autocad/Navis/Etabs profiles unchanged in diff |
| c | Additive only | ✅ | new `const`, new `static readonly` fields, new nullable auto-prop `ContextResult.Civil3d`, new `record Civil3dInfo`; no signature/removal |
| d | Follows Etabs/Navis pattern | ✅ | `Civil3dProfileTests` mirrors `EtabsProfileTests`/`NavisProfileTests` (same pipe-name/superset/Shape/round-trip shape); `Civil3d` profile appended after `Etabs` block |
| e | No new warnings/errors | ✅ | Core.Tests build+run clean; `grep Autodesk\.\|AeccDbMgd McpShared --include=*.csproj` = 0 assembly refs (only string literals + doc comments) |
| f | Guard correctness | 🟡 | superset ✅; `DataShortcuts` fully-qualified path covered ✅ (probe A2/A3/A4/A5 all DENIED via namespace + identifier); **but** name-match is `.`-only — `?.` bypasses (High, below); `ExportTo` rationale inaccurate + name-match collateral (Low) |
| g | Tests pin what matters | ✅ | pipe==generic branch, imports set, strict superset, Shape keeps both `autocad`+`civil3d`, `Civil3dInfo` omitted-when-null, 11 fields all asserted; GUID-named pipe = same pattern as existing tests, not flaky |
| h | No plan/finding codes in comments/names | ✅ | `grep -Ei 'phase 0|F[0-9]|ADR-0|§'` new+changed files = none |

The diff itself is exemplary: precise, additive, pattern-consistent, fully covered. The findings below are (1) a **pre-existing base-walker hole** the new Civil guard inherits and relies on, and (2) **incompleteness in the new file-path deny-list** — neither is a wrong line in the diff, but both bear on whether the Civil guard delivers what its doc comment promises.

## Findings

### High

**H1 — `?.` (null-conditional) fully bypasses every member denial, including the Civil rebuild/transaction denials this profile exists for.** `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs:114` (`VisitMemberAccessExpression`) only sees `a.b`; `a?.b` parses as `ConditionalAccessExpression`+`MemberBindingExpression`, for which there is no override, so the member name is never checked.
Probe (verbatim), Civil3D profile:
```
profile .Rebuild (Civil)     plain=1 conditional=0  !! BYPASS via ?.
profile .RebuildAll (Civil)  plain=1 conditional=0  !! BYPASS via ?.
profile .ExportToDEM (Civil) plain=1 conditional=0  !! BYPASS via ?.
onIdentifier tr.Commit       plain=1 conditional=0  !! BYPASS via ?.
profile .SendStringToExecute plain=1 conditional=0  !! BYPASS via ?.
```
So `corridor?.Rebuild()`, `db.TransactionManager?.StartTransaction()`, `tr?.Commit()`, `surface?.ExportToDEM(...)` all pass the guard. Two reasons this matters more than the usual "not a sandbox" caveat: (i) null-conditional is *natural* AI/codegen style on objects pulled from a collection (`GetObject(...) as Corridor` → `corridor?.Rebuild()`), i.e. an **accidental** trigger, not a reflection trick; (ii) it re-opens the exact `StartTransaction` path the AutoCAD/Civil guard exists to block (a forgotten Transaction finalises on the GC thread and takes acad.exe down — the AutoCAD ADR's documented crash).
**Scope/blocker:** pre-existing in the shared base walker; affects **all 5 hosts** (probe shows the same bypass for Revit/AutoCAD/Navis/ETABS on their own denied members); the phase-0 table correctly lists `ScriptGuard` as unchanged, so this is **not a regression from this diff and not a phase-0 blocker**. But the Civil guard's whole value proposition (block long non-cancellable rebuilds) is undermined by it, so it should be a tracked fast-follow **before the Civil host reaches users in a later phase**.
**Fix (base walker, benefits every host):** add
```csharp
public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
{
    var member = node.Name.Identifier.ValueText;
    if (DeniedMembers.Contains(member) || profile.DeniedMembers.Contains(member))
        Report(node.Name, $".{member} is not allowed in {_host} scripts.");
    base.VisitMemberBindingExpression(node);
}
```
(receiver-scoped `DeniedMembersOnIdentifier` can't be resolved through `?.` since the receiver sits in the enclosing `ConditionalAccessExpression`; a full fix walks `node.Parent` up to the `ConditionalAccessExpression.Expression`). Pin with a `?.` row per profile in the shared guard tests.

### Medium

**M2 — the file-path deny-list is name-enumerated and misses verified siblings that write/read arbitrary files, bypassing the base `System.IO` denial.** `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:118-119`. The category comment says these members "would bypass the base list's `System.IO` denial" — correct, and exactly why the gap is real: these Civil members reach the filesystem through the native wrapper, *not* through `System.IO`, so nothing else catches them. Reflection sweep of the installed `AeccDbMgd 13.8` found, **not denied**:
```
CogoPointCollection.ExportPoints(String pointFileFullName, PointFileFormat, …)  [static]  → writes a points file to any path
GridSurface.CreateFromDEM(String DEMFileName, ObjectId styleId)                 [static]  → reads any file (sibling of denied TinSurface.CreateFrom*)
TinSurface.CreateSolidsAtDepthToFile(Double, String, UInt16, String& fileName)            → sibling of the DENIED CreateSolidsAtFixedElevationToFile
TinSurface.CreateSolidsAtSurfaceToFile(ObjectId, String, UInt16, String& fileName)        → same shape, also missed
Baseline.Export/ImportTransitions(String csvFileName)                                      → CSV file I/O
```
`CogoPointCollection.ExportPoints` is the sharpest: **static, by-value `String pointFileFullName` input path, and CogoPoints is a first-class MVP object** (`Civil3dInfo.CogoPointCount`, CogoPoints seeds) — a points seed could write `C:\...\anything`. The `CreateSolidsAt*ToFile` pair proves the list is internally inconsistent: it denies one of three identical `String& fileName` siblings.
**Root cause:** ADR-04's rule "only member names in the addendum §6–7/§11" — but the addendum is a *summary*, not an exhaustive file-I/O enumeration, so a deny-list built strictly from it is incomplete (these members are absent from the addendum's tables; I verified them by reflection, not by guessing).
**Mitigations already in place (why this is Medium, not High):** opt-in OFF on every start; no MVP seed calls these; ADR-04 §4 already flags "if phase 2 opens imports, add an ETABS-style path policy." **Fix options:** (a) run one reflection sweep for `*ToFile`/`Export*File`/`Import*File`/`CreateFrom*`/`ExportPoints`/`ImportPoints` and add the hits now; or (b) prefer the more robust ETABS-style approach — a path-literal screen on write tools — over name enumeration, since name enumeration will always trail the API surface. Track explicitly as a known MVP gap in the phase-0 report if deferred.

### Low

**L3 — `ExportTo` denial rationale is inaccurate; `ExportTo`/`Rebuild` name-match has collateral.** `GuardProfile.cs:119` groups `ExportTo` under "members that take a file path", but both `StyleBase.ExportTo` overloads take `Database destinationDatabase` (an in-memory database), **not** a path — it's a cross-drawing style copy, not a file writer. It's the only `ExportTo` in the swept AutoCAD/Civil surface, so collateral is nil today, but a future "copy styles between drawings" seed would be blocked by a rationale that doesn't apply. Separately, the bare-name `Rebuild` also blocks legit AutoCAD geometry ops unrelated to Civil corridors: `acdbmgd:Spline.Rebuild(degree, numCtrlPts)` and `NurbSurface.Rebuild(...)`. Acceptable trade (the safety win on `Corridor`/`Surface.Rebuild` dominates), but worth a one-line note in the comment so the breadth is intentional, and move `ExportTo` out of the "file path" sentence.

**L4 (informational / red-team) — the guard's own static deny-lists are reachable and mutable from a script.** `HPRebar.McpBridge.Core.Scripting` is in `Civil3dImports`, `GuardProfile` lives there, and the compiler references `typeof(ScriptArgs).Assembly` (the same assembly), so a script can name `GuardProfile.Civil3d.DeniedMembers`; the property is typed `IReadOnlySet`/`IReadOnlyCollection` but backed by a live `HashSet<string>` (constructor stores it directly), so `((HashSet<string>)GuardProfile.Civil3d.DeniedMembers).Clear();` compiles, passes the guard (neither `GuardProfile` nor `Clear` is denied), and disables the Civil denials for the rest of the bridge session. Probe: `F1/F3 → ALLOWED`. This is a **deliberate-attack** vector (not accidental) and squarely inside the documented "This is NOT a sandbox" premise + opt-in + audit, so it's accepted for the MVP and **pre-existing for all hosts**, not a phase-0 change. If ever hardened: expose the profile sets as `ImmutableHashSet`/frozen copies, or keep `GuardProfile` out of `ScriptImports`.

## Red-team escape-hatch summary (state outside the drawing / filesystem)

| Vector | Reaches | Covered by phase-0 list? | Severity |
|---|---|---|---|
| `corridor?.Rebuild()` / `tr?.Commit()` / `?.ExportToDEM()` | rebuild + own-transaction (acad.exe crash) + file write | ❌ `?.` bypasses (H1, pre-existing base walker) | High |
| `CogoPointCollection.ExportPoints(path,…)` | writes a coord file to any path | ❌ not in list (M2) | Medium |
| `GridSurface.CreateFromDEM(path,…)`, `CreateSolidsAt{Depth,Surface}ToFile` | file read / file write | ❌ not in list (M2) | Medium |
| `Baseline.Export/ImportTransitions(csv)` | CSV file I/O | ❌ not in list (M2) | Low–Med |
| `DataShortcuts.*`, `SurveyProjects`, `AeccUiMgd`, `AECC.Interop` | working/project folder, survey db, dialogs, COM | ✅ identifier + namespace (probe A1–A8, E1–E3 all DENIED) | — |
| `GuardProfile.Civil3d.DeniedMembers` mutation | disables the guard for the session | ❌ (L4, accepted "not a sandbox") | Low |

## Positives

- `git diff --numstat` = 6 files, **0 deletions** — literally additive, exactly the phase-0 contract.
- 4-host `tools/list` **byte-identical** (self-verified with `cmp`), Core.dll SHA changed → recompiled without behaviour drift.
- `Civil3d` guard/analyzer built by `Autocad.*.Concat(...)` — structural superset, no literal drift, `DeniedMembersOnIdentifier` shared by reference (probe: `ReferenceEquals True`), pinned by `..._is_a_strict_superset_of_the_autocad_profile`.
- `Civil3dInfo` = 11 fields per ADR-03 §4; `IsCivilDocument=false` path preserved; round-trips camelCase and drops when null; `Shape` keeps both `autocad` and `civil3d` blocks and strips `revitVersion`/`isFamily` — all asserted.
- Data-shortcut/survey/dialog/COM coverage is genuinely robust — fully-qualified, `global::`, `using`-alias, `using static`, `nameof`, `typeof` all caught (probe A2–A7).
- Denied member names all verified present in the installed API (Rebuild/RebuildAll/RebuildSnapshot, all 11 DataShortcut/survey members, ExportToDEM/CreateFrom*/ExportTo/CreateSolidsAtFixedElevationToFile) — no invented names.
- No plan/finding codes in code or comments (repo rule respected).

## Score

**8 / 10.** The diff is a clean, additive, well-tested, byte-identical-gated contract addition that follows the Etabs/Navis precedent precisely — nothing in it is wrong. Two points off: the new file-path deny-list is demonstrably incomplete for its own stated category (name-enumeration missed verified `ExportPoints`/`CreateFromDEM`/`CreateSolidsAt{Depth,Surface}ToFile` siblings — M2), and the `ExportTo` rationale is inaccurate (L3). The `?.` bypass (H1) is the single most important thing for the plan owner to know but is a pre-existing base-walker gap explicitly out of the phase-0 change table, so it doesn't lower the phase-0 score — it's a fast-follow for the base engine that every host benefits from.

**Verdict:** Phase-0 diff is landable as-is. Before the Civil host ships to users in a later phase, track two follow-ups: (1) close the `?.` member-binding hole in the shared walker; (2) either complete the file-path deny-list from a reflection sweep or move to an ETABS-style path-literal policy on write tools (ADR-04 §4 already anticipates this).
