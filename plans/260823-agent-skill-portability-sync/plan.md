---
title: "Agent-skill portability and bidirectional sync"
description: "Migrate repository-local agent infrastructure to Claude, Codex, and Antigravity with safe bidirectional skill synchronization."
status: pending
priority: P1
effort: 18h
branch: "N/A — environment is not a Git worktree"
tags: [agents, skills, migration, portability, sync]
created: 2026-08-23
---

# Agent-skill portability and bidirectional sync

> Approved design: [`docs/superpowers/specs/2026-08-23-agent-skill-portability-sync-design.md`](../../docs/superpowers/specs/2026-08-23-agent-skill-portability-sync-design.md). This is repository-infrastructure work, not a Revit feature: it must not change `HPRebar/**/*.cs` or `HPRebar/**/*.xaml`; therefore the Revit build gate is intentionally out of scope (design section `Test Strategy`). Nice3point is present at `HPRebar/HPRebar/HPRebar.csproj:1`, but no Revit API, WPF, manifest, or product deployment behavior changes.

## Baseline and non-negotiable boundary

- Verified corpus: 60 recursive `SKILL.md` files in each of `.claude/skills/` and `.agents/skills/`; nested document skills remain bundles, while `_shared/` remains dependency infrastructure, never a skill (design section `Skill Discovery and Logical Identity`).
- Current sources to preserve/translate: 12 Claude agents in `.claude/agents/*.md`, eight rules in `.claude/rules/*.md`, and 88 files below `.claude/hooks/`. Current Codex counterparts are `.codex/agents/*.toml` and `.codex/hooks.json`. Text references rooted at `.Codex` are invalid/non-portable path references to classify and normalize; no separate `.Codex/` tree exists in this workspace.
- Root `README.md` is absent; the product README is `HPRebar/README.md`. `AGENTS.md:9-29` contains stale `RevitAIApp` references; reconcile to the actual `HPRebar` layout or label those instructions legacy, per design section `Project Instructions`.
- No symlinks, mtime winner selection, `.shadowed/` moves, user-global writes, silent overwrite, implicit deletion propagation, or commits. Python standard library only unless a dependency is explicitly documented and separately validated.

## Target data flow and contracts

```text
.claude/skills + .agents/skills
  -> discovery + ignore policy -> logical SkillBundle/FileState graph
  -> manifest bases + provider adapter -> ChangePlan
  -> temp staging -> validation -> atomic replacements -> manifest last
  -> .agents/skills (Codex + Antigravity), .claude/skills

AGENTS/rules/agents/hooks source inventory
  -> provider mapping/adapters -> native configurations + migration map
  -> parser/contract validation -> CI read-only check
```

The thin CLI `scripts/sync-agent-skills.py` consumes `skill_sync.cli.main(argv)` and emits only deterministic text/exit status. The engine consumes `SyncConfig`, `Manifest`, `SkillBundle`, `FileState`, and `ProviderAdapter`; it produces `ScanResult`, `ChangePlan`, and `ValidationReport`. Adapter methods are `normalize(relative_path: str, content: bytes) -> bytes`, `render(source_provider: str, destination_provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> bytes`, and `validate(provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> tuple[Finding, ...]`. `check`, `status`, `scan`, and `validate` never mutate; only `apply` creates a lock, stages output, atomically replaces destinations, then atomically writes the manifest (design sections `Synchronization Commands` and `Atomicity and Concurrency`).

## Dependency graph, ownership, compatibility, rollback

`0 audit/tests -> 1 engine + fixtures -> 2 bootstrap manifest -> 3 corpus conversion -> 4 instructions/agents/rules/hooks -> 5 creator + CI -> 6 final verification`. Phase owners are exclusive by file set; later phases may consume, never edit, earlier engine/test files except their explicit listed tests.

Backward compatibility: Claude remains usable throughout; `.agents/skills/` is the shared portable tree. Before the first portable normalization, save both original divergent `SKILL.md` files and a hash/diff report. One-sided deletes only report drift; explicit future `prune`/tombstone is required. Rollback for every write phase: restore destination bundle/configuration from `.skill-sync/conflicts/bootstrap/` or the immediately prior manifest-described bases, remove only the new `.skill-sync` generated state, then rerun read-only `validate`; never create or restore a case-variant `.Codex` mirror.

