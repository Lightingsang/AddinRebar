# Code review — AI clash descriptions (BIMCoordinator slice 4), 2026-10-12

Read-only review of the uncommitted slice. Rule ids: core (`HP_CLEAN_CODE_CORE.md`), `host-appendix/net48-inprocess.md` (NI*), checklist sections.
Live facts given by lead not re-litigated (FormatIntersectionDisplayString mm offsets, Element tab names, THCSLT 20 261 rows / 0 original-cell diffs / 641 groups).
Re-ran here: Python `unittest discover -s tests` → 23/23 OK. Engine 187 not re-run (needs Navisworks install; number from lead).
Scratch check over the shipped JSON (114 kinds, 18 rules HV-01..18, 0 approved, every ruleId resolves, no kind whose ruleId category pair is dead).

## Scope
| File | Change |
|---|---|
| HPNavis/HPNavis.BIMCoordinator/Reports/ClashElementFacts.cs | new, 39 l — SystemText, GridSystemFor |
| HPNavis/HPNavis.BIMCoordinator/Reports/ClashSnapshot.cs | ClashItemInfo + family/type/size/system/mark, row + gridLocation (additive, defaults) |
| HPNavis/HPNavis.BIMCoordinator/Reports/NavisClashSnapshotReader.cs | ItemInfo reads facts, GridLocation per row |
| HPNavis/HPNavis.BIMCoordinator.Tests/ClashElementFactsTests.cs | new, 3 tests |
| HPNavis/tools/bim-coordinator/clash-ai-text.py / clash-ai-groups.py | new, 150 / 141 l |
| clash-ai-knowledge.json / clash-acceptance-rules.json | new, 114 kinds / 18 rules (all approved:false) |
| clash-description-sheet.py / build-clash-report.py | AI columns X..AB, snapshots_in regex, picture embed byte-for-byte |
| tests/test_clash_ai_text.py, tests/test_build_clash_report.py | 10 new + 2 new tests |

## Acceptance criteria
| # | Criterion | Verdict | Evidence |
|---|---|---|---|
| A | "Hợp lệ theo thiết kế" only via a user-approved rule, else "Cần xác minh" | 🟡 holds today (0 approved), but approval scope leaks across disciplines (H1) and `approved` is truthiness (M1) | clash-ai-text.py:49-55, :130-132 |
| B | Confidence lowered by missing facts / no picture | 🟡 rules correct as coded; demoted rows keep "Cao" (M2); KB "Cao" without drawings (M3) | clash-ai-text.py:107-113 |
| C | Grouping key stable | ✅ per snapshot (test name + category + family + band, sha1[:10]); no carry-over of hand/pilot judgements to the next snapshot (M5); separator ambiguity (L2) | clash-ai-text.py:33-39 |
| D | Facts never guessed, missing ones named | 🟡 per-row facts all from the snapshot; grid-system match can pick another building's grid on some ISO names (M4); file short name is a positional guess (L3) | ClashElementFacts.cs:12,31; clash-ai-text.py:78-81 |
| E | Knowledge refresh by source tag | 🟡 corrected KB reaches groups; removed KB kinds leave stale judgements, hand edits that keep the tag are overwritten, no test (M6) | clash-ai-groups.py:267-285 |
| F | File naming / snapshot lookup collision | ✅ `SNAPSHOT_NAME` rejects `-ai.json` / `-ai-groups.json`, phase compared exactly (also fixes old `startswith("clash-LOD350-")` matching `350-400`) | build-clash-report.py:69-75; test_build_clash_report.py:110 |
| G | Excel layout: originals untouched, hidden GUID, 4 AI columns | ✅ X hidden GUID, Y..AB after W; no automated test of the AI columns (M7); misleading comment (L4) | clash-description-sheet.py:30-33, :125-131, :154-160 |
| H | Engine robustness | ⚠️ `ToDictionary` on grid-system names (H2) | NavisClashSnapshotReader.cs:26 |

## Findings

### High
**H1 — clash-acceptance-rules gate is category-only; one approval covers other disciplines' kinds.** `rule_allows` (clash-ai-text.py:49-55) checks approval + category pair + depth, never `row["pair"]`. The KB reuses rules across disciplines: `STR-STR|Walls|Floors` → HV-03 (clash-ai-knowledge.json:359), whose title/rationale is "Tường xây **kiến trúc** đặt trên/xuyên sàn" (clash-acceptance-rules.json:32); HV-14 serves both `ARC-STR|Floors|Structural Framing` and `STR-STR|Floors|Structural Framing` (:341); HV-13 likewise. A user who approves HV-03 for ARC masonry silently turns every STR concrete wall × slab overlap ≤ 300 mm into "Hợp lệ theo thiết kế". That widens a human approval beyond what was read — the exact thing the gate exists to stop. Rule: K1 (the acceptance scope lives in two places — rule title vs KB key), T4 (shipped-KB test does not check scope).
Fix: add `"pairs": ["ARC-STR"]` (discipline pairs) to every rule, require `row["pair"] in rule["pairs"]` in `rule_allows`; split HV-03/HV-13/HV-14 into ARC and STR rules with their own titles. Extend `test_every_kind_in_the_shipped_knowledge_base_is_well_formed` (test_clash_ai_text.py:87) to assert, for every VALID kind and VALID band, that the rule's `pairs` contains the key's pair and one `categoryPairs` entry equals the key's categories (sorted).

