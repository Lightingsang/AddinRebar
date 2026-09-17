# Phase 3 — 12 seed ETABS + registry per host (2026-09-17)

**Trạng thái:** Implemented + Built + Tested (server **81** = 17 + 25 compile/tier + 39 structure) + **Verified live** 5 lần trên ETABS 22, model bỏ đi `Project_MCP_TEST.EDB` (`run-live-verify.ps1 -Phase seeds`: 3 × (20 + 9) trước review, 2 × (**26 + 9**) sau review).

## Đã dựng
| # | Seed | Cat | Tier | Điểm chính |
|---|---|---|---|---|
| R1 | `get_model_info` | Model | R | file/folder, lock, units present/database, ETABS version, counts, `includeStories` |
| R2 | `get_stories_and_grids` | Geometry | R | `GetStories_2` (10 ref) + `GridSys.GetNameList` |
| R3 | `get_structural_objects` | Geometry | R | kind frame/area/point, story/nameLike filter, offset/limit, endpoints/vertices mm, lengthMm; **không** section cho area (`cAreaObj.GetProperty` bị base guard) |
| R4 | `get_materials_and_sections` | Property | R | E/G ×1000 → MPa; `GetSectProps` mm²/mm⁴/mm³ |
| R5 | `get_load_definitions` | Load | R | patterns/cases/combos (`sapModel.RespCombo` = `cCombo`), `includeComboCases` |
| R6 | `get_joint_reactions` | Results | R | select case **hoặc** combo cho output, `JointReact("All", GroupElm)` hoặc per point, story filter, kN·mm ÷ 1000 |
| R7 | `get_frame_forces` | Results | R | `FrameForce` all/per frame, stations mm |
| R8 | `get_modal_results` | Results | R | `ModalPeriod` + `ModalParticipatingMassRatios` khớp theo index (fallback số mode) |
| W1 | `draw_frame_by_coords` | Geometry | W | validate section → `AddByCoord`; `changed.added` = 3 |
| W2 | `assign_frame_section` | Property | W | validate section → `SetSection`; errors[] per frame |
| W3 | `assign_frame_load` | Load | W | distributed kN/m ÷ 1000 (per mm) / point kN; Dir 10 gravity; validate pattern |
| D1 | `run_analysis` | Analysis | D 600 s | `SetRunCaseFlag` all/cases → `DeleteResults` (tuỳ chọn) → `RunAnalysis` → `GetCaseStatus`; mô tả cảnh báo timeout không abort |

Chữ ký OAPI lấy bằng **reflection trên DLL** (không CHM) — ghi vào `tool.json.notes`. Sinh bởi `tools/generate-seed-library.py` (repo), commit output.

Tests: `SeedLibraryStructureTests` (12 record, validator profile ETABS, guard `GuardProfile.Etabs`, args ⇔ schema, cấm `SetPresentUnits|Helper|OpenFile|ExportFile|ImportFile|ImportProp|CSVFile|MergeAnalysisResults|ShowTablesInExcel|ApplyEditedTables|SetModelIsLocked|InitializeNewModel`, `RunAnalysis|DeleteResults|Save|Delete*` chỉ D1, mô tả D bắt đầu `DESTRUCTIVE` + "Allow destructive operations" + "timeout", R có "Read-only", W có "snapshot", ≤ 120 dòng) + `SeedLibraryCompileTests` (wrapper class = imports/globals bridge, metadata ref `ETABSv1.dll` tìm qua env/CLSID/Program Files → `Assert.SkipWhen`; mini semantic pass bind mọi method ETABSv1 → tier từ `Resources/etabs-oapi-tiers.txt` khớp `transaction`/`tags`).