| Phase | Depends on | Owner files | Principal risk (likelihood × impact) and mitigation | Measurable exit |
|---|---|---|---|---|
| 0 | none | audit/tests only | medium × high: evidence overwritten; snapshot/hashes before writes | two inventories agree on 60 logical bundles and report all differences |
| 1 | 0 | sync engine + fixture harness | high × high: corruption/races; temp staging, exclusive lock, fault tests | A–M tests pass |
| 2 | 1 | `.skill-sync/**`, CLI integration | high × high: 29 divergences lost; abort before write until every snapshot validates | bootstrap report + 29 paired originals exist |
| 3 | 2 | skill corpus/adapters | medium × high: invalid portability conversion; adapter validation and idempotency check | 60 pairs valid, no ignored payload copied |
| 4 | 3 | AGENTS/rules/agents/hooks/docs map | high × high: native-provider behavior fabricated; explicit unsupported mapping + native parser checks | 12 agent mappings, 8 rule mappings, hook mapping report |
| 5 | 4 | skill-creator, CI, docs | medium × medium: drift returns; completion gate + non-mutating CI | CI commands return 0 on clean corpus |
| 6 | 5 | reports only | medium × high: silent loss/secrets; pre/post inventory, hashes, secret scan | report records zero silent loss and zero prohibited files |

## Implementation checklist

### Phase 0 — freeze evidence and make failures reproducible

- [ ] **Test first.** Create `tests/skill-sync/test_discovery.py`, `test_manifest.py`, `test_adapters.py`, `test_apply.py`, `test_validation.py`, `test_cli.py`, `fixtures/README.md`, and `fixtures/A-claude-create` through `fixtures/M-idempotent-apply-check`. Each fixture contains isolated `claude/`, `portable/`, `expected/`, and `config.json` inputs; no test points at live `.claude/` or `.agents/`.
- [ ] Define fixtures exactly: A Claude new; B portable new; C Claude edit; D portable edit; E semantically equal dual edit; F divergent dual edit; G concurrent apply; H interrupted write; I nested discovery; J `_shared` dependency; K ignored `.venv`; L one-sided deletion; M `apply, apply, check` idempotency.
- [ ] Create `docs/agent-skill-migration-audit.md` from fresh discovery: declared names, relative paths, nesting, shared dependencies, paired hashes, 29 differences, 12 agent inputs, eight rule inputs, hook inventory, invalid references rooted at `.Codex`, missing root README, and unavailable Git evidence. Cite observations vs inferences.
- [ ] Command: `python -m unittest discover -s tests/skill-sync -p "test_*.py"`; expected initial failure because the engine does not yet exist. Do not weaken assertions to make this pass.

### Phase 1 — implement the safe, focused sync engine

- [ ] Create `scripts/skill_sync/__init__.py`, `models.py`, `paths.py`, `ignore_policy.py`, `frontmatter.py`, `discovery.py`, `manifest_store.py`, `change_detection.py`, `atomic_io.py`, `locking.py`, `validation.py`, `reporting.py`, `adapters/__init__.py`, `adapters/claude.py`, `adapters/codex.py`, `adapters/antigravity.py`, and `cli.py`; create only the thin executable `scripts/sync-agent-skills.py`. Keep each module single-purpose; split before 200 logical lines.
- [ ] Implement recursive bundle discovery, normalized declared-name identity, duplicate-name rejection, `_shared` dependency graph, complete ignore policy, local-link/reference checks, hash computation, and deterministic JSON serialization. Make configuration root-relative; reject absolute machine paths outside deliberate adapter documentation.
- [ ] Implement base-hash change matrix: neither/no-op; one-side/render to other; both/semantic-equal refresh bases; both/divergent write conflict artifacts but leave originals untouched; missing side/deletion report-only. New same-name conflicts retain both sources.
- [ ] Implement lock metadata `{pid, host, started_at, tool_version}` via exclusive create, evidence-based stale recovery, hook reentrancy flag, same-filesystem staging, validation before replacement, destination replacement before manifest-last, and cleanup/reporting after interruption.
- [ ] Run the Phase 0 command until A–M pass. Expected: conflict and interruption tests verify originals/manifest survive; concurrent test has exactly one winner; `check` leaves a before/after hash inventory identical.

### Phase 2 — configure bootstrap and preserve portable evidence

