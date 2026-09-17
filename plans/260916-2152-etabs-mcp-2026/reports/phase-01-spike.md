# Phase 1 report — HPEtabs scaffold + COM attach spike (ETABS 22 v22.7.0.4095, OAPI wrapper 2.10.0.0)

**Ngày:** 2026-09-17 · **Status:** Implemented + Built + Tested + **spike Verified với ETABS thật** (E9–E20 bên dưới; W/D chưa chạy — phase 2) · Harness `HPEtabs/tools/harness/` (`run-live-verify.ps1`, `live-verify.py`, `spike-step.ps1`) · Log bridge `%LocalAppData%\HPEtabs\McpBridge\logs\`.

## Đã dựng

`HPEtabs/` (slnx, `global.json`, `Directory.Build.props` dò qua CLSID `LocalServer32`, README, .gitignore) · `HPEtabs.McpBridge` (WPF net8.0-windows, 14 file: `BridgeEntry`, `EtabsExecutor` (+Audit), `Service/{EtabsApiLocator, EtabsAssemblyResolver, EtabsAttachment, EtabsTierGate, EtabsScriptRunner, EtabsResultSerializer, EtabsContextReader, ScriptingSelfCheck, SpikeProbes}`, `Model/EtabsScriptGlobals`, `View`/`ViewModel`, theme) · `HPEtabs.McpBridge.Tests` (26 test: tier gate 22 + serializer 4 + module initializer cài resolver; cần ETABS cài) · `HPEtabs.Mcp.Server` (profile 22, 4 core tool, prompts, resources) · `HPEtabs.Mcp.Server.Tests` (17, không cần ETABS) · publish: server single-file 7.1 MB, bridge **folder** 21 file 13.5 MB không có `ETABSv1.dll`.

## Kết quả spike (ETABS 22 thật, model bỏ đi `…\Desktop\Etabs\NABTLU-HPC-NA-ZZ-ETA-ES-0001.EDB` — 6 story, 7 pattern, 8 case, 29 combo, 46 frame section, 0 object, chưa chạy analysis)

| # | Kết luận | Bằng chứng |
|---|---|---|
| **E9** | **`File.Save(path)` = save-as: đổi tên hiện tại của model** (`Save(tmp)` ret 0/1 172 ms → `GetModelFilename` = tmp; `Save(original)` ret 0/911 ms → tên về gốc). Save ghi sidecar `.$et` + `.ico` cạnh `.EDB`. **Sau save, `GetModelFilename(true)` trả `…​.$et`** (file working của ETABS), không phải `.EDB` — context/snapshot phải chuẩn hoá về `.EDB` | log `E9:` 05:49:38; thư mục model có `.EDB` 74 391 B + `.$et` 109 409 B cùng mtime; context sau probe `docPath …​.$et` |
| **E10** | `GetCaseStatus(ref int, ref string[], ref int[])` đúng chữ ký (status 1 = not-run); `SetModelIsLocked(false)` ret 0 / 1 ms; model chưa có kết quả → **"unlock xoá kết quả" vẫn `[chưa xác minh]`** (cần model đã chạy analysis) | log `E10:` |
| **E11** | `Save()` không dialog, ~1 s cho model 74 KB (quan sát trong E9) | log `E9:` |
| **E12** | Start screen / model đóng: `GetModelFilename()` = **"(Untitled)"** (không rooted), `GetNameList` ret 0 count 0, units đọc được, ETABS vẫn trả lời → rule "no model" = `GetModelFilename(true)` không phải path rooted; R chạy vô hại, W/D từ chối | `nomodel-context.json`, `nomodel-execute.json` |
| **E13** | Units **ép `kN_mm_C` trong run** (`GetPresentUnits()` trong script = kN_mm_C, `units.Label` kN_mm_C, mmPerUnit 1) và **restore** sau run (context `presentUnits` = N_mm_C của user); `GetDatabaseUnits()` = N_mm_C; `GetCoordCartesian(name, ref x, ref y, ref z)` đúng chữ ký (không có point để đo) | `spike-units.json` |
| **E13b** | `GetNameList(ref int, ref string[])` **ret 0 trên cả 8 receiver** `PointObj/FrameObj/AreaObj/LoadPatterns/LoadCases/RespCombo/PropFrame/PropMaterial`; `Story.GetStories_2` có **10 tham số** (`ref double BaseElevation` đầu) ret 0 → fingerprint dùng được | `spike-receivers.json` |
| **E14** | Proxy tạo trên STA worker **gọi được từ thread pool (MTA)** — wrapper/COM marshal xuyên apartment OK. Giữ 1 STA worker (1 chủ, tuần tự) nhưng không phải vì bắt buộc | log `E14:` |
| **E15** | Dialog mở trong ETABS (theo user): `IsWindowEnabled(MainWindowHandle)` = **true**, `execute` trả lời trong **0.5 s** → ETABS 22 (.NET 8) **vẫn phục vụ COM khi dialog mở** (message loop lồng). Pre-check vô hại; "busy" chủ yếu từ script đang chạy. Chưa quan sát được dialog nào chặn `[chưa xác minh]` cho file dialog native | `modal-execute.json` |
| **E16** | `ETABSv1.dll resolved from C:\Program Files\Computers and Structures\ETABS 22\ETABSv1.dll (2.10.0.0)`; `bin/` và publish **không** có `ETABSv1.dll`; `Assembly.Location` khác rỗng (self-check assert) trên Debug **và** publish folder | log Debug + publish; `Test-Path` false |
| **E17** | Roslyn compile + run script tham chiếu `ETABSv1` qua stdio → int (`PointObj.GetNameList`) 20–60 ms sau lần compile đầu | `spike` PASS ×4 lần |
| **E18** | `Helper.GetObject` (qua interface `cHelper` — `Helper` implement explicit) → cOAPI; wrapper 2.10.0.0; **2 instance ETABS → GetObject trả instance MỚI hơn (rỗng)**, warning "2 instances" hiện đúng; 1 instance → pid xác định, hook `Exited` | context `attachedPid 23064/49336`, warning text |
| **E19** | Đóng ETABS → `Process.Exited` → "Detached: ETABS exited" → execute từ chối **0.1 s** `-32003` "click Attach", context `isAttached=false`; mở lại ETABS → **Attach lại không restart bridge** → spike chạy. Bổ sung sau spike: **COM disconnect (0x800706BA/0x80010108/…) trên bất kỳ call → Detach + `-32003`** — bắt trường hợp >1 instance (không watch pid) mà lần đầu treo 23 s | `closed` PASS; `Detached from ETABS: ETABS exited` 05:57:52 |
| **E20** | Guard `GuardProfile.Etabs` từ chối 7 mẫu (`ApplicationExit`, `new Helper()`, `Marshal`, `Expression.Call`, `HPEtabs.McpBridge.*`, `McpBridgeHost.Current`, `global::System.IO.File`); **`sapModel.File.Save()` qua guard (`File` ở vị trí member) → tier D → `-32001`** khi checkbox OFF | `detached` 15/15 ×3 (Debug ×2, publish ×1) |
| T1 | Timeout 5 s cooperative trên vòng lặp kiểm `ct`: `timedOut=true` | `spike` |
| P1/P2 | W → static preview (`isError`, `PREVIEW: FrameObj.SetSection (W)`, `rolledBack:true`, không chạy); D + checkbox OFF → JSON-RPC `-32001` "Allow destructive operations" | `detached` |

## Phát hiện đổi thiết kế (đã sửa trong phase 1)
1. `Helper` implement `cHelper` **explicit** → gọi qua interface.
2. Tier gate: member trên **kết quả call** (`GetPresentUnits().ToString()`) không phải member OAPI → bỏ qua; `ToString/Equals/GetHashCode/GetType` không bao giờ là W. `EditGeneral.Move` = W (không phải D).
3. Serializer: `Dictionary<K,V>` qua `IDictionary` phải duyệt bằng `IDictionaryEnumerator` (`Cast<DictionaryEntry>` ném InvalidCast).
4. Liveness: `Process.Exited` chỉ khi 1 instance; thêm phát hiện COM disconnect trên mọi call.
5. Mô tả `execute_etabs_code` 1 704 ký tự (plan ghi ≤ 1 500; test cap 1 800 như AutoCAD).

## Cho phase 2
- Snapshot: chuẩn hoá tên (`.$et` → `.EDB`), copy `.EDB` (sidecar không cần), `Save()` ~1 s/74 KB; "no file path" = không rooted / "(Untitled)".
- Fingerprint: 8 `GetNameList` + `GetStories_2` (10 tham số) — tất cả ret 0.
- E10 cần model có kết quả để chốt "unlock xoá kết quả" (mục `[chưa xác minh trong CHM]` giữ nguyên).
- Busy: dialog không chặn OAPI → giữ pre-check, không đầu tư thêm; `expireWithoutTicks` che trường hợp call kẹt.
- Multi-instance: `GetObject` chọn instance mới nhất → cảnh báo là đúng; cân nhắc "prefer instance có model" (`GetObjectProcess`) — ngoài MVP.

## Test / gate
| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPEtabs/HPEtabs.slnx -c Debug` | ✅ 0 warning 0 error |
| `-p:EtabsInstallDir=C:\nope\` | ✅ đúng 1 lỗi có hướng dẫn |
| `HPEtabs.Mcp.Server.Tests` | ✅ 17/17 (không ETABS) |
| `HPEtabs.McpBridge.Tests` | ✅ 26/26 (cần ETABS) |
| `tools/list` qua stdio (registry cách ly) | ✅ 12 tool; không bridge → hint "Start HPEtabs.McpBridge.exe…" không path máy |
| Harness `disabled` + `detached` | ✅ 2/2 + 15/15 (Debug ×2, publish ×1) |
| Harness `spike` (attached) | ✅ 6/6 với model mở; `nomodel` 1/1, `modal` 1/1 (quan sát), `closed` 1/1 |
| Probe E9/E10/E14 | ✅ chạy, log |
| grep reference cấm / bypass / `McpShared` diff | ✅ 0 / 0 / rỗng |

## Review round (2026-09-17, sau `code-review-phase-01.md` 6.5/10 fix-first)
Đã sửa toàn bộ 14 finding trừ #13 (probe/nút spike gỡ ở phase 2 — đã đổi nhãn, bỏ mã E khỏi UI/log):

| # | Sửa | Ở đâu |
|---|---|---|
| 1 Critical | Gate **fail-closed**: `sapModel`/`etabs` ngoài chuỗi `X.Member(...)` (alias/cast/`?.`/lambda/đối số/so sánh) = D; sub-object OAPI lấy làm giá trị (`var fo = sapModel.FrameObj;`, `etabs.SapModel`, method group) = D, thông báo nêu dạng duy nhất được nhận | `EtabsTierGate.Inspect/Record` |
| 2 High | Mọi thứ dưới `sapModel.Results` (kể cả `Results.Setup`) = R trừ tên D | `ReadOnlyReceivers` |
| 3 High | Prefix D chỉ áp cho receiver bắt nguồn từ global; receiver khác chỉ tên D chính xác | `Record` |
| 4 | `(Untitled)` → docTitle/docPath/openDocs null; `isModifiable = quiescent && hasFile` | `EtabsContextReader.ModelFile` |
| 5 | `IsQuiescent` bỏ qua HWND đã huỷ (`IsWindow`) | `EtabsExecutor` |
| 6 | Catch thường + `DetachIfGone` ngoài exception filter | `EtabsExecutor.OnWorker` |
| 7 | `.$et` → `.EDB` khi công bố model | `EtabsContextReader.ModelFile` |
| 8 | Mô tả tool: bỏ `AnalysisResults*`, nêu "all of sapModel.Results" + quy tắc chuỗi `sapModel.X.Member(...)` (1 7xx ký tự, cap 1800) | `ExecuteEtabsCodeTool` |
| 9 | `_attached = true` trước `Watch`; ETABS chết lúc attach → "ETABS exited while the bridge was attaching" | `EtabsAttachment.Attach` |
| 10 | `_destructiveEnabled` volatile | `EtabsExecutor` |
| 11 | E12 assert docTitle/docPath/openDocs rỗng + isModifiable false + read chạy; E15 assert busy ≥ 8 s **hoặc** read xong < 8 s | `live-verify.py` |
| 12 | `Use-StartedBridge` + `stop` kiểm `ProcessName -eq HPEtabs.McpBridge` | `spike-step.ps1` |
| 14 | Row `AnalysisResultsSetup` (không có trong wrapper — chỉ `Results.Setup`) → `DesignResults.GetDesignResultsAvailable`; `AttachWarning` xoá khi Detach | tests, VM |

Test mới: `Globals_used_outside_a_plain_member_chain_fail_closed_as_destructive` (7 row), `Oapi_sub_objects_taken_as_values_fail_closed_as_destructive` (4), `Everything_under_Results_reads` (4), `Bcl_members_on_other_receivers_are_not_classified` (5); `EditGeneral.Move` = W (không phải D — prefix `Move` không có trong danh sách).

| Kiểm tra sau sửa | Kết quả |
|---|---|
| `dotnet build HPEtabs.slnx -c Debug` | ✅ 0 error |
| `HPEtabs.McpBridge.Tests` | ✅ **46/46** |
| `HPEtabs.Mcp.Server.Tests` | ✅ 17/17 (mô tả ≤ 1800) |
| Harness `disabled` + `detached` | ✅ 2/2 + 15/15 (×2: `fixes`, `fixes2`) |
| Harness `nomodel` (attach pid 49336, ETABS đang giữ "(Untitled)") | ✅ 1/1 với assert thật |
| Harness `spike` E17/E13/E13b/T1 | ✅ 5/5; **E18 FAIL do môi trường** — ETABS lúc chạy không có model (`GetModelFilename` = "(Untitled)"), cần mở lại model rồi chạy lại |
| `modal` / `closed` | ⏸ chưa chạy lại sau sửa (cần thao tác ETABS) |

Gotcha PS 5.1: file `.ps1` không BOM đọc theo ANSI → em dash `—` (0xE2 0x80 0x94) thành `"` (0x94) và phá parse; dùng ASCII trong chuỗi PowerShell.

## Chưa làm / gap
- `.mcp.json` entry `hprebar-etabs` (user thêm). E10 với model có kết quả. File dialog native (Open) chưa thử. Multi-instance chọn đúng instance. File `.hpetabs-e9-probe.*` trong thư mục model của user — xoá tay.
