# ADR-04 — Transaction/dryRun kế thừa AutoCAD nguyên vẹn; rebuild/data-shortcut/survey/UI/file-taking bị guard chặn trong MVP; spike chứng minh abort với Civil object

**Ngày:** 2026-09-17 · **Status:** Proposed (S-05 phase 1 quyết phần rebuild) · **Owner:** HPCivil3d
**Kế thừa:** [AutoCAD ADR-03 + "Revised after phase 2"](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-03-autocad-transaction-undo-dryrun-policy.md) (outer/inner qua `doc.TransactionManager`, script không mở transaction, `Changed` từ HANDSEED + `ObjectOpenedForModify`, undo gộp `HPMCP`) · [Navis ADR-04 §3 heavy gate](../../260915-0824-navisworks-mcp-2026/adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) · [ETABS ADR-02 §4 path policy](../../260916-2152-etabs-mcp-2026/adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md)
**Bằng chứng:** [addendum §6–7, §11, §12 U4](../research/reflection-addendum-verified-signatures.md) · [E17](../research/evidence-on-machine-2026-09-17.md) · [researcher-02 §4, §7](../research/researcher-02-civil3d-autoloader-launch-rebuild-facts.md) (KB Autodesk "corridor disappears when rebuilt", "stuck in a loop to Rebuild") · `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:22–45` (`Autocad`), `AnalyzerProfile.cs:14–16`.

## Context
- Civil object là `DBObject` trong cùng `Database` (addendum §4: `Civil Entity : Aec Entity : AutoCAD Entity`; `CogoPoint : AutoCAD Entity`). Mô hình AutoCAD đã verified live: `outer` (vai TransactionGroup) + `inner = tr`; `dryRun`/lỗi/timeout/`none` → `outer.Abort()` huỷ mọi thứ kể cả nested đã commit. Về nguyên lý áp dụng nguyên cho Civil — **nhưng** Civil có ba thứ AutoCAD không có:
  1. **Rebuild** dài, không hợp tác với `ct`, và KB Autodesk ghi corridor "biến mất"/"lặp rebuild" (researcher-02 §4/§7); `Corridor.Rebuild()`, `CorridorCollection.RebuildAll()`, `Surface.Rebuild()`/`RebuildSnapshot()`, setter `RebuildAutomatic`/`AutoRebuild` (E17).
  2. **Trạng thái ngoài DWG:** data shortcuts (working folder/project folder là per-user/per-machine — `DataShortcuts.SetWorkingFolder/SetCurrentProjectFolder/CreateProjectFolder/AssociateDSProject`), survey database (`CivilApplication.SurveyProjects`, `SurveyProject*`) — `Abort()` không hoàn lại.
  3. **API nhận đường dẫn file** (`ExportToDEM`, `CreateFromLandXML`, `CreateFromTin`, `CreateFromIMX`, `CreateSolidsAtFixedElevationToFile`, `StyleBase.ExportTo(Database…)`) — vượt deny `System.IO` (cùng lỗ ETABS/Navis đã bịt bằng path policy/heavy gate).
  4. **Dialog:** `AeccUiMgd` (`Autodesk.Civil.AeccUiMgd.*`), COM `Autodesk.AECC.Interop.*`, label-style editors.
- Dependency chain (alignment → profile → corridor → corridor surface): tạo/sửa alignment có thể kích rebuild ngầm nếu `RebuildAutomatic` bật — `Abort()` có hoàn lại kết quả rebuild không → `[chưa xác minh]` (S-05).

## Decision

### 1. Transaction policy = AutoCAD, không đổi một dòng
`Civil3dScriptRunner` = copy `AutocadScriptRunner` (mirror test). `auto|manual(≡auto + log)|none`, `dryRun` → `outer.Abort()`, timeout luôn fail + abort, `LockDocument(ProtectedAutoWrite, "HPMCP")`, `Changed` từ HANDSEED/`ObjectOpenedForModify`/`IsErased`. Description tool nói "U reverts the AI's runs since your last command" (undo gộp).

### 2. `GuardProfile.Civil3d` (phase 0) = **superset** của `Autocad`
```csharp
public static readonly GuardProfile Civil3d = new GuardProfile("Civil 3D",
    deniedIdentifiers: Autocad.DeniedIdentifiers + ["DataShortcuts", "SurveyProject", "SurveyProjectCollection"],
    deniedMembers: Autocad.DeniedMembers + [
        // long, non-cooperative rebuilds with a known corruption history — closed in the MVP, S-05 may reopen under auto
        "Rebuild", "RebuildAll", "RebuildSnapshot",
        // state that lives outside the drawing (per-user working folder, project, survey db) — Abort() cannot undo it
        "SetWorkingFolder", "SetCurrentProjectFolder", "CreateProjectFolder", "AssociateDSProject", "CreateReference",
        "CreatePartialReferenceSurface", "UpdatePartialReferenceSurface", "RepairBrokenDRef",
        "CreateDataShortcutManager", "SaveDataShortcutManager", "SurveyProjects",
        // members that take a file path (bypass the System.IO deny)
        "ExportToDEM", "ExportTo", "CreateFromLandXML", "CreateFromTin", "CreateFromIMX", "CreateSolidsAtFixedElevationToFile"],
    deniedMembersOnIdentifier: Autocad.DeniedMembersOnIdentifier,          // tr.Commit / Abort / Dispose
    deniedNamespaces: Autocad.DeniedNamespaces + ["Autodesk.Civil.DataShortcuts", "Autodesk.Civil.AeccUiMgd", "Autodesk.AECC.Interop"]);
```
Cách viết `Autocad.X + [...]` là ý — phase 0 hiện thực bằng cách nối mảng (GuardProfile hiện không expose các list; nếu cần thì thêm getter additive hoặc lặp literal như `Navis` đã làm). Danh sách chốt ở phase 0 sau khi probe thêm `Feature`/`LabelSetStylesRoot` (U6/U8); nguyên tắc: **chỉ member name có trong bảng addendum §6–7, §11**, không đoán tên. **Không** deny: `ImportLabelSet` (đổi label set theo tên style trong DWG, undo được), `RebuildAutomatic`/`AutoRebuild` (guard chặn theo tên member nên chặn cả getter mà seed đọc; setter đổi setting persist trong DWG, undo được, không kích rebuild ngay). Base deny-list (`System.IO/Net/Reflection/Process`, `Expression`/`Delegate`, `await/Task/Thread/dynamic/unsafe`, `#r/#load`, `global::`) tự áp dụng.
`AnalyzerProfile.Civil3d` = copy `Autocad` (`StartTransaction`, `StartOpenCloseTransaction`).