- [ ] Create `.skill-sync/config.json`, `.skill-sync/manifest.json`, `.skill-sync/adapters/claude/skill-markdown-v1.json`, `.skill-sync/adapters/codex/skill-markdown-v1.json`, `.skill-sync/adapters/antigravity/skill-markdown-v1.json`, and `.skill-sync/.gitignore` for locks, temp files, and transient reports only. Manifest schema is v1 and contains per-file Claude, portable, and semantic SHA-256 bases plus adapter ID (design section `Adapter-Aware Manifest`).
- [ ] Add bootstrap mode: before any portable write, create `.skill-sync/reports/bootstrap-portable-differences.md` and, for every detected differing logical ID, `.skill-sync/conflicts/bootstrap/<logical-id>/portable.SKILL.md` plus `claude.SKILL.md`. Verify snapshot file hashes against the pre-write audit; abort on count/path/hash mismatch.
- [ ] Test first with fixture F plus a corpus-scale temporary copy: a deliberately incomplete bootstrap set must fail with zero portable writes; complete evidence permits Claude-seed conversion and first synchronized bases. Add `test_bootstrap.py`.
- [ ] Commands: `python scripts/sync-agent-skills.py scan`, `status`, then `apply`; expected: scan/status read-only, apply reports evidence creation then manifest initialization. Re-run `apply`; expected zero meaningful changes.

### Phase 3 — migrate the complete portable skill corpus

- [ ] Modify every discovered bundle rooted at `.claude/skills/**/SKILL.md` and `.agents/skills/**/SKILL.md` (60 paired logical skills, including `document-skills/docx`, `pdf`, `pptx`, `xlsx`) only through the engine. Preserve complete eligible bundle resources byte-for-byte; do not register containers or `_shared` as skills.
- [ ] Add adapter rules to retain valid `name`, fold meaningful `when_to_use`/keywords into portable `description`, preserve common metadata, externalize unsupported directives under `.skill-sync/adapters/<provider>/`, and make provider tool/path rewrites semantic. Specifically repair invalid `.Codex/skills` references, fabricated `docs.Codex.com` URLs, Claude-only tool contracts, and hard-coded runtime machine paths without invented replacements.
- [ ] Verify portable descriptions preserve routing for `revit-addin`, `revit-wpf-mvvm`, `revit-xaml-styles`, `revit-debug`, `revit-test`, and workflow skills `scout`, `plan`, `cook`, `fix`, `test`, `code-review`, `ship`, `journal`, `debug`, `brainstorm` (design section `Validation`).
- [ ] Commands: `python scripts/sync-agent-skills.py apply`, `validate`, `check`; expected: 60 valid paired bundles, zero copied ignore matches, no conflict, read-only commands leave hash inventory unchanged. Capture failures as conflict artifacts, never hand-edit away their evidence.

### Phase 4 — translate control plane, agents, rules, and hooks

- [ ] Modify `AGENTS.md` into a concise provider-neutral map; retain the Revit architecture/routing constraints from `AGENTS.md:30-105`, correct `RevitAIApp` paths to `HPRebar` or explicitly mark legacy, and link detailed reusable rules instead of duplicating them.
- [ ] Create `.agents/rules/development-rules.md`, `documentation-management.md`, `orchestration-protocol.md`, `primary-workflow.md`, `review-audit-self-decision.md`, `skill-domain-routing.md`, `skill-workflow-routing.md`, `team-coordination-rules.md`; modify their `.claude/rules/` sources only for classified compatibility corrections. Keep behavioral rules out of Codex shell approval `.rules` files.
- [ ] Create `.agents/agents/{brainstormer,code-reviewer,code-simplifier,debugger,docs-manager,git-manager,journal-writer,planner,project-manager,researcher,tester,ui-ux-designer}.md`; modify the matching twelve `.codex/agents/*.toml`; preserve `.claude/agents/*.md` except supported corrections. Add all 12 source/destination contracts, tool substitutions, unsupported capabilities, input/output, isolation, and `DONE`, `DONE_WITH_CONCERNS`, `BLOCKED`, `NEEDS_CONTEXT` semantics to `docs/agent-migration-map.md`.
- [ ] Create `.agents/hooks.json`; modify `.codex/hooks.json`, `.claude/settings.json`, and only needed `.claude/hooks/**` / `.codex/hooks/**` adapters. Translate each lifecycle event semantically from `.claude/settings.json`—not by copying command strings—using repository-root resolution, thin input/output adapters, reentrancy guard, debounce, and explicit CLI correctness. Record every unsupported event/payload in the map. Normalize invalid references rooted at `.Codex` to their actual native destinations; do not create a case-variant mirror.
- [ ] Add `tests/skill-sync/test_platform_configs.py` and fixtures for malformed JSON, YAML, TOML, invalid native tool names, recursive hooks, and absolute paths. Expected: parsers/validators accept generated configurations and reject copied Claude contracts.

