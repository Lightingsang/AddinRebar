# Phase 2 — runtime bridge ETABS: tier semantic, snapshot, fingerprint, path policy (2026-09-17)

**Trạng thái:** Implemented + Built + Tested (184 bridge + 17 server; engine 164 + 62) + **Verified live** trên ETABS 22 v22.7.0.4095, model bỏ đi `Project_MCP_TEST.EDB` (0 object): harness `-Phase bridge -Runs 2` → 2 × 48 PASS.

## Đã dựng
| Thành phần | Vai trò |
|---|---|
| `tools/generate-oapi-tier-fixture.ps1` | One-off: reflection trên `ETABSv1.dll` cài sẵn → `Resources/etabs-oapi-tiers.txt` (1 281 dòng, 135 interface: **733 R / 446 W / 102 D**) + `etabs-oapi-index.txt` (1 107 topic CHM `c*.X Method`). PowerShell thay `.py` vì cần .NET reflection. Rule (thứ tự): `cHelper.*` + lifecycle `cOAPI` D → tham số path **by-value** (`FileName|csvFilePath|SourceFileName|FilePath|Path`) D → `cAnalysisResults/cAnalysisResultsSetup/cView/cSelect` R → prefix `Start/Modify/Merge/Reset/Clear/Rename/Show/Export/Import/Replicate/Delete` D → D explicit (`SetModelIsLocked, RunAnalysis, DeleteResults, CreateAnalysisModel, InitializeNewModel, ApplyEditedTables, Save, OpenFile, New*`) → `Get*/Is*/Has*/Verify*`, `Count/RefreshView/Visible` R → còn lại W |
| `EtabsTierTable` | Load fixture embedded; `Parse` từ chối trùng/tier lạ; `TryGet(cInterface, member)` |
| `EtabsTierAnalyzer` | **Semantic**: `Script.GetCompilation()` → `SemanticModel`; mọi member access/binding bind về symbol thuộc namespace `ETABSv1` → tra bảng; enum field bỏ qua; property điều hướng (trả interface ETABSv1) bỏ qua; member không có trong bảng → D; member không bind trên receiver ETABSv1/unknown → D. Member `path=`: đối số phải là **string literal** (screen tĩnh) hoặc `args.Str("k")`/`args.Require("k")` key literal (screen lúc chạy); khác → refusal `PATH`; `File.Save()` không đối số (optional) = save tại chỗ, không screen |
| `EtabsPathPolicy` | Tĩnh: UNC, `HPEtabs\McpBridge|McpServer`, `\Computers and Structures\`, rỗng. Lúc chạy: tuyệt đối có ổ đĩa, `GetFullPath` nằm dưới thư mục model hoặc `%LocalAppData%\HPEtabs\`; `LooksLikePath`; `StringValues(args)` đệ quy |
| `EtabsSnapshotManager` | `EnsureSnapshotable` (không path / UNC / file không tồn tại → `-32003`, trạng thái môi trường); `Prepare`: presave khi file trên đĩa **không** do bridge ghi lần cuối (nhớ mtime+size sau mỗi `Save()` của mình) → `File.Save()` (ret≠0 → fail, không copy) → prerun copy; bucket `presave` 5 / `prerun` 10; label `[A-Za-z0-9_-]{1,40}`; đích assert trong bucket; trùng giây → `-2`; trả **tên file** |
| `EtabsFingerprint` | `GetNameList` 8 receiver + `Story.GetStories_2` + lock + file; `Diff` → `Changed(added, 0, deleted)` + ghi chú |
| `EtabsUnitsPolicy` | Ép `kN_mm_C`, restore trong `finally` (inject get/set để test) |
| `EtabsScriptRunner` | Đồng hồ budget chạy **trước** save → units → (W/D) model path chuẩn hoá `.$et`→`.EDB` → `EnsureSnapshotable` → path refusals lúc chạy → audit `started` (callback) → snapshot → fingerprint trước → script → fingerprint sau; exception sau ghi: `isError`, `rolledBack:false`, message nêu snapshot; `Snapshot` = tên file |
| `EtabsExecutor` (+ `.Audit`, `.Worker`) | guard → compile → analyzer → `PATH` refusal → D & !opt-in → JSON-RPC `-32001` → (tier ≥ W & (dryRun \| none)) → static preview (`isError`, `rolledBack:true`, `PREVIEW`) → chưa attach → `-32003` → ceiling 600 s cho D / 120 → chạy. `Analyze` thêm `PATH`/`DESTRUCTIVE`/`PREVIEW`. Audit: `started` ngay trước save; dòng kết thúc gắn `[tier:X] [snapshot:file] [destructive]` |
| Window | Bỏ 3 nút probe spike + `SpikeProbes.cs`; text "writing script saves the model first…"; link mở thư mục snapshot (`OpenSnapshots`) |
| Harness | `live-verify.py --phase bridge` (B1–B13b, 17 check) + `bridgedestructive` (D0–D5, 7 check); `run-live-verify.ps1 -Phase bridge` tick/untick `AllowDestructive` quanh phase D; `nomodel` thêm E12b (W không model → `-32003 No model (.EDB)`); `detached` P1 → preview dưới `none`, P1b W dưới `auto` chưa attach → `-32003`; `spike-step.ps1` bỏ `probe` |

## Kết quả live (ETABS 22, model `Project_MCP_TEST.EDB` đã lưu, `run-live-verify.ps1 -Phase bridge -Runs 2 -Tag phase2-bridge`, 75 s)
| Phase | Run 1 | Run 2 | Ghi chú |
|---|---|---|---|
| disabled | 2/2 | 2/2 | |
| detached | 16/16 | 16/16 | |
| spike | 6/6 | 6/6 | E18 pass với model mở |
| bridge (D off) | 17/17 | 17/17 | B5: `AddByCoord` → `changed.added = 3` (1 frame + 2 point), snapshot `prerun\<ts>-B5_add_frame.EDB`, presave lần đầu; audit `started … forced save` trước dòng `[tier:W]`; B6 `manual` ≡ auto, label `../x` → `…-x.EDB`, không presave mới; B7 throw sau ghi → `rolledBack:false`, message nêu snapshot; B9 D-off `-32001`; B10/B11 `PATH`; B13 timeout 5 s rồi request kế chạy |
| bridgedestructive (D on) | 7/7 | 7/7 | D1 xoá 2 frame → `deleted = 6` (frame + point mồ côi — ETABS tự xoá), snapshot + audit `[destructive]`; D3/D4 path ngoài thư mục model → `PATH` lúc chạy, **không** dòng `started` (D4a); D5 dryRun D → preview |
| Retention | prerun 10, presave 2 | | `%LocalAppData%\HPEtabs\McpBridge\snapshots\Project_MCP_TEST\` |

`Save()` model 0 object: ~590–640 ms trong budget (log "taken before the run (638 ms)").

## Test
| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPEtabs.slnx -c Debug` | ✅ 0 error |
| `HPEtabs.McpBridge.Tests` | ✅ **184/184** sau review round (166 trước), 0 skip (cần ETABS cài): TierFixture, TierAnalyzer (theory ~70 row), PathPolicy, SnapshotManager (temp dir), RunnerParts, ExecutorRefusal, ResultSerializer |
| `HPEtabs.Mcp.Server.Tests` | ✅ 17/17 |
| grep `Helper\.` | ✅ chỉ `EtabsAttachment.cs` (self-check không còn gọi Helper) |
| grep `public static` executor/analyzer/snapshot | ✅ chỉ hàm thuần (`EnsureSnapshotable`, `SanitizeLabel`, `Preview`) + `BridgeEntry.Start` (namespace bị guard chặn — E20.5) |
| Guard ∩ tên member OAPI | ✅ ≤ 12, không chứa `GetNameList/SetSection/AddByCoord/Save/RunAnalysis/…` (test `The_guard_denies_few_oapi_member_names…`) |

