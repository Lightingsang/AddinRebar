# Agent Skill Portability and Bidirectional Sync Design

## Overview

This design migrates the repository's Claude Code skill and agent infrastructure into a portable, repository-local architecture for Claude Code, OpenAI Codex, and Google Antigravity. It preserves the existing Claude implementation, uses `.agents/skills/` as the shared portable skill tree for Codex and Antigravity, and adds deterministic bidirectional synchronization between the two physical skill representations.

The design is intentionally adapter-aware. Claude and portable `SKILL.md` files may differ when platform semantics differ, so synchronization tracks a base hash for each representation instead of assuming both files must remain byte-identical.

## Approved Decisions

- Treat `.claude/skills/` as the initial migration seed.
- Preserve all current `.agents/skills/` divergences as audit evidence before normalization.
- Use `.agents/skills/` as the one physical portable tree consumed by both Codex and Antigravity.
- Keep `.claude/skills/` for Claude Code compatibility.
- Use provider-specific adapters for metadata, tool names, paths, agents, rules, and hooks.
- Never use modification time to select a winner.
- Never silently overwrite divergent edits.
- Never propagate deletion by default.
- Keep all implementation repository-local; do not modify user-global directories.
- Do not use symlinks or the historical `.shadowed/` move/restore pattern.
- Do not modify product `.cs` or `.xaml` files for this migration.

## Current Repository Baseline

The audit established this starting state:

- `.claude/skills/`: 60 recursively discovered `SKILL.md` files.
- `.agents/skills/`: the same 60 logical skills.
- Four nested skills: `document-skills/docx`, `document-skills/pdf`, `document-skills/pptx`, and `document-skills/xlsx`.
- Shared non-skill infrastructure: `_shared/` and `common/`.
- 31 paired `SKILL.md` files are byte-identical.
- 29 paired `SKILL.md` files differ, primarily because of mechanical provider-name/path replacement.
- `.codex/agents/`: 12 TOML agent definitions.
- `.codex/hooks/`: an 88-file copy of the Claude hook implementation.
- `.skill-sync/`: absent.
- `scripts/sync-agent-skills.py`: absent.
- `.agents/rules/`, `.agents/agents/`, and `.agents/hooks.json`: absent.
- Root `README.md`: absent; `HPRebar/README.md` is the available product README.
- The current folder is not a Git worktree, so Git history/status evidence cannot be produced in this environment.

Known migration defects include invalid `.Codex/skills` references, fabricated `docs.Codex.com` URLs, Claude-only tool contracts inside Codex files, hard-coded machine paths, missing portable runtime dependency resolution, and unregistered or mechanically copied hooks.

## Platform Contracts

### OpenAI Codex

Codex natively discovers repository skills under `.agents/skills/`. Each skill requires a `SKILL.md` with `name` and `description`; scripts, references, assets, and optional `agents/openai.yaml` are supported.

Codex project instructions use `AGENTS.md`. Project-local custom configuration uses `.codex/config.toml`, custom agents use `.codex/agents/*.toml`, and hooks use `.codex/hooks.json` or inline config. Behavioral instructions must not be encoded as shell approval rules.

References:

- <https://learn.chatgpt.com/docs/build-skills>
- <https://learn.chatgpt.com/docs/agent-configuration/agents-md>
- <https://learn.chatgpt.com/docs/agent-configuration/subagents>
- <https://learn.chatgpt.com/docs/hooks>

### Google Antigravity

Antigravity natively discovers workspace skills under `.agents/skills/`, workspace rules under `.agents/rules/`, custom agents under `.agents/agents/`, and hooks in `.agents/hooks.json`.

Antigravity agent definitions are Markdown files with YAML frontmatter and native tool names. Antigravity hook tool matchers and lifecycle payloads differ from Codex and Claude, so hook JSON must be generated semantically rather than copied.

References:

- <https://antigravity.google/docs/skills/>
- <https://antigravity.google/docs/rules-workflows/>
- <https://antigravity.google/docs/subagents/>
- <https://antigravity.google/docs/ide/hooks/>

### Claude Code

The existing `.claude/` implementation remains intact except for deliberate compatibility corrections and skill-creator integration. Claude-specific metadata and behavior are retained in the Claude representation or Claude adapter.

## Target Layout

```text
AGENTS.md

.claude/
├── skills/
├── agents/
├── rules/
├── hooks/
└── settings.json

.agents/
├── skills/
├── rules/
├── agents/
└── hooks.json

.codex/
├── config.toml
├── agents/
├── hooks/
└── hooks.json

.skill-sync/
├── config.json
├── manifest.json
├── adapters/
│   ├── claude/
│   ├── codex/
│   └── antigravity/
├── conflicts/
└── reports/

scripts/
└── sync-agent-skills.py

tests/
└── skill-sync/
```

