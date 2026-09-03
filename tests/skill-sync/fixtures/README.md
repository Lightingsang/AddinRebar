# Skill-sync black-box fixtures

Each directory is a self-contained repository root copied to a temporary directory by the tests. `claude/` and `portable/` are the two provider inputs, `expected/` holds hand-authored expected artifacts or assertions, and `config.json` fixes the provider and state paths. Fixtures deliberately never link to the repository's live `.claude/` or `.agents/` trees.

| Fixture | Contract exercised |
| --- | --- |
| A-claude-create | A Claude-only new skill is adapted to portable. |
| B-portable-create | A portable-only new skill is adapted to Claude. |
| C-claude-edit | Claude drift produces adapter-aware manifest bases. |
| D-portable-edit | Invalid portable metadata is rejected. |
| E-semantically-equal-dual-edit | Equivalent dual edits do not conflict. |
| F-divergent-dual-edit | Divergent dual edits preserve both originals. |
| G-concurrent-apply | Competing applies cannot corrupt the state. |
| H-interrupted-write | A staged failure leaves destinations untouched. |
| I-nested-discovery | Only directories with `SKILL.md` are skills. |
| J-_shared-dependency | `_shared` is a dependency, never a skill. |
| K-ignored-.venv | Virtual environments are ignored. |
| L-one-sided-deletion | A missing side is reported, not propagated. |
| M-idempotent-apply-check | `apply`, `apply`, `check` is stable and read-only. |