## Sai lệch so với phase file (tự quyết)
- Fixture sinh bằng **reflection trên DLL** (PowerShell), CHM index chỉ để test phủ — binding nhìn DLL, DLL có overload `_2/_3` mà CHM gộp.
- `EditGeneral.Move` = **W** (không D như dòng test dự kiến): theo rule (a)–(e) ADR-02; không unlock/file/delete; snapshot đủ. Test pin ≠ R.
- `cSelect.*`, `cView.*` = R (UI state, không đụng file) — rule (b) `Clear*` lẽ ra cho `ClearSelection` D; review tay.
- `Verify*` = R (`DesignSteel.VerifyPassed` chỉ đọc kết quả).
- Path value check "dưới thư mục model" chạy **trên worker** (biết model dir), không ở pipe thread; static chỉ chặn shape/UNC/folder cấm.
- Refusal path lúc chạy: `ScriptDiagnostic(0,0,"PATH",…)` (không có vị trí nguồn).
- Audit `started` ghi **ngay trước `File.Save()`** (sau path check lúc chạy) thay vì trước queue: D3 lần đầu cho thấy `started` + `rejected` mà không save gì — sửa, harness D4a pin.
- Gate syntax phase 1 (`EtabsTierGate`, 46 test) **xoá**, thay bằng analyzer semantic (ADR-02 nói rõ). Alias/cast/lambda giờ bind đúng member thay vì fail-closed.
- `EtabsExecutor.cs` 326 dòng → tách `EtabsExecutor.Worker.cs` (control lane, loop, quiescence, P/Invoke).