### Phase 5 — complete authoring workflow, CI, and operations documentation

- [ ] Modify `.claude/skills/skill-creator/SKILL.md` and `.agents/skills/skill-creator/SKILL.md`: Claude authors `.claude/skills/`; Codex/Antigravity authors `.agents/skills/`; each completion sequence is validate changed bundle -> `apply` -> `validate` -> report both destinations. Remove fabricated documentation links and any user-global installation instruction.
- [ ] Create `.github/workflows/skill-sync.yml` with read-only steps exactly `python scripts/sync-agent-skills.py check` then `python scripts/sync-agent-skills.py validate`; CI must not call `apply`, resolve/prune, or write generated corpus state.
- [ ] Create `docs/agent-skill-architecture.md` (components/data flow/concurrency/rollback), `docs/skill-sync.md` (commands, resolution, prerequisites), and `docs/skill-migration-report.md` (pre/post inventory, adaptations, limitations, unsupported equivalence, no-Git limitation). Update `docs/agent-skill-migration-audit.md` only with post-migration facts clearly dated as such.
- [ ] Test skill-creator gate from temp fixture: changed Claude and portable bundles each arrive at the opposite path on successful apply; divergence produces a clear conflict and non-success completion. CI workflow syntax is validated locally without executing remote CI.

### Phase 6 — independent compliance/quality review and release evidence

- [ ] **Spec/compliance review:** trace every section from `Approved Decisions` through `Acceptance Criteria` to a file, test, command, and report row; reject omissions, mtime selection, automatic deletion, user-global writes, symlinks, fabricated URLs/tools, or any `.cs`/`.xaml` diff.
- [ ] **Quality/security review:** inspect atomic replacement/lock edge cases; scan synchronized resources, manifests, reports, and configs for secrets, absolute user paths, ignored environments/caches/logs, broken links, and invalid references rooted at `.Codex`. Confirm prior valid manifest remains authoritative after injected failure.
- [ ] Commands (in order): `python -m unittest discover -s tests/skill-sync -p "test_*.py"`; `python scripts/sync-agent-skills.py apply`; `python scripts/sync-agent-skills.py apply`; `python scripts/sync-agent-skills.py check`; `python scripts/sync-agent-skills.py validate`; repository-wide inventory/hash comparison; `rg -n --hidden -g '!HPRebar/**' '(BEGIN (RSA|OPENSSH) PRIVATE KEY|api[_-]?key|secret|token|password)' .`; `rg --files HPRebar -g '*.cs' -g '*.xaml'` plus before/after file-list comparison.
- [ ] Expected: all tests pass; second apply has zero meaningful changes; check/validate are non-mutating; conflict tests preserve both originals; 60 logical skills retained; no ignored payload or secret added; product source comparison is empty. Publish final inventory/diff/secret outcomes and all manual-review exceptions in `docs/skill-migration-report.md`.

## Test matrix

| Layer | Proof |
|---|---|
| Unit | frontmatter/name normalization, ignore rules, SHA bases, adapters, manifest serialization, links, lock/stale lock, atomic recovery |
| Integration | fixtures A–M, bootstrap incomplete/complete, both-direction conversion, platform config parsing, skill-creator gate |
| Corpus/e2e | full copied corpus `apply -> apply -> check`; pre/post inventory and hash evidence; conflict/no-delete/read-only behavior |
| Regression/safety | no `HPRebar` `.cs`/`.xaml` changes, no user-global paths, no secrets/ignored artifacts, no invalid runtime references rooted at `.Codex` |

## Status protocol

Implementer reports each phase as `DONE` only with commands and observed counts; `DONE_WITH_CONCERNS` only with documented non-blocking adapter limits; `BLOCKED` for a failed required command or unsafe conflict; `NEEDS_CONTEXT` only for an explicit manual decision, such as resolving independently authored same-name skills. Never mark final completion until Phase 6 evidence is attached. No commits in this plan.

## Unresolved questions

None. Invalid references rooted at `.Codex` are a verified path-portability defect with a defined normalization strategy, not an open architecture decision.
