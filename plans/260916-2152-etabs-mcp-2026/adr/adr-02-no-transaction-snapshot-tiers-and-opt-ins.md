# ADR-02 — ETABS không có transaction/undo → snapshot `.EDB` vô điều kiện + ba tier R/W/D (allow-list) + hai opt-in

**Ngày:** 2026-09-16 · **Revised 2026-09-16 (red-team #2, #3, #4, #5, #6, #8, #14c/d/e):** R = allow-list thay prefix; bảng tier từ CHM; snapshot vô điều kiện, trong budget, tên file; preview = `isError` + `PREVIEW`; D-off = `-32001`; drift R không phải lỗi; `-presave` có điều kiện; path policy trên `args`; không pid picker · **Status:** Proposed · **Owner:** HPEtabs
**Kế thừa:** [Navis ADR-02](../../260915-0824-navisworks-mcp-2026/adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) · [Navis ADR-04 §3 heavy gate](../../260915-0824-navisworks-mcp-2026/adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md)
**Bằng chứng:** [evidence §E6, §E8](../research/evidence-on-machine-2026-09-16.md) (CHM index 1 707 topic; trích CHM) · [researcher-01 #10, #11, #13→sửa, #17, #24, §5, §6](../research/researcher-01-etabs-oapi-facts.md) · [red-team 2026-09-16](../reports/red-team-2026-09-16.md) hàng 2–6, 8, 14 · `HPNavis/HPNavis.McpBridge/Service/NavisHeavyGate.cs:17–60` · `McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs:30–31` · `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.cs:115–117` (`VerifiedRunId` chỉ khi `!IsError`) · `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs:64` (JSON-RPC error → `BridgeErrorException`) · `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolManager.cs:178–186` (chỉ bắt timeout → `-32001` không thành run)

## Context

- **Không transaction, không undo API:** CHM › "Information for Plugin Developers" § "Plugin Considerations — General" (researcher-01 #17); không topic `*Undo*`/`*Rollback*` (E8). Mỗi call ghi là vĩnh viễn trong bộ nhớ ETABS.
- **Bề mặt ghi rộng hơn prefix `Set/Add/…`** (red-team #2, CHM index 1 281 method): `ExportFile`, `GetTableForDisplayCSVFile` (Get* mà ghi file), `EditGeneral.Move`, `StartDesign`, `StartSlabDesign`, `ModifyUndeformedGeometry*`, `MergeAnalysisResults`, `ShowTablesInExcel`, `StartDetailing/ClearDetailing`, `ResetOverwrites`, `RenameTower` → deny-list theo prefix bỏ sót; `Count()` không tồn tại trên `cCombo`/`cStory`, `Count(...)` có tham số.
- **Lock:** CHM › "cSapModel.SetModelIsLocked Method" (E8); unlock xoá kết quả `[chưa xác minh trong CHM]` → spike E10. **File:** CHM › "cFile.Save Method" — `Save("")` = tên hiện tại; `Save(path)` save-as? `[chưa xác minh]` → E9. **Analysis:** CHM › "cAnalyze.RunAnalysis Method" đồng bộ, cần file path, không cancel.
- `ExecuteResult.RolledBack` (`ExecuteResult.cs:30`) chỉ đúng khi **không chạy gì**; `ExecuteResult.IsError` được registry tính là fail (`ToolManager.cs:186`) → mọi "từ chối" không phải lỗi của tool phải là **JSON-RPC error**, không phải `ExecuteResult`.

## Decision

### 1. Ba tier từ **bảng allow-list** (không prefix), phân loại tĩnh (semantic) + kiểm động

- **Fixture** `HPEtabs/HPEtabs.McpBridge/Resources/etabs-oapi-tiers.txt` (`cInterface.Member<TAB>R|W|D`, EmbeddedResource) sinh **một lần** từ CHM index (E6: topic `c*.X Method`/`Property` + tham số đọc từ topic HTML) bằng script `HPEtabs/tools/generate-oapi-tier-fixture.py` (không chạy trong build); là dữ liệu dẫn xuất, không phải binary CSI. Test `EtabsTierFixtureTests`: **mọi** topic `c*.X Method` trong index có dòng trong fixture; không dòng trùng; mọi tier ∈ {R,W,D}.
- **Quy tắc sinh** (thứ tự): (a) member có tham số path-like (`FileName|csvFilePath|SourceFileName|FilePath|Path`) → **D**; (b) tên khớp `Start*|Modify*|Merge*|Reset*|Clear*|Rename*|Show*|Export*|Import*|Replicate*` → **D**; (c) explicit D: `SetModelIsLocked, RunAnalysis, DeleteResults, CreateAnalysisModel, File.Save, File.OpenFile, File.New*, InitializeNewModel, DatabaseTables.ApplyEditedTables, *.Delete*` → **D**; (d) allow-list R: `Get*`, `Count`, `Is*`, `Has*`, `RefreshView`, `GetAvailableTables`, `GetTableForDisplayArray`, mọi member `cAnalysisResults.*`, `cAnalysisResultsSetup.*` → **R**; (e) còn lại trên `ETABSv1` → **W**. Không có trong fixture → **D**. Property điều hướng (`sapModel.FrameObj`) không phân loại.
- **`EtabsTierAnalyzer`** (bridge, semantic): `CompileOutcome.Script.GetCompilation()` (`McpShared/HPRebar.McpBridge.Core/Scripting/ScriptCompiler.cs:13`) → `SemanticModel`; mọi invocation/member access có receiver bind về type namespace **`ETABSv1`** (CHM in "Namespace: ETABSv1") → tra fixture; receiver không bind được (alias hỏng, `dynamic` đã cấm) → **D**. Tier script = max. `list.Add(x)` trên `List<T>` không bind ETABSv1 → bỏ qua.

| Tier | Request hợp lệ | Trước khi chạy | Sau khi chạy |
|---|---|---|---|
| **R** | `transaction:none` (mặc định seed R) / `dryRun:true` | không snapshot | fingerprint (§2) khác → `changed` + dòng `Logs` "model changed during a read-only run (another writer?)"; **`isError=false`** (red-team #5) |
| **W** | `transaction:auto` (`manual` ≡ `auto` + log) | audit `started` → snapshot vô điều kiện (§3) trong budget → chạy | throw → `rolledBack:false`, `Message` nêu `Snapshot` (tên file); `Changed` add/delete |
| **D** | `transaction:auto`; `dryRun` refused | checkbox "Allow destructive operations" OFF → **JSON-RPC `-32001`** `new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, "Destructive operations are disabled — tick 'Allow destructive operations' in the HPEtabs MCP Bridge window")` **trước** khi chạy, không `ExecuteResult` (→ `BridgeErrorException` `RevitBridgeClient.cs:64`, không thành run `ToolManager.cs:178–186`); ON → path policy (§4) → audit `started` + `[destructive]` → snapshot → chạy; ceiling `HostScriptContracts.EtabsHeavyMaxTimeoutSeconds = 600` | như W |

**Ma trận `transaction` × `dryRun`:**

| request | R | W | D |
|---|---|---|---|
| `none`, dryRun=false | chạy | **static preview** | static preview |
| bất kỳ, dryRun=true | chạy (đọc) | static preview | static preview (không "refused up front" riêng — cùng một preview) |
| `auto`/`manual`, dryRun=false | chạy như R | snapshot → chạy | ON: snapshot → chạy; OFF: `-32001` |

- **Static preview** = `ExecuteResult{ IsError=true, RolledBack=true, Diagnostics=[ScriptDiagnostic(line,col,"PREVIEW","would call cFrameObj.SetSection (W); cAnalyze.RunAnalysis (D) — nothing ran; send transaction:auto, dryRun:false")] }`. Hệ quả (red-team #4): `test_tool` (dryRun) trên tool W/D **fail** → không `tested` (`ToolLifecycleService.cs:115–117`); tool W chỉ `tested` qua `test_tool realRun=true` (có snapshot); review note ghi "executed" / "preview only".
- **`etabs.analyze`** từ chối proposal khai `transaction:none` mà tier ≥ W: `GuardViolations` += `PREVIEW` "declared transaction:none but calls W/D members" → validator lỗi (`ToolValidator.cs:68`). Cơ chế (phase 0, dẫn xuất từ red-team #4): `AnalyzeRequest` thêm `string? Transaction = null` (additive), `ToolLifecycleService.cs:50` truyền `candidate.Transaction`; bridge cũ bỏ qua field.
- **Exception / timeout / cancel:** R → `isError`; W/D → `isError`, `rolledBack:false`, `Snapshot`; timeout luôn fail nhưng worker bận tới khi call OAPI trả (ADR-04 §3); snapshot **trong** budget (§3).

### 2. Fingerprint & `Changed` — add/delete only

`EtabsFingerprint.Take(sapModel)`: tập tên qua `GetNameList(ref int count, ref string[] names)` trên `PointObj`, `FrameObj`, `AreaObj` (`[chưa xác minh]` chữ ký — spike E13b), + `LoadPatterns`, `LoadCases`, `RespCombo`, `PropFrame`, `PropMaterial`, `Story` (`GetNameList` `[chưa xác minh]` từng cái; thiếu → bỏ receiver đó, ghi report), + `GetModelIsLocked()` (CHM › "cSapModel.GetModelIsLocked Method"), `GetModelFilename()`. `Changed.Added` = tên mới, `Deleted` = tên mất, **`Modified` luôn 0** — mô tả tool: "Changed counts additions/deletions only; a Set* on an existing object is not counted". Không dùng `Count()`.

### 3. Snapshot — vô điều kiện cho W/D, trong budget, tên file

Thư mục `%LocalAppData%\HPEtabs\McpBridge\snapshots\<modelName>\`; **hai bucket**: `presave\` giữ 5, `prerun\` giữ 10 (xoá cũ nhất theo mtime).
1. Đồng hồ budget bắt đầu **trước** bước 2 (red-team #6): hết `timeoutSeconds` trước khi script chạy → `TimedOut=true`, script không chạy, `Message` "timed out while saving the model for the snapshot; nothing ran".
2. Tiền điều kiện: `path = GetModelFilepath()+GetModelFilename()` khác rỗng, `File.Exists`; rỗng → JSON-RPC `-32003` (`new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "model has no file path — save it in ETABS first")`); **UNC (`\\…`) → `-32003`** "model is on a UNC share — copy it locally first" (mapped drive: chỉ log). Cả hai là trạng thái môi trường, không phải lỗi tool → không thành run (cùng nguyên tắc red-team #5).
3. Audit `started` (mọi tier ≥ W) ghi label + tier + "forced save" **trước** khi ghi đĩa.
4. **`-presave`** chỉ khi file trên đĩa **không** do bridge ghi lần cuối (`EtabsSnapshotManager` nhớ `(mtime,size)` sau mỗi `Save()` của mình; khác → user đã save → copy `presave\<yyyyMMdd-HHmmss>-<label>-presave.EDB`). Mỗi lần user save tối đa 1 bản.
5. `sapModel.File.Save()` (CHM › "cFile.Save Method"); `ret≠0` → `isError` "ETABS returned {ret} from File.Save — snapshot impossible, nothing ran".
6. Copy `path` → `prerun\<yyyyMMdd-HHmmss>-<label>.EDB`. Label sanitize `[A-Za-z0-9_-]{1,40}` (khác → `_`), reject `..`, assert `Path.GetFullPath(dest).StartsWith(bucketDir)`.
7. Chạy script. `ExecuteResult.Snapshot` = **tên file** (`20260916-2231-draw_frame.EDB`), không thư mục (username không lên wire — `ResultFormatter` không strip). Thư mục hiện ở cửa sổ + audit; harness suy từ ADR-05.
- Không opt-out (`snapshot:false` bỏ — red-team #3). Không checkbox thứ ba: forced save nêu trong mô tả tool + text cạnh checkbox "Allow AI code execution" ("writing scripts save the model first").
- Giới hạn nói thẳng: snapshot ≠ undo (restore = mở lại file, mất kết quả chưa lưu); chỉ `.EDB` (sidecar `<model>\`, `.$et`, `.LOG` không copy); `Save()` ghi đè file user bằng trạng thái bộ nhớ (có `-presave`); model lớn → `Save()` tốn giây **trong** budget; `Save()` bật dialog? `[chưa xác minh]` (E11).
- Restore = seed tuỳ chọn `restore_model_snapshot` (D) phụ thuộc E9; không MVP.

### 4. Path policy — mọi member path-taking, literal + `args` lúc chạy

Áp dụng cho **mọi** member tier ≥ W có tham số path-like (fixture đánh dấu `path`): (a) tĩnh: biểu thức đối số path phải là **string literal** hoặc `args.Str("k")`/`args.Require("k")` (semantic) — khác (ghép chuỗi, biến, interpolation) → refused `DESTRUCTIVE` "path arguments must be a literal or args.Str(\"key\")"; (b) tĩnh: literal UNC (`\\`, `//`), `HPEtabs\McpBridge|McpServer`, `\Computers and Structures\` → refused; chỉ dưới thư mục model hiện tại hoặc `%LocalAppData%\HPEtabs\` (`GetFullPath` prefix, không phân biệt hoa thường); (c) **động, lúc chạy** (red-team #14e): nếu script có member path-taking, **mọi** string value trong `args` được screen theo (b) trước khi chạy → refused. Không có gì khác qua được.

### 5. Attach policy, mất kết nối, licensing

- `Helper.GetObject("CSI.ETABS.API.ETABSObject")` **duy nhất** (CHM › "Attaching to a Manually Started Instance"); **không pid picker** (red-team #13): `Process.GetProcessesByName("ETABS").Length > 1` → cửa sổ + context warning "more than one ETABS is running — pick the instance in ETABS: Tools › Active Instance for API, then Attach". Không `CreateObject*`/`StartAPIWrapper`/`ShellOut*` (guard cấm identifier `Helper` — ADR-04 §5).
- Liveness eager (ADR-04 §1): `Process.Exited`/`HasExited` → `Attached=false` → mọi request `-32003` "ETABS not attached — click Attach in the HPEtabs MCP Bridge window" (`new BridgeRequestException(BridgeErrorCode.NoActiveDocument, …)`, ctor public `BridgeRequestException.cs:13`); Attach lại **không cần restart** bridge.
- AI làm việc trên model sống: R mặc định, snapshot vô điều kiện, hai opt-in, audit, cửa sổ "attached to pid N — <model>".
- Licensing: CHM › "Information for Plugin Developers" "no license … beyond having a valid license for ETABS"; attach không tốn seat `[chưa xác minh]`.

### 6. Điều gì KHÔNG hứa
Không rollback in-flight; không undo; preview không chạy gì; `Changed` add/delete only; `RunAnalysis` không ngắt được; snapshot chỉ `.EDB`; UNC model không ghi được; > 1 ETABS → user chọn trong GUI.

## Alternatives rejected
- **Tier theo prefix** (bản sáng 2026-09-16): bỏ sót `ExportFile`/`Start*`/`Modify*` (red-team #2 Critical) — loại.
- **`snapshot:false` opt-out / `ExecuteRequest.Snapshot`:** không đường set (`ExecuteCodeService.cs:68`, `ToolManager.cs:174`) và AI tắt được lưới duy nhất (red-team #3) — loại.
- **Preview `isError=false`:** đánh `tested` tool chưa chạy (red-team #4) — loại.
- **D-off/drift = `ExecuteResult.IsError`:** quarantine oan (red-team #5) — loại.
- **Bỏ `-presave` hoàn toàn** (SC): lưới cho dữ liệu user — **reject** (giữ có điều kiện). **Checkbox thứ ba** (SA): **reject** — mô tả + text cạnh checkbox 1.
- **dryRun = chạy rồi restore:** `OpenFile` mất kết quả, không atomic — loại.

## Consequences
- Contracts additive: `ExecuteResult.Snapshot : string?` (tên file), `AnalyzeRequest.Transaction : string?`, `HostScriptContracts.EtabsHeavyMaxTimeoutSeconds = 600`. **Không** `ExecuteRequest.Snapshot`.
- `GuardProfile.Etabs`/`AnalyzerProfile.Etabs`: ADR-04 §5. W/D không nằm trong guard.
- Diagnostic id mới host-side: `PREVIEW`, `DESTRUCTIVE` (path policy khi checkbox ON hoặc member path-taking W).
- Mô tả `execute_etabs_code` (phase 1): 3 tier + allow-list + preview + snapshot vô điều kiện + forced save + `Changed` add/delete + `-32001` cho D-off + `ret` convention.

## Open items `[chưa xác minh]` → spike phase 1
E9 `Save(path)` save-as; E10 unlock xoá kết quả (`GetCaseStatus`); E11 `Save()` dialog; E12 no-model probe; E13b chữ ký `GetNameList` từng receiver; mã `COMException` khi ETABS đóng; số member CHM index không phân loại được tự động (kỳ vọng 0 sau review tay).