## Phát hiện
- `AddByCoord` tạo 2 point + 1 frame; `Delete` frame xoá luôn point mồ côi (ETABS) → `changed` đếm cả point. Mô tả tool nói "additions/deletions only" — đúng, bổ sung ví dụ ở phase 3.
- `GetModelFilename()` trong script trả `.$et` sau save (E9) — script thấy `.$et`; context/snapshot chuẩn hoá.
- `cFile.Save(FileName = "")`: tham số path **optional** → `Save()` = save tại chỗ, không screen; `Save("x")` screen.
- PS 5.1 + file không BOM: em dash trong chuỗi phá parse (lần 2) — generator dùng ASCII.

## Review round (`code-review-phase-02.md` 6.5/10 fix-first → đã sửa cùng ngày)
| # | Sửa | Ở đâu |
|---|---|---|
| H1 | `args.Str(key, fallback)` / `ScriptArgs` tự tạo / class trùng tên / method group → refusal `PATH`; key khai báo phải có trong `args` dạng string (thiếu hoặc số → refusal) | `EtabsTierAnalyzer.ArgsKey` (receiver = field `args` của `EtabsScriptGlobals`, type `HPRebar.McpBridge.Core.Scripting.ScriptArgs`, đúng 1 đối số), `EtabsScriptRunner.PathRefusals` |
| H2 | `#r` / `#load` bị guard từ chối cho **mọi host** (trivia, walker không thấy; compiler sẽ honour) — engine `ScriptGuard.Check` + test 2 row (`HPRebar.Mcp.Server.Core.Tests` 164, `Net48Tests` 62) + pin trong `EtabsExecutorRefusalTests` | `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs` |
| M1 | Static rule chạy lại trên path đã `GetFullPath` (`\.\`, `\\`, `..`); `%LocalAppData%\HPEtabs\McpBridge\**` cấm (snapshot/audit/settings) | `EtabsPathPolicy.RuntimeRefusal` |
| M2 | Preview (`none`/dryRun) **trước** check opt-in D — đúng bảng ADR-02; theory `A_destructive_script_under_none_or_dry_run_is_previewed_without_the_second_opt_in` | `EtabsExecutor` |
| M3 | `cancel_execution` trong lúc save → "Cancelled while saving…", `TimedOut=false` | `EtabsScriptRunner` |
| L1–L8 | Key non-string refusal; prune theo tên (timestamp) không theo mtime; `nameof(...)` bỏ qua; `Save("")` = `Save()`; receiver refuse → `null`, không diff; model ở drive root / ADS `:stream` refused; `fullPath` flag (`cHelper.CreateObject*`); harness `snapshot_dir` = `SanitizeLabel(stem, 60)` | analyzer, snapshot manager, fingerprint, path policy, generator, harness |
| Info | `EtabsExecutor.DefaultMaxTimeoutSeconds = 120`; kết quả "nothing ran" có `rolledBack:true`; mô tả tool nêu screening `args` dạng path (1 7xx ≤ 1800) | executor, runner, server |

Sau sửa: build xanh; bridge tests **184/184**; server 17/17; engine 164 + 62; harness `-Phase bridge -Runs 1 -Tag phase2-review` **48/48** (37 s).

## Chưa làm / gap
- E10 unlock-xoá-kết-quả vẫn `[chưa xác minh]` (model không có results).
- Fingerprint drift trên R (writer khác) chưa có kịch bản live.
- `modal`/`closed` chưa chạy lại phase 2 (liveness không đổi).
- `.mcp.json` `hprebar-etabs` — user thêm.
- Phase 3: seed R/W trên runner này; `test_tool realRun`.