**H2 — `doc.Grids.Systems.ToDictionary(s => s.DisplayName)` throws on duplicate grid-system names.** NavisClashSnapshotReader.cs:26. Grid systems are named after the appended file; the same file appended twice (two NWFs, a central and a local of the same name in different folders) gives duplicates → `ArgumentException` → the heavy export seed fails before any snapshot is written (Read runs before the HTML writer, CoordinatorTools.Reports.cs:29). Recurring defect class in this engine (slice-1 review). Also mis-signals: a host-data fault surfaces as a caller error excluded from stability (NI7).
Fix: keep a list (`doc.Grids.Systems.ToList()`), resolve by name in `GridSystemFor`, and treat an ambiguous match as null (or first, logged). Add a test with two equal names.

### Medium
**M1 — approval is truthiness, with no provenance.** clash-ai-text.py:51 `rule.get("approved")` accepts `"false"`/`"no"` strings and `approved:true` with `approvedBy: null`. The SKILL says Claude never flips `approved`, but nothing checks it. Fix: `rule.get("approved") is True and str(rule.get("approvedBy") or "").strip()` (optionally reject `approvedBy` containing "Claude"/"AI"); `float(rule.get("maxPenetrationMm") or 0)` so `null` does not raise. Test both.

**M2 — a demoted row keeps the original confidence and the "valid" wording.** columns_for (clash-ai-text.py:130-141): VALID → VERIFY keeps `confidence` from the judgement ("Cao" for HV-01/02/05..08 kinds) and the description still says "đây là chồng lấn do dựng model, không thay đổi giải pháp…", action "Không phải sửa thiết kế…" + an appended note. With 0 rules approved this is ~18 k rows reading "Cần xác minh · Cao · no fix needed". Fix: on demotion cap confidence at "Trung bình", prefix the assessment with "Nhận định sơ bộ (chờ duyệt quy tắc <id>):", and put the BIM-check sentence first in the action. Test: demoted row → confidence ≤ Trung bình.

**M3 — kind-level judgements state causes as facts and rate them "Cao".** KB is keyed by `pair|cat1|cat2` (clash-ai-text.py:61-75) while groups are family-level; the KB author note says "chưa có bản vẽ thiết kế" (clash-ai-knowledge.json:3), yet 28 kinds are "Cao" and assessments assert causes, e.g. :152 "Tường xây … được dựng tới cao độ sàn tầng trên nên xuyên qua dầm" for every Walls × Framing up to 600 mm (a beam running along the wall reads the same). Fix: in `judgement_from_knowledge` cap confidence at "Trung bình" for `source: knowledge:*` (only group-level reviewed judgements may be "Cao"), and phrase KB assessments as "thường/nhiều khả năng …". User decision on wording if they disagree — not reversing anything they confirmed.

**M4 — `ClashElementFacts.FileBase` cuts at the first `-dddd`.** ClashElementFacts.cs:12 `^(.*?-\d{4})` is lazy: an ISO name whose originator/volume field is four digits (`PRJ-HPC-2501-ZZ-M3-AA-0001_u`) collapses every file to `PRJ-HPC-2501` → `FirstOrDefault` (:31) returns another building's grid, quoted in the description as fact — the bug this slice removes. Fix: strip only the local-copy suffix after the last field: `^(?<base>.*-\d{4})(?:_[^-]*)?$` (greedy) and require exactly one matching system (else null). Add `[InlineData]` with a numeric volume field and with two matches. K1: ISO-name parsing now exists three times (this regex, `short_file` in Python, the `*-<role>-????*` search-set wildcard).

**M5 — hand/pilot judgements do not carry to the next snapshot.** `<snapshot>-ai.json` is per snapshot (clash-ai-groups.py:271); next week's export starts from the KB only, so the pilot's reviewed judgements and hand edits are lost unless copied by hand. Group ids are stable across snapshots (same test name, families, band), so carry-over is cheap. Fix: `--from-previous` (or default) seeding non-`knowledge:` judgements from the previous snapshot's `-ai.json` (reuse `find_previous`), reported as "N carried".

