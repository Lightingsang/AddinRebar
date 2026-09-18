# Phase 2 — bridge runtime hoàn chỉnh, MirrorTests, harness pipe + ribbon (2026-09-18)

Bundle Debug deploy vào `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\`, Civil 3D 2026 trên máy dev. Log: `HPCivil3d/output/live-verify/bridge-unattended-run{1..4}.log`, `ribbon-check-run{1,2}.log`, screenshot `HPCivil3d/output/ribbon-check/ribbon-tab{,-theme0,-theme1}.png`.

## Thay đổi
| Việc | Kết quả |
|---|---|
| Serializer Civil | `AlignmentSubEntity` → `{type, subEntityType, startStation, endStation, length, start{x,y}, end{x,y}}`; `AlignmentEntity` → `{type, entityType, entityId, subEntityCount (+ stations/length khi là AlignmentCurve)}`; `CogoPoint` (Entity) thêm `{number, name, description, x, y, elevation}`; `StyleBase` (DBObject) thêm `name`; namespace check `Autodesk.` qua **token**. Alias type thay import namespace (`Autodesk.Civil.DatabaseServices` có `Entity`/`DBObject` riêng → `case Entity` mơ hồ). Exception Civil đã đúng `{Type}: {Message}` qua nhánh chung |
| Units | `Civil3dUnitTable` (thuần, link vào test project): Meters 1000 / Feet 304.8, INSUNITS fallback, `insunitsMismatch` tương đối 1e-4 (US survey feet = feet), `"."` = không zone; `Civil3dUnits` chỉ còn đọc settings |
| Description (review M4) | `execute_civil3d_code`, `Civil3dHostProfile.ScriptContractSummary`, `get_civil3d_context`, README: `civil` luôn có; `DrawingUnits` mặc định **Feet** khi DWG không có Civil settings; đọc `insunitsMismatch` trước khi ghi toạ độ |
| Cửa sổ (review L7) | 6 literal "AutoCAD" → "Civil 3D" (title chip, checkbox, tooltip, ghi chú U, auto-start, cache note) — qua token |
| MirrorTests (review M2/L13) | Quy ước chốt trong `tools/mirror-tokens.json` `note`: token **theo thứ tự**; block `civil-only` chỉ **thêm** dòng; `versionPatterns` thay version bằng placeholder; `civilOwnedFiles` không so (BridgeEntry, SelfCheck, csproj, PackageContents.xml, launchSettings.json, Civil3dUnits, Civil3dDocumentAccess, Civil3dUnitTable). 21 file mirrored = AutoCAD sau token + strip. Runner giữ dòng `var units = AutocadInsunits.For(...)` rồi block Civil gán lại; ctor globals nhận `civil` qua token `Transaction tr, ScriptUnits units,`; context reader giữ dòng `Units` AutoCAD rồi block ghi đè |
| `HPCivil3d.McpBridge.Tests` (NEW, net10, xunit v3, **không** ref AutoCAD/Civil) | **47** test (41 + review: AutoCAD-side coverage + 5 pin sha256 `ownedCounterparts`): 21 mirror (Theory per file) + balanced blocks + mọi file bridge có quyết định + civil-owned tồn tại + mọi token có trong nguồn AutoCAD + token unique + normalize; `Civil3dUnitTableTests` 13. **Mutation:** đổi 1 khoảng trắng trong `Civil3dScriptRunner.cs` ngoài block → đúng 1 fail "drifted … line 141: AutoCAD `…` vs Civil `…`" |
| Harness pipe (NEW, copy AutoCAD + token) | `run-bridge-unattended.ps1` + `pipe-scenarios.py` + `bridge.scr`: 20 check AutoCAD + **C1–C9 Civil** + ESC/retry tự động (spike chứng minh PostMessage huỷ LINE) = **31 check**; mở `Align-7C.dwg` (copy) qua COM; exit code = số bước fail |
| Harness ribbon (NEW, copy) | `run-ribbon-check.ps1` (pwsh 7): 12 check + 1 MANUAL; Civil cần **kiên nhẫn COM** hơn AutoCAD (workspace switch tải lại Toolspace → `RPC_E_CALL_REJECTED` hàng chục giây): retry 12 × 5 s + chờ 20 s sau khi trả workspace |
| Harness chung | `Stop-Acad` graceful (COM đóng mọi drawing không lưu + `Quit`, pid-guard, 20 s grace, kill khi từ chối) dùng cho spike/pipe/ribbon (review L12); `Set-OptIn` retry 3 × poll 3 s (toggle ngay sau toggle bị nuốt — run 2 pipe) |

## Kết quả live
| Run | Kết quả |
|---|---|
| Pipe run 1 (09:43) | **31/31** — disabled 1, main 27 (20 AutoCAD + C1…C9 — C5 COGO commit + `U` count 0→1→0), nodoc 1, busy 2 (`-32002` sau 8.0 s, ESC PostMessage, retry `value 2`) |
| Pipe run 2 (09:51) | harness: `Set-OptIn $true` ngay sau untick đọc lại False → bỏ qua main; wrapper cũ exit 0 → sửa retry + exit code |
| Pipe run 3 (10:0x) | **31/31**, `harness steps failed: 0` |
| Pipe run 4 (10:13) | **31/31** — với `Stop-Acad` graceful: `acad pid 104436 quit gracefully after 19 s` (grace nâng lên 45 s), không Drawing Recovery, không `.dwl` (sweep cần `-Force`: lock file là Hidden) |
| Pipe run 5 (10:17) | **31/31** — sau review fix (serializer `number` null, wording), graceful quit 18 s |
| Ribbon run 1 (09:46) | 10/12 + 1 MANUAL — 2 FAIL COLORTHEME vì COM `RPC_E_CALL_REJECTED` sau workspace round trip (4 × 4 s không đủ với Civil) |
| Ribbon run 2 (09:51) | **12/12 + 1 MANUAL** — tab `HPCivil3d` đúng một lần, còn một sau workspace `Civil 3D` → `Drafting & Annotation` → về, COLORTHEME 0→1→0 với loader "created" 3→4→5, nút mở cửa sổ, click 2 không mở thêm, loader.log 0 lỗi. Icon (MANUAL): xem cả 2 screenshot — window-with-plug, ink sáng trên theme 0 / tối trên theme 1, plug xanh, sắc nét |

| Ribbon run 3 (10:21) | **12/12 + 1 MANUAL** — sau review fix (restore-refused → kill; env cleanup), graceful quit 20 s |
SECURELOAD: mỗi start 4 × *Load Once* (harness) — như phase 1.

## Bằng chứng serializer (run 1/3)
- C2 `{"handle":"10DB5","type":"Alignment","layer":"C-ROAD","dxfName":"AECC_ALIGNMENT","name":"Alignment - (1)"}`
- C3 `e = {"type":"AlignmentLine","entityType":"Line","entityId":1,"subEntityCount":1,"startStation":0,"endStation":316.81,"length":316.81}`, `s = {"type":"AlignmentSubEntityLine","subEntityType":"Line",…}`
- C4 `{"type":"CogoPoint","dxfName":"AECC_COGO_POINT","number":1,"description":"MCP harness","x":1,"y":2,"elevation":12.5}` — dryRun rolled back, count 0→0
- C8 `PointNotOnEntityException: Point Outside Surface.` · C9 `{"handle":"1024B","type":"AlignmentStyle","name":"Local Road"}`

## Chưa làm / giới hạn
- Context < 100 ms chưa đo riêng (harness đo tổng; context trên Align-7C trả trong < 1 s).
- `Profile.Name` qua `Feature` (U6) chưa đọc live — phase 3.
- Harness AutoCAD vẫn bấm *Always Load* im lặng (ngoài phạm vi plan này).