### 3. Rebuild — MVP chặn, đường mở có điều kiện
- MVP: không seed nào rebuild; corridor/surface chỉ **đọc** (`IsOutOfDate`, `RebuildAutomatic`, `AutoRebuild` đọc được — xem §2).
- S-05 (phase 1, drawing `Corridor-*.dwg` copy): (a) `Surface.Rebuild()` dưới `auto` rồi `U`; (b) `TinSurface.AddVertices` (không Rebuild) dưới dryRun → `Abort()` → surface về nguyên (so `GetGeneralProperties().NumberOfPoints`); (c) `Corridor.Rebuild()` dưới dryRun → `Abort()` → `IsOutOfDate`/hiển thị còn nguyên? thời gian? — kết quả quyết định phase 2: nếu (c) sạch và < 120 s trên sample → phase 2 mở `Rebuild` **chỉ dưới `auto`** với pre-pass như Navis heavy (`REBUILD` diagnostic khi `none`/dryRun), timeout vẫn 120 s (không thêm opt-in thứ hai — YAGNI cho MVP; heavy tier là follow-up nếu user cần corridor thật). Nếu (c) hỏng → giữ deny, ghi known gap.

### 4. Không có path policy riêng
Khác ETABS: mọi member nhận path đã deny (§2) → không cần `EtabsPathPolicy` tương đương. `args` string dạng path không bị soi (không có sink nhận nó). Nếu phase 2 mở `CreateFromLandXML` cho import → lúc đó thêm policy (follow-up).

### 5. Lỗi Civil → message AI đọc được
`Autodesk.Civil.CivilException`, `EntityNotFoundException`, `PointNotOnEntityException`, `SurfaceException` (addendum §11): serializer message = `"{TypeName}: {Message}"` + `SafeText.StripPaths` — như `Autodesk.AutoCAD.Runtime.Exception` `"{ErrorStatus}: {Message}"` hiện tại (`AutocadResultSerializer`). Seed bắt per-item (`FindElevationAtXY` ngoài biên → `ok:false, error`), không để một điểm hỏng fail cả run.

## Alternatives rejected
- **Heavy tier thứ hai (checkbox "Allow rebuilds", 600 s) như Navis** ngay MVP: chưa có bằng chứng cần; thêm UI + `MaxTimeoutSeconds` 600 + audit `[heavy]`. Để sau S-05.
- **Snapshot DWG trước W như ETABS:** AutoCAD có `Abort()` + Undo thật; không cần.
- **Cho script mở transaction riêng (`manual` thật):** AutoCAD ADR-03 revised đã loại (finalizer crash acad.exe) — Civil cùng process, cùng rủi ro.

## Consequences
- Phase 0: `GuardProfile.Civil3d`, `AnalyzerProfile.Civil3d` + test (cấm `civil.CorridorCollection.RebuildAll()`, `DataShortcuts.SetWorkingFolder("x")`, `surface.ExportToDEM(...)`, `CivilApplication.SurveyProjects`, `new Autodesk.Civil.AeccUiMgd.X()`, `Autodesk.AECC.Interop.Land.AeccApplication`; cho phép `civil.GetAlignmentIds()`, `surface.FindElevationAtXY(x,y)`, `civil.CogoPoints.Add(pt, "d", true)`, `Alignment.Create(civil, opts, …)`, `corridor.IsOutOfDate`, `corridor.RebuildAutomatic`).
- Phase 1 S-05 → phase 2 quyết `Rebuild` mở/đóng; ADR này `Accepted (revised)` sau spike.
- Description `execute_civil3d_code` liệt kê rõ: `tr` của bridge; không rebuild trong MVP; data shortcut/survey/UI/file export bị chặn; station/elevation đơn vị bản vẽ.

## Open items `[chưa xác minh]`
- U4: `Abort()` hoàn lại `Rebuild()`/`AddVertices`/`Alignment.Create` + `Profile.CreateFromSurface` phụ thuộc sạch không; corridor có "biến mất" không (S-05).
- `DocumentLock` có đủ cho Civil object khi gọi từ `Application.Idle` (application context) — AutoCAD verified; Civil giả định như nhau (S-05 chạy qua đúng đường bridge).
- `RebuildAutomatic` bật + sửa alignment → rebuild ngầm trong `inner.Commit()`? thời gian? (S-05 (d) trên `Corridor-1.dwg`).
