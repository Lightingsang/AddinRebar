# Kata beam rebar rules — evaluate 04_quy_dinh_thep_dam.md, rewrite as HPRebar's standard

Status: done 2026-10-04 (docs only, not committed). Rules doc rewritten in place at `docs/specs/kata-beam-rebar-rules.md` (84 rules R-01…R-127) — the existing file, not a new one, per "update existing files". Contract by grill-me (user: "implement").

## Output
- `docs/specs/kata-beam-rebar-rules.md` — rule register by chapter: `R-xx`, statement + formula, controlling sheet Dam cell /
  setting, source tier, HPRebar status (✅ code + test / 🟡 partial / ❌ not done) with `file:line` + test name.
- `reports/source-evaluation.md` — every claim of the source doc: đúng / sai / không kiểm chứng / ngoài phạm vi.
- `reports/hprebar-rule-inventory.md` — what HPRebar implements, with code + tests.
- `reports/kata-decompiled-crosscheck.md` — rule values from the decompiled Kata (comparison only, no code copied).
- Old `docs/kata-beam-rebar-algorithm-and-revit-api.md` gets a "replaced by" line.

## Source priority
Kata drawing T2-DY7.dwg (measured, golden-tested) → decompiled Kata (comparison) → TCVN 5574:2018 (minimum checks) →
source doc. The source doc on Q: is read-only.

## Out of scope
Drawing rules (dims/tags), columns/footings, code changes, writing to Q:, commit.
