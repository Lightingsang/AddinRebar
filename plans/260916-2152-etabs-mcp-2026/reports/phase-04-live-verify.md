# Phase 4 — live-verify `-Phase full` từ publish folder, registry loop, hồi quy 3 host, docs (2026-09-17)

**Trạng thái:** **Verified** — `run-live-verify.ps1 -Phase full -Publish -Runs 3 -Tag phase4-full` → **3 × 102 PASS, 0 fail, 0 skip** (236 s; ~80 s/run), bridge + server chạy từ `HPEtabs/output/HPEtabs.McpBridge` (folder publish, self-check OK) và `HPEtabs/output/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.exe` (single-file); ETABS 22 pid 73096, model bỏ đi `Project_MCP_TEST.EDB`; ETABS còn chạy sau harness; mỗi run 1 registry root riêng (`registry-runN`), **không wipe giữa các nhóm** trong run.

## Ma trận (mỗi run)
| Nhóm | Phase harness | Check | Nội dung |
|---|---|---|---|
| A | `disabled`, `detached` (X1) | 2 + 1 | `-32001` "(a separate app, not inside ETABS)" trước tick; context chưa attach; bridge publish self-check OK (log `MCP scripting self-check OK … ETABSv1.dll from …`) |
| E | `detached` (E19/E20/P/I), `spike`, `bridge`, `bridgedestructive` | 15 + 6 + 17 + 7 | guard ×7 (+`global::`), `File.Save()` D, not-attached `-32003` tức thì, W dưới auto chưa attach `-32003`; units ép/restore, GetNameList 9 receiver, timeout 5 s; R read, preview `none`/dryRun, W `AddByCoord` snapshot + presave + audit `started` trước `[tier:W]`, `manual` ≡ auto, label `../x` sanitized, throw sau ghi `rolledBack:false`, D-off `-32001`, `PATH` ×2, compile error, timeout rồi request kế; D-on: delete + `[destructive]`, path lúc chạy refused không `started`, dryRun D = preview |
| S | `seeds` | 26 | 12 seed: R ×5, caller error ×5 (`ArgumentException`), `test_tool` dryRun trên W/D = preview, `realRun=true`, W ×3 snapshot, dir 2 Local, `run_analysis` OFF ×5 `-32001` vẫn published, `propose_tool` RunAnalysis / `none`+SetSection từ chối, paging |
| R | `registry` | 19 | MISS → ad-hoc (`runId`, hint) → `get_run` → `toolify_run` (nêu ETABS) → `propose_tool count_frames_by_section` (host etabs, Geometry) → `test_tool` 2/2 tested → `publish_tool` pending + review file nêu `HPEtabs.Mcp.Server.exe registry approve` → `run_tool` refused pending → CLI approve → **listed ≤ 0.5 s** (`tools/list_changed`) → search hit → gọi theo tên; fragile `mcp_verify_frame_section` → 5 fail → **quarantined**, out of list, `run_tool` refused → `manage_tool restore` → newVersion v2 (guarded) → test → publish → approve → back → 5 `ArgumentException` **vẫn published**; CLI `list`/`stats` |
| S (D) | `seedsdestructive` | 9 | `SetRestraint` (W), `run_analysis` [Dead] thật (~14 s, `errors []`, locked), reactions FZ 21.9 kN, frame forces, Modal + mass ratio, story filter rỗng, cleanup (delete + unlock + run flags = all), **E10**: unlock → mọi case not-run |
| **Tổng** | | **102** | |

Harness không start/stop/gửi phím vào ETABS; UIA chỉ trên cửa sổ bridge (pid); registry cách ly; `.mcp.json` không đụng.

## X — hồi quy 3 host (`reports/regression-tools-list-phase-04.py`, build Release mới)
| Host | Tool phase 0 | Hiện tại | Identical | Ghi chú |
|---|---|---|---|---|
| Revit | 33 | 33 | **33/33** | |
| AutoCAD | 40 | 57 | **37/37** + 3 đổi bởi **plan AEC** (commit 28b075d, `audit_aec_drawing`/`cad_standards_check`/`create_issue_markup` — review round phase D) + 17 seed AEC mới | `git log 9afc503..HEAD -- McpShared/` = chỉ `a8c867b` (`#r`/`#load`), không đổi tools/list |
| Navisworks | 24 | 24 | **24/24** | |

Suites sau đổi engine (chạy cùng ngày, sau `a8c867b`): McpShared 164 + 62, Revit 109, Navis 49 + 135, AutoCAD 241 (working tree session AEC), ETABS 81 + 184. `git diff --stat HPRebar/ HPNavis/` rỗng; `HPAutoCad/` = working tree của session AEC (không phải plan này).

## M — manual-only (👤)
| Ô | Kết quả |
|---|---|
| Modal dialog trong ETABS → busy `-32002` | 🟡 Phase 1 E15: dialog **không** chặn OAPI (call trả 0.5 s) — hành vi ghi nhận, không phải `-32002`; harness `modal` assert "busy ≥ 8 s hoặc read xong" |
| Kill ETABS **giữa call** → not-attached sau khi call trả/timeout, Attach lại OK | ❌ Chưa làm (phase 1 chỉ đóng ETABS **giữa các call** → `-32003` tức thì, re-attach OK) |
| Licence seat (ETABS thứ hai) | ❌ Chưa làm |
| Restore snapshot: mở `presave`/`prerun` trong ETABS, so mắt | ❌ Chưa làm (file tồn tại, 10/5 retention verified) |
| `run_analysis` trên project thật | ❌ Chưa làm — chỉ model bỏ đi 2 cột |

## Phát hiện phase 4
- Wrapper `-Runs N` ban đầu dùng chung 1 registry → run 2/3: MISS sai, fail tích luỹ → `get_joint_reactions` **bị quarantine** (S15c ném `InvalidOperationException` mỗi run). Sửa 2 chỗ: registry riêng mỗi run; seed results ném **`ArgumentException`** "has no results … run_analysis first or pick a case that ran" (người dùng hỏi kết quả trước khi chạy analysis không được làm seed bị quarantine — đảo lại M5 của review phase 3, có lý do).
- `os.environ[...]` trên Windows upper-case key → `dict(os.environ)["HPETABS_MCP_Registry__LibraryPath"]` KeyError; .NET bind không phân biệt hoa thường.
- `-Exe`/`-BridgeExe` tương đối → python không tìm thấy; wrapper `Resolve-Path`.
- Presave bucket ở cap 5 → check "count + 1" sai; dùng `min(before + 1, 5)`.
- CLI `stats` không in tên tool; `list` có.
- Lần chạy `-Publish` đầu tiên: UIA không thấy cửa sổ bridge trong 40 s dù log "window opened" (bridge chạy tay thì thấy) — không lặp lại ở 4 lần sau; chưa giải thích.

## Sai lệch so với phase file
- Nhóm E "ETABS closed/no-model/UNC model" không có trong `full` (cần thao tác ETABS); đã verify ở phase 1 (`closed`, `nomodel`) — E12b (W không model → `-32003`) ở phase 2. UNC model: chưa có `-UncModelPath`.
- Nhóm X so `tools/list` theo tên+JSON, loại trừ có chứng cứ git thay vì byte-identical toàn file (AutoCAD đã lớn thêm 17 seed).
- `-SkipNoModel`/`-SkipClose` không cần: các phase đó tách riêng (`nomodel`/`closed` với `-Interactive`).