`conflicts/` and `reports/` contain committed explanatory artifacts only when useful. Transient lock and temporary files are ignored.

## Skill Discovery and Logical Identity

Discovery walks each skill tree recursively and registers every directory containing `SKILL.md`. A container such as `document-skills/` is not a skill unless it contains its own `SKILL.md`.

Each discovered skill records:

- logical skill ID;
- declared frontmatter name;
- relative bundle root;
- Claude bundle path;
- portable bundle path;
- nested/container relationship;
- shared dependencies;
- adapter requirements;
- migration status.

Logical identity defaults to normalized declared `name`. Folder aliases and platform invocation aliases are recorded explicitly, especially for `bs:*` skills. Duplicate names are validation errors.

`_shared/` remains shared runtime infrastructure and is never registered as a skill. Dependencies on `_shared/` are recorded in the dependency graph and validated.

## Portable Skill Conversion

### `SKILL.md`

The portable representation guarantees this minimum frontmatter:

```yaml
---
name: skill-name
description: Clear capability and trigger description.
---
```

Conversion rules:

1. Preserve `name` when it is valid and intentional.
2. Merge meaningful `when_to_use`, keywords, and trigger behavior into `description` when those fields are not portable.
3. Preserve fields supported safely by both portable consumers.
4. Store unsupported provider metadata in `.skill-sync/adapters/<provider>/`.
5. Rewrite provider-specific tool and path instructions semantically, not by global string replacement.
6. Preserve full useful Markdown instructions.
7. Never create fabricated documentation URLs.
8. Preserve `bs:*` logical names and record per-platform invocation differences.

### Bundle Resources

The complete bundle is migrated, including applicable:

- scripts;
- references;
- assets;
- agents;
- examples and resources;
- templates;
- tests and fixtures;
- README and license files;
- files referenced by `SKILL.md`.

Resource files are copied byte-for-byte unless a documented path adapter is required. Relative links are validated after relocation.

### Ignore Policy

The default ignore policy includes:

```text
.venv/
venv/
.env
*.secret
__pycache__/
.pytest_cache/
.mypy_cache/
.ruff_cache/
node_modules/
dist/
build/
coverage/
logs/
*.log
.DS_Store
Thumbs.db
.shadowed/
.skill-sync lock/temp files
```

Useful templates such as `.env.example` remain eligible for synchronization.

## Adapter-Aware Manifest

Because Claude and portable representations may be intentionally different, the manifest stores separate successful bases:

```json
{
  "schema_version": 1,
  "skills": {
    "logical-skill-id": {
      "claude_path": ".claude/skills/example",
      "portable_path": ".agents/skills/example",
      "files": {
        "SKILL.md": {
          "claude_base_sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "portable_base_sha256": "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb",
          "semantic_base_sha256": "3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d",
          "adapter": "skill-markdown-v1"
        }
      },
      "status": "synchronized"
    }
  }
}
```

The three hashes above are valid-format illustrative values, not hashes from the repository audit.

The manifest stores only deterministic metadata and hashes. It contains no secrets or machine-local absolute paths.

For exact-copy resources, Claude and portable base hashes normally match. For adapted files such as `SKILL.md`, each side has its own base hash plus a semantic normalized hash used to recognize equivalent outcomes.

### Bootstrap Preservation

Before normalizing any of the 29 currently divergent portable `SKILL.md` files, initialization writes:

- a deterministic path/hash/diff report under `.skill-sync/reports/bootstrap-portable-differences.md`;
- an untouched copy of each divergent portable `SKILL.md` under `.skill-sync/conflicts/bootstrap/<logical-skill-id>/portable.SKILL.md`;
- the matching untouched Claude source beside it as `claude.SKILL.md`.

These bootstrap snapshots are evidence, not active conflict state. After the snapshots and report validate successfully, the approved Claude seed may be converted into the portable representation and registered as the first synchronized base. Initialization aborts before any portable write if the evidence set is incomplete.

## Change Detection and Conflict Rules

For each paired file:

- `C_changed = current_C != claude_base`
- `P_changed = current_P != portable_base`

Outcomes:

1. Neither changed: no operation.
2. Claude only changed: validate Claude input, run Claude-to-portable adapter, stage portable output.
3. Portable only changed: validate portable input, run portable-to-Claude adapter, stage Claude output.
4. Both changed and normalize to identical semantics: accept both and refresh bases.
5. Both changed and remain semantically divergent: create a conflict; do not modify either original.

For new skills:

- Claude-only valid skill propagates to portable.
- Portable-only valid skill propagates to Claude.
- Independently created same-name skills with different content conflict.

For deletion:

- One-sided deletion is reported only.
- Propagation requires an explicit prune/delete command or a deliberate manifest tombstone.
- A missing file or directory alone is never proof of intended deletion.

Conflict reports identify the logical skill, both paths, relevant hashes, adapter used, and suggested resolution workflow. Copies or snapshots stored in the conflict area never replace originals.

## Synchronization Commands

The repository-local Python CLI provides:

```text
python scripts/sync-agent-skills.py scan
python scripts/sync-agent-skills.py status
python scripts/sync-agent-skills.py check
python scripts/sync-agent-skills.py apply
python scripts/sync-agent-skills.py validate
```

Optional commands may include `diff`, `resolve`, and explicit `prune`, but only when they remain deterministic and testable.

Command contracts:

- `scan`: discover and report without mutation.
- `status`: compare current state against manifest without mutation.
- `check`: CI-oriented, read-only, non-zero on drift, missing sides, invalid manifest, invalid portable metadata, missing references, or unresolved conflicts.
- `apply`: acquire lock, recalculate state, stage conversions, validate, atomically replace destinations, and update manifest last.
- `validate`: validate corpus, dependency graph, provider adapters, and platform configuration without mutation.

## Atomicity and Concurrency

`apply` uses one repository-local lock containing process ID, host identity, start time, and tool version. Lock acquisition uses exclusive creation.

Stale-lock recovery requires evidence that the owning process is absent or the lock exceeds the configured stale threshold. Recovery is reported; a live competing process is never displaced.

Apply order:

1. Acquire lock.
2. Re-scan under lock.
3. Build all converted output in same-filesystem temporary locations.
4. Validate all staged output.
5. Flush files where practical.
6. Atomically replace destination files or bundle directories.
7. Atomically replace `manifest.json` last.
8. Release lock.

If validation or staging fails, destinations and manifest remain unchanged. If interruption occurs during replacement, the previous manifest continues to describe the last complete synchronization and recovery reports any staged residue.

A reentrancy environment flag prevents hook-triggered recursion. The lock remains the authoritative cross-process guard.

## Project Instructions, Rules, Agents, and Hooks

### Project Instructions

`AGENTS.md` becomes a concise provider-neutral control map rather than a duplicate encyclopedia. It preserves Revit architecture, feature folders, namespaces, MVVM, XAML, transactions, `ExternalEvent`, logging, versioned build commands, test expectations, documentation policy, no-cheating rules, skill routing, and subagent context requirements.

Detailed reusable rules live in maintainable referenced files. Stale `RevitAIApp` paths are reconciled with the actual `HPRebar` layout or explicitly marked as legacy guidance; agents must not infer nonexistent product structure.

### Custom Agents

Each of the 12 Claude agents receives:

- a Codex TOML definition under `.codex/agents/` using native Codex configuration and tool behavior;
- an Antigravity Markdown definition under `.agents/agents/` using YAML frontmatter and native Antigravity tool names;
- a mapping entry in `docs/agent-migration-map.md`.

Purpose, trigger, scope, expected input/output, repository paths, context isolation, and DONE/DONE_WITH_CONCERNS/BLOCKED/NEEDS_CONTEXT semantics are preserved where supported. Unsupported capabilities are documented rather than simulated with nonexistent tools.

### Rules

Claude behavioral rules are classified into project instruction, domain routing, workflow routing, safety, documentation, orchestration, and provider-specific behavior.

- Codex behavioral guidance maps to `AGENTS.md` and referenced Markdown.
- Codex `.rules` files are used only for shell execution policy when needed.
- Antigravity behavioral rules map to `.agents/rules/*.md` and respect its per-file character limit.
- Portable skill routing belongs in skill descriptions and shared references when appropriate.

### Hooks

Hooks are translated semantically per platform event and payload schema. Scripts may share provider-neutral policy logic, but each platform receives a thin input/output adapter.

Hooks are convenience triggers only. They may debounce or schedule synchronization after skill writes or session completion, but correctness depends on explicit CLI commands and CI checks.

Recursive hook invocation is blocked. Expensive full scans do not run after every keystroke.

## Skill-Creator Completion Gate

Every repository-local skill creation/update workflow ends with:

1. Validate the changed skill bundle.
2. Run synchronization apply.
3. Run synchronization validation.
4. Report Claude and portable destinations.

A skill-creation task is complete only when synchronization succeeds or a clear conflict is reported. Codex and Antigravity author portable skills under `.agents/skills/`; Claude authors under `.claude/skills/`.

No workflow installs into user-global directories.

## Cross-Platform Runtime Resolution

Scripts locate the repository root from their own file location or explicit CLI configuration, not from a provider-specific current working directory.

Python runtime resolution is repository-neutral and supports Windows first. Virtual environments are never copied. Dependency requirements are documented and validated separately from skill content.

Provider-specific paths such as `~/.claude`, `~/.codex`, and `~/.gemini` are allowed only inside deliberate adapter documentation or platform configuration. Portable skill runtime instructions avoid user-specific absolute paths.

## Validation

Validation covers:

- directory and `SKILL.md` presence;
- valid YAML frontmatter;
- non-empty unique name and description;
- description includes capability and trigger;
- intentional folder/name relationship;
- referenced local files exist;
- source-relative links remain valid;
- no accidental user-specific absolute runtime paths;
- no secrets or ignored artifacts;
- referenced scripts exist;
- runtime requirements documented;
- license notices preserved;
- provider-specific directives classified;
- Revit routing coverage for `revit-addin`, `revit-wpf-mvvm`, `revit-xaml-styles`, `revit-debug`, and `revit-test`;
- workflow routing coverage for scout, plan, cook, fix, test, code-review, ship, journal, debug, and brainstorm;
- JSON, YAML, and TOML parseability;
- native agent and hook tool names.

## Test Strategy

Tests use temporary directories before any real corpus mutation.

Required fixtures:

1. Claude creates a new skill.
2. Portable creates a new skill.
3. Claude modifies a skill.
4. Portable modifies a skill.
5. Both sides make the same semantic change.
6. Both sides make divergent changes.
7. Concurrent sync attempts.
8. Interrupted write simulation.
9. Nested skill discovery.
10. Shared resource dependency.
11. Ignored `.venv` content.
12. One-sided deletion.
13. Idempotent `apply`, `apply`, `check` sequence.

Additional proof:

- `check` does not change any repository file hash.
- second `apply` produces zero meaningful changes.
- conflict tests preserve both originals.
- fault injection preserves previous valid state.
- full pre/post inventory comparison reports zero silent loss.
- platform config parsers accept generated files.
- no `.cs` or `.xaml` changes occur, so the Revit build gate is outside this migration's execution path.

## CI Contract

CI runs:

```text
python scripts/sync-agent-skills.py check
python scripts/sync-agent-skills.py validate
```

CI never runs `apply`, mutates the repository, resolves conflicts, or commits generated output.

## Documentation Deliverables

Implementation produces:

- `docs/agent-skill-migration-audit.md`;
- `docs/agent-skill-architecture.md`;
- `docs/agent-migration-map.md`;
- `docs/skill-sync.md`;
- `docs/skill-migration-report.md`.

Reports distinguish verified facts, inferences, adaptations, and unsupported equivalence. Every exception and manual-review item is listed explicitly.

## Rollout Order

1. Preserve the audit baseline and current portable divergences.
2. Implement and fixture-test discovery, manifest, adapters, locking, conflicts, and atomic writes.
3. Initialize the manifest from the approved Claude seed without destroying portable evidence.
4. Generate and validate all portable skill bundles.
5. Translate project instructions, rules, agents, and hooks.
6. Integrate skill-creator completion gates and optional debounced hooks.
7. Run full validation, inventory comparison, idempotency, concurrency, and interruption tests.
8. Write final architecture, operation, mapping, and migration reports.

## Acceptance Criteria

Completion requires:

- all existing Claude skills remain available;
- all migratable skills have portable bundles;
- Codex and Antigravity discover the shared `.agents/skills/` tree;
- nested skills and shared infrastructure are preserved;
- provider adapters document every non-equivalent behavior;
- Revit routing semantics remain intact;
- agents, rules, and hooks use current native formats;
- synchronization works in both directions;
- concurrent sessions cannot corrupt state;
- conflicts and deletions cannot cause silent loss;
- `check` is read-only;
- `apply` is idempotent;
- fixture and corpus validation pass;
- no ignored environment, cache, log, secret, or dependency tree is copied;
- final reports list every limitation;
- Git status is reported only when a Git worktree is available.

## Unresolved Questions

None. The missing root README and missing Git metadata are verified environment limitations, not open design decisions.