## Live (3 lần, mỗi lần 53 check; `phase3-seeds`, `phase3-seeds2` ×2)
| Check | Kết quả |
|---|---|
| S0 `tools/list` = 24 (12 core + 12 seed), 6 category, `_seeds.json` | ✅ |
| S1–S5 R seeds trên model 4 story / 4 material / 126 section / patterns Dead,Live / cases Dead,Live,Modal | ✅ |
| S3b/S9b/S15 caller error → `ArgumentException` (không đếm stability) | ✅ |
| S6/S11 `test_tool` (dryRun) trên W/D → 0 passed, "static preview … nothing ran" | ✅ (red-team #4) |
| S7 `test_tool realRun=true` W1 → chạy thật, snapshot, `changed.added` 3 | ✅ |
| S8–S10b `run_tool` W1/W2/W3 → snapshot mỗi lần; Set* `changed` 0 | ✅ |
| S12 `run_tool run_analysis` ×5 với D off → `-32001`, tool **vẫn published** | ✅ (red-team #5) |
| S13/S14 `propose_tool` RunAnalysis / `none`+SetSection → từ chối ("is destructive … cannot be stored as a tool" / "declared transaction: none, but … writes") | ✅ |
| D0 `SetRestraint` chân cột (W qua execute) | ✅ |
| D1 `run_analysis` cases [Dead], deleteResultsFirst → Dead finished (+`~LLRF`), model locked, **14 s** | ✅ |
| D2 `get_joint_reactions` Dead → FZ = **21.9 kN** tại Base = trọng lượng W14X500 × 3 m (≈ 744 kg/m × 3 × 9.81) | ✅ sanity vật lý |
| D3 `get_frame_forces` 1 cột → 3 station, P = −21.9 kN | ✅ |
| D4a/D4 `run_analysis` Modal → 4 mode, T1 = 0.041 s, mass ratio có số | ✅ |
| D5 story filter không khớp → `count 0`, `matched 0`, `truncated false` | ✅ |
| D6 cleanup: unlock + delete 2 frame (D) → presave mới (analysis tự ghi file) | ✅ |
| **D7 (E10)** sau `SetModelIsLocked(false)`: mọi case `GetCaseStatus` = 1 not-run | ✅ **unlock xoá kết quả — `[chưa xác minh]` đóng** |

## Phát hiện
- **ETABS không đăng ký ROT khi mở bằng cách khác thường:** instance 45088/62260 (user mở lại) không có entry ROT, `GetObject`/`GetObjectProcess` đều null, `Tools › Active Instance for API` mờ; mở lại theo trình tự "ETABS trước → File › Open" (pid 73096) → ROT có ngay. Giả thuyết: shell-open `.EDB` (hoặc launcher khác) bỏ `RegisterActiveObject`. Thông báo Attach nay nêu cả trường hợp elevated. Ghi CLAUDE.md gotcha.
- `RunAnalysis` tự lưu file → run W/D kế tiếp thấy "file không do bridge ghi" → presave (đúng thiết kế).
- `AddByCoord` với `UserName` trùng lần chạy trước: ETABS tự đánh số (S8 name trả "MCPSEED" lần đầu; lần sau khác) — harness so theo danh sách trước/sau.
- `test_tool` (dryRun) trên seed W/D không bao giờ "tested" — đúng red-team #4; test bằng `realRun=true` trên model bỏ đi.
- Runner: message sau exception trong W/D phân biệt `changed` 0 ("No additions or deletions were recorded") vs > 0 ("Changes … persisted").

## Sai lệch so với phase file
- Chữ ký từ reflection thay CHM; `notes` vẫn ghi topic.
- R1 có arg `includeStories` (structure test đòi ≥ 2 example khác args).
- R5: `includeComboCases`; combos qua `cCombo` (không có `cRespCombo`).
- Harness tách `seeds` (D off) / `seedsdestructive` (D on) như phase 2; D1 chạy `cases:[Dead]` (model 2 cột không có tải Live) rồi `Modal` riêng.
- Envelope `snapshot` không nằm trong value (script không biết tên file) — `ExecuteResult.snapshot`; mô tả seed nói "snapshot in the result".

## Review round (`code-review-phase-03.md` 7/10 fix-first → đã sửa cùng ngày)
| # | Sửa |
|---|---|
| H1 | `assign_frame_load`: `CSys` = Local khi direction 1–3 (trước luôn "Global" → 1–3 không bao giờ chạy); harness S10c direction 2 → `coordinateSystem Local` |
| H2 | `get_frame_forces`/`get_joint_reactions`: tên validate trước → `ArgumentException` "labels are not names — use get_structural_objects.name" (không tính stability); harness S15b |
| M1/M2 | `get_model_info`: mọi `ret` kiểm; `.$et` → `.EDB` + `workingFile` |
| M3 | `get_structural_objects`: `matched` null khi truncated, `matchedAtLeast`; harness S8b paging |
| M4 | `run_analysis`: validate `cases` trước khi đụng run flag; `success` = không case could-not-start/not-finished, `errors[CASE_FAILED]`; mô tả nói flag tồn tại trong model; cleanup harness restore flags |
| M5 | results seeds: `GetCaseStatus` pre-check "has no results (status not run) — run_analysis first" (harness S15c); modal: case không phải Modal → `ArgumentException` (S15d) |
| M6 | regex cấm `\w*CSVFile` + `CreateAnalysisModel|ModifyUndeformedGeometry\w*|New*`; test pin regex |
| L1–L8, L10 | 1 lần `GetCoordCartesian`/đầu; section casing chuẩn; `limit`+`ct`+tên comboType; `z22Mm3`; check "kết thúc bằng return top-level"; cấm `?.` trong seed; notes không mã evidence; mô tả gravity "downward" |
| L9 | Không thêm unit test runner (cần `cSapModel` sống) — harness S9b pin |

Generator `tools/generate-seed-library.py` vào repo (đường dẫn tương đối) — sửa seed = sửa generator rồi sinh lại.

## Chưa làm / gap
- Registry loop MISS → propose → approve → quarantine (phase 4).
- Máy không ETABS: 25 test compile/tier **SKIP** quan sát được — chưa chạy trên máy như vậy.
- `.mcp.json` `hprebar-etabs` — user thêm.