**M6 — knowledge refresh edge cases, untested.** clash-ai-groups.py:274-282: (a) a KB kind renamed/removed leaves the old `knowledge:` judgement in place (`judgement is None → continue`) — stale text survives; (b) a hand edit that leaves `"source": "knowledge:…"` is silently overwritten (docstring promises hand edits are kept); (c) `added` counts rewrites; (d) `data["knowledge"]` records the author string, not a KB revision. Fix: on `None` delete an existing `knowledge:` entry (and list it); stamp `source: "knowledge:<key>@<sha1 of KB entry>"` and overwrite only when the stored entry still equals what that KB revision produced (else treat as hand-edited and keep); store the KB file sha256. Tests: refresh replaces knowledge entries, keeps non-knowledge entries, drops removed kinds (T4 — currently 0 tests of `write_judgements`).

**M7 — no automated test of the AI columns in the workbook.** test_build_clash_report.py covers lookup and pictures only; "0 original-cell differences" was proven once live. Fix: one test writing `<snapshot>-ai.json` for one group, building, and asserting X hidden + GUID, Y..AB headers/values, the row without judgement blank, and A..W identical to a build without `-ai.json`. Also warn in `build` when `judgements` is non-empty but `described < listed` (stale group ids after a key change would otherwise drop AI columns silently) — build-clash-report.py:253-254, :286.

**M8 — picture embedding change is a separate concern in this slice and multiplies workbook size.** clash-description-sheet.py `place_picture` now embeds the 1024 px original (plan notes 155 MB vs 5.4 MB before). User-facing choice (sharp zoom) — not reversing it; flagging P6 (one concern per commit) and the size risk: commit it separately and consider an opt-in `--full-pictures` or a 2× display resample (≈680 px) as default.

### Low
- **L1** clash-ai-text.py:23 `MEP_CATEGORIES` substring match vs comment: "Plumbing" also hits Plumbing Fixtures and "Sprinkler" Sprinklers while the comment says fixtures/devices are exempt; "Pipe" hits Pipe Insulations. Use an exact category set.
- **L2** clash-ai-text.py:39 key joined with `|` while test names contain `|` — use `json.dumps(key)` / `"\x1f"` (collision theoretical; changing it re-keys existing `-ai.json`, do it together with M5).
- **L3** clash-ai-text.py:78-81 `short_file` takes fields 2 and 5 by position for any name with ≥ 7 hyphen parts — a non-ISO name yields a plausible-looking wrong code. Match `-(AA|ES|E[CEFP])-\d{4}` (K1 with M4) and print the file name otherwise; empty source file prints `[Id …, ]` — name it missing.
- **L4** clash-description-sheet.py:30 comment says the GUID column is "the key of <snapshot>-ai.json"; that file is keyed by group id (CM2). SKILL.md step 5 says "khoá theo GUID nhóm", plan slice-4 row says "keyed by GUID" — same fix; SKILL table "Trục = Grid Location của Navisworks" is now the element's own grid with report fallback.
- **L5** clash-ai-text.py:100 hard-codes `"(không rõ)"` = `ClashSnapshotRules.UnknownLevel` (ClashSnapshot.cs:81) — second copy (K1); read it from the snapshot or one shared constant note.
- **L6** clash-ai-text.py:120 non-negative distance prints "cách nhau … (trong dung sai kiểm tra)"; every HP test is Hard, so the branch is for clearance tests that do not exist — the wording is wrong if it ever fires (a hard result at 0 mm is touch, not within tolerance).
- **L7** NavisClashSnapshotReader.cs:44-45, :79-82 lines past 120 chars (FM2); `ClashItemInfo` now 10 positional params (M4 trigger — acceptable for a wire DTO record).
- **L8** `-ai-groups.json` stores absolute picture paths (clash-ai-groups.py:195) into the shared ClashReports folder beside the model — prefer paths relative to the snapshot.

### Info
- Snapshot schema change is additive with defaults; Python reads new keys with `.get` → older snapshots still build (P9 spirit).
- `Text()` formats doubles raw invariant (NavisClashSnapshotReader.cs:110); if a "Size" ever arrives as a double it is internal units, not mm — fine today (Revit sizes are strings).

## Positives
- Fail-closed gate: missing rules file, unknown ruleId, unapproved, wrong pair, too deep → "Cần xác minh"; unknown classification → VERIFY (clash-ai-text.py:130-138); KB band overrides strip `ruleId` when not VALID (:72-73).
- Missing facts are named per element and drive confidence; MEP system only demanded where meaningful (cable trays exempt, tested).
- Facts vs judgement cleanly split: per-row text only from snapshot fields; judgement per group.
- Grid location read on the element's own file's grid system, null when none — honest, documented in the summary.
- `SNAPSHOT_NAME` regex + exact phase comparison fixes a latent `350` vs `350-400` prefix collision; regression test added.
- Original agenda columns untouched; GUID column hidden; AI columns appended after W.
- Pure rules extracted to `ClashElementFacts` with tests (M6/M7 core).

## Score / verdict
**7/10 — approve after H1 + H2.** H1 must land before any HV rule is approved (today no rule is approved, so no wrong row exists yet). M1/M2/M4/M6/M7 recommended in the same slice; M3/M5 are workflow-quality and can follow.

**Status:** DONE_WITH_CONCERNS
