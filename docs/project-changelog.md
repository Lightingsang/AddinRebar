# Changelog — HPRebar

Ghi lại thay đổi đáng kể. Mục mới nhất ở trên.

## 2026-09-16 — McpShared: ETABS MCP engine constants, profiles, hints (phase 0 — additive only)

**Bổ sung:** `McpShared/` thêm hằng/profile/DTO/hint cho host thứ tư (ETABS 22, chưa code) — `PipeNaming.EtabsHost`, `JsonRpcMethods.EtabsPrefix`, `HostScriptContracts.Etabs{Imports,Globals,HeavyMaxTimeoutSeconds}`, `ContextResult.Etabs` + `EtabsInfo` (10 field), `ExecuteResult.Snapshot` (tên file snapshot), `AnalyzeRequest.Transaction` (forwarded qua `ToolLifecycleService`), `GuardProfile.Etabs`/`AnalyzerProfile.Etabs`, `IHostProfile.BridgeNotConnectedHint`/`TimeoutSemanticsHint` (hint text), `RequestDispatcher`/`McpBridgeHost` optional `executionDisabledMessage`, `McpServerHost.ConfigureOptions` seed `BridgeOptions.HostVersion` (phần engine không apply trước đó), `ScriptGuard.IsDeniedNamespace` strip tiền tố `global::` (pre-existing bypass, vá làm ETABS guard có tác dụng), `ResultFormatter.FromExecute` strip path khỏi `Snapshot`.

**Xác minh:** `tools/list` 3 host (Revit 33, AutoCAD 37, Navisworks 24) byte-identical sau rebuild; 5 test suite nguyên số: McpShared 96 + 60, HPRebar 109, AutoCAD 58, Navisworks 49 (= 372 cũ); McpShared Core.Tests 128 → **162** (thêm 34 test ETABS: `EtabsProfileTests`, `EtabsBridgeMessagesTests`, `EtabsTestProfile`). Build 0 error. Report: `plans/260916-2152-…/reports/phase-00-…`.

## 2026-09-16 — AutoCAD MCP: AEC phase D — QA/QC (`cad_standards_check`, `audit_aec_drawing`, `create_issue_markup`)

**Bổ sung:** `HPAutoCad.Aec/Standards/` (rule set JSON nhúng `cad-standards.default.json` hoặc file project `rules\cad-standards.json` qua `RuleFileLocator` dùng chung; checker thuần trên record + `DrawingTables`: layer_naming, entity_layer, layer_zero, color/linetype/lineweight_override, text_style, text_height, dim_style, block_naming, unused_layer → `STD-nnnn` thứ tự ổn định), `Issues/AuditIssue` (record chung, `Ordered` critical → warning → info, `AtLeast`), `Cad/AuditService` (một query, section geometry + standards, giữ id GEO-/STD-), `Cad/IssueMarkupService` (circle/rectangle/revcloud + MLeader `<id>: <mô tả>` trên layer `HP-MCP-ISSUES` tự tạo, màu theo severity, hai pha), `AecTools.Audit`; record thêm `Style`, `TextHeightMm`. 3 seed → server 40 tool.

**Xác minh:** build 0 warning; `HPAutoCad.Aec.Tests` 141/141; `HPAutoCad.Mcp.Server.Tests` 153/153 (28 seed); live `run-aec-tools-live.ps1` 63/63 (bước T) + `run-aec-edit-tools-live.ps1` 67/67 (bước K). Report `plans/260916-1140-…/reports/phase-D-qaqc-live.md`.

## 2026-09-16 — AutoCAD MCP: AEC phase C — 6 tool ghi (`create_entities_batch`, `update_entities_batch`, `manage_blocks_attributes`, `manage_annotations`, `manage_hatches`, `manage_xrefs`)

**Bổ sung:** `HPAutoCad.Aec/Model/EditResult` (edit envelope + `ItemOutcome` theo thứ tự input), `Cad/EditContext` (layer guard LAYER_LOCKED/LAYER_FROZEN, space, điểm mm, thuộc tính chung), `EntityFactory` / `EntityUpdater` / `BatchEditService` (atomic: validate hết → một item lỗi từ chối cả batch, không ghi gì; lỗi lúc ghi ném → bridge abort; `atomic:false` ghi phần hợp lệ), `BlockService` (định nghĩa, reference, attribute, dynamic property mm/độ), `AnnotationService` (text, mtext, dimension linear/aligned/angular/radial/diameter, mleader; delete chỉ annotation), `HatchService` (boundaryHandles / polygon / seedPoint, NOT_CLOSED, detectBoundary hình học), `XrefService` (list/resolveStatus/attach/detach/reload/unload/bind an toàn), `AecTools.Editing`; 6 seed `transaction: auto`, mô tả nêu side effect + dryRun. Server 37 tool. Harness mới `run-aec-edit-tools-live.ps1` (40 check).

**Review round (6/10 → sửa xong):** mọi write hai pha — pha 1 mở **đọc** (`OpenForEdit`: handle → layer → owner phải là layout, entity trong block definition bị từ chối `UNSUPPORTED_ENTITY`) + validate không mutate (`EntityUpdater.Validate`, `ValidateAttributes` kể cả layer của từng attribute reference), batch atomic bị từ chối để change counter = 0; pha 2 `Upgrade` + `Apply` với `changed` tích luỹ để báo trung thực khi lỗi giữa chừng. Op đơn lẻ từ chối toàn bộ khi có key sai (hatch update, setDynamic, detach); create đơn lẻ trả `EditResult.Refused` có mã thay vì ném. `SetDatabaseDefaults` trước layer/colour (H1); `Associative = true` trước `AppendLoop(ids)` — verified live hatch theo boundary di chuyển. Cap 64 KB: batch 200, references 100/trang, definitions 200, handles 100/20, attribute 8 × 120 ký tự, `Settle` liệt kê 20 lỗi + đếm, warning gộp theo item. `XrefPathPolicy` (đường dẫn đầy đủ, `.dwg`, không `..`, không tự tham chiếu; UNC cảnh báo), `ValidateSymbolName`. `EntityFilter.From` mặc định `space: all` khi có handles. Tách file > 300 dòng thành partial.

**Xác minh:** build 0 warning; `HPAutoCad.Aec.Tests` 122/122; `HPAutoCad.Mcp.Server.Tests` 136/136 (25 seed compile + validate + cap pin); live `run-aec-edit-tools-live.ps1` 62/62 sau review (trước review 40/40 (lần 1: 33/40 — `Hatch.Area` không đọc được trong transaction đang tạo → fallback diện tích boundary; script harness dùng `using var` top-level không compile dạng Roslyn script) — mọi op chạy dryRun + commit, lớp khoá/đóng băng/handle đã xoá trả lỗi có cấu trúc, `U` qua COM hoàn tác trọn batch; regression `run-aec-tools-live.ps1` 55/55. Report `plans/260916-1140-…/reports/phase-C-editing-live.md`.

## 2026-09-16 — AutoCAD MCP: AEC phase B — `classify_aec_entities` + `get_entity_relationships`

**Bổ sung:** `HPAutoCad.Aec/Classification/` (19 loại AEC, 3 discipline; rule JSON: layer/type/block wildcard, closed, size/aspect, regex text, nearby text làm evidence; rule set mặc định nhúng 26 rule AIA/NCS + tiếng Anh + tiếng Việt, override theo project ở `%AppData%\HPAutoCad\McpServer\rules\<name>.json`, validate khi nạp; `AecClassifier` chọn rule tốt nhất + alternatives + Unknown theo yêu cầu), `Relationships/RelationshipDetector` (intersect, connected có gap + confidence, near, inside, contains, touching, aligned/parallel/perpendicular theo trục chính), `Cad/ClassificationService`, `AecTools.Semantic`; 2 seed category `Aec`. Server 31 tool.

**Review round (6.5/10 → sửa xong):** `count` của relationships = tổng số quan hệ thật (`RelationshipOutcome.Total`), khử trùng lặp quan hệ đối xứng ngay trong vòng lặp khi tự-tập; mọi quan hệ có `locationMm` thật (không còn (0,0,0)); aligned/parallel/perpendicular trả `angleDeg` (aligned kèm offset `valueMm`); từ chối > 2 000 000 cặp trục; rule set mặc định v2 neo stem vào ranh giới tên (`*DAM*` từng bắt nhầm M-DAMPER; STRUCT/S-MONG/COTAS/A-CLNG-GRID/P-SANR-FIXT không còn bị xếp sai, CUASO → Window, ONGGIO → Duct); rule file từ chối list null, key sai chính tả, range âm; hoà điểm giữ thứ tự file; hình chữ nhật thay thế của block/text/dimension không phải footprint `closed`; text/attribute cắt 120 ký tự / 8 attribute; `classify_aec_entities` tối đa 50/trang, `get_entity_relationships` tối đa 250 (schema `maximum` khớp engine, test pin); một text index dùng chung cho hai tập + cảnh báo khi cắt 5 000 text; `ruleSet.source` không lộ đường dẫn profile; polyline lặp đỉnh đóng đọc đúng 4 đỉnh.

**Xác minh:** build 0 warning; `HPAutoCad.Aec.Tests` 100/100; `HPAutoCad.Mcp.Server.Tests` 97/97; live `run-aec-tools-live.ps1` 55/55 sau review round (trước review 51/51 (run 1: 47/51 — `AecObject.Record` `required` + `[JsonIgnore]` làm System.Text.Json từ chối serialize, đã sửa + test): 4 cột / 3 dầm / 9 tường / ống / block cửa phân loại đúng với evidence và kích thước, connected dầm→cột kèm gap 7 mm confidence 0.65, intersect ống×dầm, parallel tự-tập khử trùng lặp. Report `plans/260916-1140-…/reports/phase-B-semantic-live.md`.

## 2026-09-16 — AutoCAD MCP: AEC engine `HPAutoCad.Aec` + 5 tool AEC (plan 260916-1140, phase A)

**Bổ sung:** assembly `HPAutoCad.Aec` (net8.0-windows; geometry/spatial/issue thuần + adapter AutoCAD + facade `AecTools`) được bridge tham chiếu và đưa vào Roslyn (`CompilerReferences`, `AutocadImports` += `HPAutoCad.Aec`); 5 seed read-only (`transaction: none`): `get_drawing_context`, `query_entities` (filter wildcard + paging + detail/property selector), `query_entities_spatial` (10 quan hệ, tolerance override), `measure_geometry` (10 phép đo, dùng curve API chính xác), `detect_geometry_issues` (9 loại lỗi, issue `GEO-nnnn` có vị trí/giá trị/tolerance/gợi ý). Envelope camelCase `{success, summary, items, count, offset, truncated, warnings, errors[{code,message,handle}]}`; `GeometryTolerance` tập trung mọi ngưỡng; handle là định danh duy nhất. Server giờ 29 tool (4 core + 8 registry + 17 seed); profile thêm category Geometry, Audit. Test project `HPAutoCad.Mcp.Server.Tests` → net10.0-windows để tham chiếu Aec.

**Xác minh:** build 0 warning; `HPAutoCad.Aec.Tests` 54/54; `HPAutoCad.Mcp.Server.Tests` 83/83 (mọi seed compile với AutoCAD.NET + Aec); McpShared 128 + 60; Revit 109. Live AutoCAD 2026 `run-aec-tools-live.ps1` 42/42 (run 1: 30/33 — đếm sai entity của scene, `success` của measure; run 4 sau review: arc lật OCS −Z, polyline bulge, hatch, block có attribute, lưới 3 000 line: query lọc layer 92 ms, nearest 1000×1000 1.6 s, quét lỗi 158 ms) trên template inch nên chuyển đổi mm được kiểm thật; hồi quy `run-bridge-unattended` 21/21, `run-server-smoke` 22/22. Code review 7/10 → sửa hết 3 High (SpatialIndex bỏ sót item quá khổ, arc OCS −Z bị phản chiếu, trang mặc định vượt cap 64 KB) + 5 Medium + phần lớn Low, có test hồi quy `ReviewRegressionTests`. Chưa republish exe (bị khoá bởi MCP server đang chạy). Kế tiếp: phase B (classify_aec_entities, get_entity_relationships).

## 2026-09-16 — AutoCAD MCP bridge: Ribbon thu về kiểu Revit `HPAutoCad` ▸ `MCP` ▸ `MCP Bridge` (bundle 0.3.0)

**Thay đổi:** Tab `HPAutoCad` (id `HPAUTOCAD_MCP_TAB` giữ nguyên) ▸ panel `MCP` ▸ 1 nút `MCP Bridge` → `BridgeActions.Run("show")` (cùng delegate `HPMCPBRIDGE`), cùng bề mặt với ribbon MCP của Revit và Navisworks. Bỏ 3 panel / 10 nút của 0.2.0 (mọi nút đều là control có sẵn trong cửa sổ trạng thái) → xoá `RibbonStatusPresenter.cs`, `BridgeEntry.Ribbon.cs` (entry point `status.subscribe`/`copyLastScript`/`autoStart.*`/`path`), `BridgeActions.Query/OpenPath`, bước copy README vào bundle. Icon vector `DrawingImage` vẽ trong code (cửa sổ + phích cắm, cùng glyph với Navis; toạ độ chẵn nên 16 px = ½ 32 px; mực `#E6E6E6` theme tối / `#3C3C3C` theme sáng, accent `#0696D7`), tab rebuild khi `COLORTHEME` đổi.

**Xác minh:** `dotnet build HPAutoCad.slnx -c Debug` 0 warning; `run-ribbon-check.ps1` (viết lại) 12/12 PASS + 1 MANUAL (icon — 2 screenshot theo theme, đã xem: sắc nét cả hai): tab đúng 1 lần, vẫn 1 sau đổi workspace và sau đổi COLORTHEME, `MCP Bridge` mở cửa sổ, bấm lần 2 không mở cửa sổ thứ hai, loader.log sạch. Hồi quy: `run-bridge-unattended.ps1` 21/21, `run-server-smoke.ps1` 22/22.

## 2026-09-16 — `McpShared/tools/harness_common.py`: bookkeeping chung cho harness Python

**Thay đổi:** `Checklist` (check/skip/save/finish → PASS/FAIL/SKIP + JSON summary + exit code), `utf8_console()`, `ok`/`short`, re-export `Server`/`mcp_session`; 4 script (`HPNavis` live-verify/seeds-live/server-smoke, `HPAutoCad` live-verify) bỏ ~30 dòng lặp mỗi file và import theo đường tương đối. Shape JSON summary giữ nguyên các key wrapper đọc (`passed`, `total`, `failed`, `phase`…), thêm `skipped`/`skippedNames` ở mọi script. Self-test: `python McpShared/tools/harness_common.py`.

**Xác minh:** Navisworks: `run-server-smoke.ps1` PASS 9/9, `run-seeds-live.ps1` PASS, `run-live-verify.ps1 -WithNoDoc -IncludeIsolation` PASS (main 52, heavy 3, nodoc 1, isolation 3; modal skip vì `SetForegroundWindow` bị từ chối khi người dùng đang thao tác app khác — đường skip có sẵn). AutoCAD: `run-live-verify.ps1 -SkipRevit` wrapper 4 bước 0 fail, main 56/56. Không đổi code C#.

## 2026-09-16 — Harness AutoCAD dùng script stdio chung `McpShared/tools/`

**Thay đổi:** Xóa `HPAutoCad/tools/harness/{mcp-call.py, mcp-session.py}` (chỉ khác bản canonical ở docstring); `live-verify.py` import `../../../McpShared/tools/mcp-session.py`, `run-live-verify.ps1`/`run-server-smoke.ps1` gọi `McpShared/tools/mcp-call.py`; `plans/260915-…/reports/snapshot-tools-list.ps1` cũng trỏ sang đó. Chiều phụ thuộc giữ nguyên: thư mục MCP → `McpShared/`, không có bản copy nào khác trong repo.

**Xác minh:** `python McpShared/tools/mcp-call.py <AutoCAD exe> tools/list` → 24 tools; `HPAutoCad/tools/harness/run-live-verify.ps1 -SkipRevit` live trong AutoCAD 2026: main A–D 56/56, disabled 1/1, nodoc 1/1, busy 2/2 (wrapper 4 bước, 0 fail; Revit không chạy nên E bỏ qua — lần chạy đầy đủ trước đó chỉ fail 2 check E vì bridge Revit chưa mở); `run-server-smoke.ps1` 22/22 (lần đầu pipe không lên vì AutoCAD khởi động sau khi bị kill — hiện tượng môi trường, chạy lại PASS). Sửa kèm: bước nodoc retry `ActiveDocument.Close` 5 lần khi AutoCAD trả `RPC_E_CALL_REJECTED`. Không đổi code C#.

## 2026-09-16 — Navisworks MCP bridge: Ribbon tab "HPNavis" ▸ "MCP" ▸ "MCP Bridge" (plan 260916-0005-navisworks-ribbon-tab)

**Bổ sung:** `HPNavisRibbonPlugin : CommandHandlerPlugin` (`[RibbonLayout("HPNavisRibbon.xaml")]`, `[RibbonTab("ID_HPNAVIS")]`, `[Command("ID_HPNAVIS_MCP_BRIDGE", CallCanExecute = Always)]`) mở/activate cửa sổ bridge — cùng bề mặt 1 nút như ribbon MCP Revit, nhãn English. Layout `Ribbon/en-US/HPNavisRibbon.xaml` (XAML của Navisworks, `<Page Remove>` khỏi WPF markup compile) + `HPNavisRibbon.name`; icon `Ribbon/Images/McpBridge_16|32.png` render pixel-exact từ 1 glyph vector (cửa sổ + phích cắm, ink #3C3C3C / accent #0696D7) bằng `tools/icons/render-ribbon-icons.ps1`. `HPNavisWindowPlugin` → `AddInLocation.None` (hết trùng nút Add-ins, vẫn `ExecuteAddInPlugin`). Deploy target sẵn có copy `en-US\` + `Images\`.

**Xác minh:** `run-ribbon-check.ps1 -WithNoDoc` 2026-09-16: 14 PASS, 0 FAIL, 1 MANUAL (screenshot icon — sắc nét): tab đúng 1 lần, không cửa sổ trước khi bấm, bấm → cửa sổ + log `MCP bridge status window opened`, bấm lần 2 → 1 cửa sổ, không còn mục Add-ins, không ERR/FTL; không có model → Navisworks tự làm mờ mọi tab (start page). Tests `HPNavis.McpBridge.Tests` 135 (+11 `RibbonPluginTests`: attribute ⇔ XAML id ⇔ `.name` ⇔ PNG 16/32 RGBA, `AddInLocation.None`, file copy vào `en-US\`/`Images\`). Hồi quy: `run-live-verify.ps1` + `run-bridge-unattended.ps1` (xem report). Harness: `Find-RibbonTabHeaders`/`Select-RibbonTab` (Invoke → SelectionItem, verify nút hiện) /`Wait-RibbonButtonVisible`/`Count-BridgeWindows`/`Save-RibbonScreenshot`, `Start-NavisworksWithModel -showWindow`.

## 2026-09-15 — Navisworks MCP (plan 260915-0824-navisworks-mcp-2026, phases 0–5)

**Bổ sung:** `HPNavis/` — plugin `HPNavis.McpBridge` (net48) trong Roamer.exe + server `HPNavis.Mcp.Server` (net10, stdio) với `NavisHostProfile`, 24 tools (4 core + 8 registry + 12 seed nhúng: 8 read-only, 3 review edit, 1 heavy `create_and_run_clash_test`), resources `navis://document/info`/`navis://selection`, prompts `navis_query_template`/`navis_review_template`. Engine `McpShared/`: `HPRebar.Mcp.Contracts` multi-target `netstandard2.0;net48`, `HPRebar.McpBridge.Core` `net8.0;net48` (`PipeSecurity` ACL, `MainThreadQueue(expireWithoutTicks)`), `HostScriptContracts.NavisHeavyMaxTimeoutSeconds = 600`, `McpShared/tools/` (script stdio host-neutral). Undo policy commit-rồi-`Rollback()` chỉ khi `NextUndo` là entry của bridge; heavy gate với opt-in thứ hai; quiescence bằng Progress depth + `IsWindowEnabled`.

**Xác minh:** live trong Navisworks Manage 2026: bridge 43 check ×2 + no-doc, server smoke 9/9, seeds 18/18 + 2/2, live-verify 62 pass trên run 2–4 (run 1: 59 + 1 fail ở assertion của harness) (execute matrix, mọi seed, registry loop MISS→propose→test→publish→CLI approve→list_changed 0.5 s→gọi theo tên, quarantine→restore→newVersion, heavy proposal bị từ chối, modal, heavy ON clash 3 015 kết quả + append, no-doc, isolation Roamer thứ hai + gỡ plugin). Hồi quy: Revit 33 / AutoCAD 24 `tools/list` byte-identical với snapshot phase 0 sau rebuild Release; tests 128 + 60 + 109 + 58 + 124 + 49. Chưa làm: Ribbon tab Navisworks (quyết định để sau phase 5).

## 2026-09-14 — AutoCAD MCP bridge: Ribbon tab "MCP AutoCAD" (plan 260914-2204-autocad-ribbon-tab)

**Bổ sung:** Ribbon tab "MCP AutoCAD" (bundle 0.2.0) in the loader via Autodesk.Windows (compile-time only). Tab id `HPAUTOCAD_MCP_TAB`; three panels (Kết nối: status window, start/stop, status, live label; Công cụ: copy last script, tool library; Thiết lập: logs, audit, auto-start toggle, guide); 9 buttons + 1 live label, all forward to bridge entry points (status.subscribe, copyLastScript, autoStart.get/set, path, show/start/stop/status). Entry points cross ALC via BCL types only. No CUIx modification. Tab lifecycle: created once on Ribbon init, re-created after workspace switch (SystemVariableChanged → Idle → EnsureCreated guard), guarded against duplication, removed on Terminate. Icons are vector drawings in code. When bridge unavailable, Ribbon buttons disabled with tooltip.

**Xác minh:** Live run 2026-09-14 via `run-ribbon-check.ps1` 8/8 (UIA automation): tab exactly once, survives workspace round trip, Bật listener → pipe up, Tắt → down, Bảng điều khiển → window, Trạng thái clean. Regressions: bridge 21/21 (`run-bridge-unattended.ps1`), server 22/22 (`run-server-smoke.ps1`, now accepts approved tools ≥ 24). Build 0 warn/err (HPAutoCad.McpBridge.Loader.csproj, -UseWPF, -DeployBundle copy README.md). Tests: 263 total (96 McpShared, 109 HPRebar MCP, 58 AutoCAD). Code: loader 6 files / 398 LOC (BridgeActions 78, McpRibbonTab 181, RibbonStatusPresenter 52, RibbonCommandHandler 27, RibbonIcons 65), bridge BridgeEntry.Ribbon 71; harness +run-ribbon-check.ps1 98 + harness-common.ps1 +49 helper functions.

**Load-bearing gotchas for future work:**
- `UseWPF` on the loader removes implicit `System.IO` using → add explicitly
- AdWindows exposes Ribbon tab header as `Button` with `AutomationId=tab id` (not `TabItem`)
- RibbonButton is `Button` named after its text once tab selected
- `SendCommand('_.WSCURRENT …')` over COM never returns → use `ActiveDocument.SetVariable('WSCURRENT', …)` instead
- Workspace switch drops code-added tabs → loader re-creates them automatically

**Các quyết định:**
- Ribbon entry points BCL-only: `Func<Action<string,string>, Action>` (status.subscribe), `Func<string>` (show/status/copyLastScript/path), `Func<bool>` (autoStart.get), `Action<bool>` (autoStart.set) — no bridge types cross ALC
- Commands + buttons share `BridgeActions` runner to prevent duplication
- Tab never duplicates via `FindTab` guard + `EnsureCreated` logic
- Not on Ribbon: opt-in checkbox (stays in window, OFF on load), tool-run button (server owns tools, never bridge)
- Icons: vector DrawingImage in code, no external image files

**Bước tiếp:**
- Ribbon plan (260914-2204) complete on top of the finished bridge plan (260913-0000, phases 0–5)
- Phases 4–5 known gaps remain: Revit opt-in runtime unverified (harness E1 skipped), Revit 2025, AutoCAD 2026 Update 1.2 (.NET 10), modal dialog + ESC-retry, per-run undo
- Next: user smoke test in live AutoCAD (normal Start menu launch, clicks while command waits, clipboard/Explorer effects)

**Commits:** e5fae0a feat (ribbon + 4 entry points + bundle 0.2.0), phased into [`plans/260914-2204-autocad-ribbon-tab/`](../plans/260914-2204-autocad-ribbon-tab/plan.md).

---

## 2026-09-14 — AutoCAD MCP bridge phase 5: live verification harness + stability-window fixes, plan complete

**Bổ sung:** Live verification harness (unattended) tại `HPAutoCad/tools/harness/` — `run-live-verify.ps1` + `live-verify.py` + `mcp-session.py` (≈560 + 120 + 150 lines) dùng một stdio MCP session để chạy 65 scenario (64 pass + 1 skip) trên registry riêng (isolated `output/live-verify/`) sinh động trong AutoCAD + Revit: execute matrix 18 (none/dryRun/commit, exception, none+modify, manual, guard trên `GetPoint`/`Commit`/`SendStringToExecute`, compile error, `cancel_execution` racing, timeout 5s, **busy → ESC posted → retry automated**, no drawing, audit), every seed 18 (real block, pickfirst set, dryRun), MISS → ad-hoc code + `propose_tool` → `test_tool` → `publish_tool` → CLI approve → `tools/list_changed` in 0.5s → call by name, fragile tool (unguarded `eKeyNotFound`) → 5× fail → quarantine → `manage_tool restore` + `propose_tool newVersion` (guarded, `ArgumentException`) → re-approve → stays published on 5× fail, Revit exe beside (34 tools, Revit lib hash unchanged, opt-in off → E1 skipped), isolation (second AutoCAD fails fast naming host, Civil 3D never loads).

**Stability window fixes (engine, both hosts):** Runs với lỗi `Argument…Exception:` không được tính (là lỗi caller, không tool), window restarts ở lifecycle event cuối (approved/published/restore/proposed_version/imported/status_changed), restored tool không bị re-quarantine bởi lỗi cũ. Hai defect tìm live: (1) seed `insert_block` bị quarantine bởi smoke tests của chính nó — fixed by excluding arg errors; (2) restored tool `mcp_verify_count_block_refs` bị re-quarantine ngay lần chạy đầu — fixed by restarting window at lifecycle event. Harness chạy registry isolated (không chạm `%AppData%` live) với flag `-IncludeIsolation` / `-OnlyIsolation` / `-SkipRevit` / `-UseLiveRegistry`.

**Xác minh:** Live run 2 (sau 2 engine fix) = 64 pass + 1 skip (Revit opt-in off) + isolation 4/4 + bridge regression 21/21; run 3 trên registry isolated: 64 pass + 1 skip. Build zero warn/err. Tests: McpShared 96/96, HPRebar MCP 109/109, AutoCAD 58/58 (263 total). Code review 7.5/10 → 16 actionable findings fixed; majors: the two engine fixes, harness registry isolation, pid-scoped cleanup. Open: Revit opt-in with new engine unverified at runtime (harness E skip); Revit 2025; AutoCAD 2026 U1.2 (.NET 10); modal dialog while waiting; per-run undo; `HPRebar/output/…exe` not republished (locked by running `hprebar-revit` MCP).

**Các quyết định:**
- Stability window logic: lỗi `Argument…Exception` do script tự từ chối đối số là lỗi caller, không phải tool failure → exclude khỏi quarantine check; window restart ở cuối lifecycle event mình = approved/published/restore/proposed_version/imported/status_changed từ bất cứ nguồn (manual edit `tool.json` triggered reload writes event, manual CLI action, etc.)
- Harness: one stdio session vì `notifications/tools/list_changed` chỉ reach running server; registry isolation = env var override LibraryPath + DbPath để server + CLI không chạm user's %AppData%; pid guard cleanup to avoid killing user-opened processes; `-UseLiveRegistry` opt để bypass isolation khi user muốn test on live registry
- Bridge `RequestDispatcher.HostVersion` + pipe-in-use message names host + version; Revit bridge log sink `shared: true` like AutoCAD
- `ToolRegistryDb` split: schema/search (cs) + runs/stability (Runs.cs partial)

**Commits:** d8507e8 feat (harness + engine fixes), 1b2ba8b fix (16 review findings). Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 0–5 of 6 done, **plan complete**).

## 2026-09-14 — AutoCAD MCP bridge phase 4: registry per host profile + 12 seed tools

Bổ sung: Registry engine per `IHostProfile` (categories, reserved names from profile, host stamp); 12 embedded AutoCAD seed tools (6 read-only: list_layers, list_block_definitions, get_entities, list_layouts, get_drawing_info, get_selected_entities; 6 auto-transaction: draw_polyline, draw_circle, add_text, create_layer, insert_block, add_linear_dimension) installed into `%AppData%\HPAutoCad\McpServer\tools-library\` on first run across 7 categories (Drawing, Layer, Block, Annotation, Layout, Data, Generic); `SeedLibraryTests` 58 xUnit tests compile every seed against AutoCAD.NET 25.1.0 with bridge's exact imports/globals (0 skipped); meta-tool descriptions host-neutral (8 engine tools + inspect_type title worded for any host).

**Xác minh:** Live smoke harness `run-server-smoke.ps1` 22/22 (all 12 seeds by name, category filters, dryRun, layer round-trip), code review 7.5/10 with 12 actionable findings fixed the same day (no re-score), zero crashes, no .NET Runtime 1026 event. Tests: McpShared 95/95, HPRebar MCP 106/106, AutoCAD 58/58 (259 total). Build zero warn/err. Registry per host: `registry stats` prints `host: autocad (AutoCAD)` on AutoCAD server. Revit tools/list 34 names/schemas/annotations byte-identical to phase 0 (descriptions of 8 + title differ).

**Các quyết định:**
- `IHostProfile.CliExecutable` names the exe in every human instruction (publish_tool message, _review/*.md, CLI usage banner); defaults from assembly name if not overridden (Revit explicit `HPRebar.Mcp.Server.exe`, AutoCAD `HPAutoCad.Mcp.Server.exe`)
- Seed points as `{x, y}` objects not `[x, y]` arrays; mm at every edge via `ScriptUnits.ToDrawing/ToMm`; guard denies `StartTransaction/Commit/Abort/LockDocument`, `ed.Get*`
- No seed creates missing layer — clear error "run create_layer first"; only the `autocad_modify_template` prompt auto-creates
- Seed contract: no `tr` open/close (guard catches `UsesTransaction`), reads via `GetBlockModelSpaceId`, writes via `db.CurrentSpaceId` + `AppendEntity` + `AddNewlyCreatedDBObject`
- New `Validate(record, analysis, existing, newVersion, profile)` overload; the four-argument overload keeps the Revit behaviour locked (tests unchanged)

**Commits:** 69e3505 feat (engine + 12 seeds), f257071 fix (12 review findings). Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 0–4 of 6 done).

## 2026-09-14 — AutoCAD MCP bridge phase 3: server exe over stdio

Bổ sung: `HPAutoCad.Mcp.Server` (net10 console exe) + `AutocadHostProfile` (12 tools: 4 core + 8 registry, `autocad://` resources, 2 prompts), registry root `%AppData%\HPAutoCad\McpServer\`, pipe `hpautocad-mcp-2026`, env prefix `HPAUTOCAD_MCP_`. Core: `ContextService.Shape` drops `revitVersion`/`isFamily` for non-Revit hosts (wire unchanged, Revit output byte-identical, regression test 89/89).

**Xác minh:** Publish exe 7.4 MB, stdio harness 7/7 ×2 (lead + tester): initialize, 12 tools/list, context without Revit fields, execute none/dryRun/real with runId + hint, get_run from registry.db. Revit exe 34 tools unchanged with Revit 2026 running side-by-side. Build zero warn/err. Tests: AutoCAD 8/8 + McpShared 89/89 + HPRebar MCP 106/106 (203 total). Harness files (`mcp-call.py`, `run-server-smoke.ps1`, `run-bridge-unattended.ps1`, `harness-common.ps1`) live in repo.

**Các quyết định:**
- `revitVersion`/`isFamily` hidden in `ContextService.Shape` (non-Revit hosts); Revit: wire unchanged, output byte-identical
- `IsModifiable` documented in Contracts XML comment + descriptions per host (Revit: transaction open; AutoCAD: writable + quiescent)
- Description length 1717 chars (AutoCAD contract needs `tr`/deny/units/semantics; plan budget ~1200, real budget 1800)
- Harnesses in repo (Python + PowerShell 7.3+, JSON quoting safe, depth guards)
- `.mcp.json` entry user adds (untracked); snippet in `HPAutoCad/README.md` + smoke report

**Những chưa làm:**
- Engine meta-tool descriptions still say "Revit"/`execute_revit_code` (phase 4: host-neutral wording or profile-driven text)
- Seed tools for AutoCAD (phase 4)
- R27 AutoCAD support (not planned)

**Các gaps được chấp nhận:**
- `runId`/`hint` branch exercised only by live smoke, not by xUnit (registry-less `ExecuteCodeService` in tests; phase 4 will add AutoCAD test with temp-dir `ToolManager`)
- Modal dialog while waiting + ESC-then-retry (phase 5 manual)
- Per-run undo (needs `ExecuteInCommandContextAsync`, phase 5)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 1–3/6 done).

## 2026-09-14 — AutoCAD MCP bridge phase 2: bridge runtime

Bổ sung: `MainThreadExecutor` (Application.Idle + IsQuiescent, PostMessage WM_NULL wake, busy grace 8 s), `AutocadScriptRunner` (outer = group, inner = `tr`, commit inner trước quyết định outer, dryRun rollback), `DatabaseChangeCounter` (HANDSEED + ObjectOpenedForModify + IsErased, không giữ wrapper), `AutocadContextReader`/`AutocadResultSerializer` (entities, ObjectId handles, units mm ↔ drawing), XAML status window (theme dark/light override, per-session opt-in "Allow AI code execution"), Core `MainThreadQueue` + `BridgeRequestException` + `AutocadInsunits` + guard deny `StartTransaction`/`LockDocument`.

**Xác minh:** Build zero warn/err, tests McpShared 88/88 + HPRebar MCP 106/106, harness unattended 21/21 (lead + tester independent), AutoCAD 2026 R25.1, 234+ audit lines, zero crashes.

**Các quyết định:**
- ADR-03 Accepted (revised): outer transaction = TransactionGroup role, inner = `tr`, inner commit → event fire → count → outer decision; `transaction=manual` chạy như `auto` + cảnh báo, guard deny `StartTransaction` (vì finalizer crash ở GC thread)
- `transaction=none`: vẫn mở transaction (read cần tr ở AutoCAD), luôn abort, sửa như Revit nếu modified
- Undo merged per user command trong lock "HPMCP"; per-run undo out of MVP
- Idle one-shot, busy grace bị cắt bớt (tester #2), bỏ BusyGrace default 10s → enforce deadline

**Những chưa làm:**
- Modal dialog + ESC-then-retry (phase 5 manual)
- Per-run undo (needs `ExecuteInCommandContextAsync`)
- `IsModifiable` semantics document (phase 3)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 1–2/6 done).

## 2026-09-14 — AutoCAD MCP bridge phase 1: loader, ALC, bundle, spike

Bổ sung: loader DLL với BridgeLoadContext (Roslyn 5.9 + Immutable 10 riêng, AutoCAD API shared), self-check startup, 4 core command (HPMCPBRIDGE/STATUS/START/STOP) + spike 2 command tạm thời, bundle 24 file / 14 MB → `%AppData%\Autodesk\ApplicationPlugins\`. Spike chạy 5/5 lần ×3 run liên tiếp, cuối cùng unattended (SECURELOAD auto-click, `Document.CloseAndDiscard()` + `Quit()` exit code 0).

**Xác minh:** Build zero warn/err, tests 70+106+334 xUnit pass, live in AutoCAD 2026 (2026-09-14).

**Các quyết định:**
- ADR-02 Accepted: Idle one-shot, timeout hủy subscribe (không để hang), executor rule: complete request trước unsubscribe
- ADR-05 Accepted: SECURELOAD prompt trên mỗi hash loader mới → "Always Load"; signing hoãn tới pack phase
- Spike gated `HPAUTOCAD_MCP_SPIKE=1` env var; xóa ngay khi phase 2 đâm ống listener

**Những chưa làm:**
- Pipe listener + executor (phase 2)
- MCP server exe + tools (phase 3)
- Modal dialog + Dynamo coexistence test
- Multi-version (R26/R27)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phase 1/6 done).

## 2026-09-14 — Tách MCP engine host-neutral, scaffold AutoCAD

Refactor hạ tầng MCP để dùng chung cho Revit + AutoCAD. Tách engine (neutral với host) ra thư mục `McpShared/` cấp cao nhất; tạo scaffold `HPAutoCad/` để bắt đầu bridge AutoCAD.

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/phase-00-…`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phase 0 xong; phase 1–5 tiếp theo).

### Thêm

- **`McpShared/`** (thư mục cấp cao nhất, slnx + global.json riêng):
  - `HPRebar.Mcp.Contracts/` (moved từ HPRebar) — netstandard2.0, hợp đồng dây JSON-RPC.
  - `HPRebar.McpBridge.Core/` (moved, refactor) — net8.0, pipe listener/dispatcher, guard/compiler, `BridgeSettingsStore`, `RequestDispatcher` dispatch theo method suffix (`revit.*` vs `autocad.*`), `IHostProfile` interface mới, `HostNeutralityTests`.
  - `HPRebar.Mcp.Server.Core/` (NEW) — net10.0 class lib, `McpServerHost` builder pattern, `HostProfile` abstraction, `ExecuteCodeService`/`ContextService`, Registry engine (`ToolRecord.Host/HostVersions`, SQLite WAL+FTS5), 4 core tool + registry CLI.
  - `HPRebar.Mcp.Server.Core.Tests/` (NEW) — net10.0 xUnit v3, 70 test: host-neutrality, guard/compiler/args/analyzer, HostProfile binding, registry store/db/manager, pipe round-trip fake executor.

- **`HPAutoCad/`** (thư mục cấp cao nhất, slnx + global.json, scaffold):
  - `HPAutoCad.slnx` reference `../McpShared/...` projects only.
  - `README.md` ghi layout, không có project class C# chưa.

### Sửa

| Vấn đề | Xử lý |
|---|---|
| HPRebar.Mcp.Server bị phồng (server + registry + pipe client) | Tách pipe client + registry → McpShared; server nay là thin exe (Program.cs một dòng) |
| Registry instance-per-app (Revit 2026 lúc này) | HostProfile dùng chung, registry root = `%AppData%\<Product>\McpServer` |
| Server build từ HPRebar cwd, khó test riêng | McpShared.slnx độc lập, test từ `cd McpShared && dotnet test` |
| Revit ↔ AutoCAD share pipes/registry mà không có cách select | RequestDispatcher routing: method prefix suffix xác định executor (`revit.execute` ≡ `autocad.execute`, ngoài các tool factory/impl riêng) |

### Bỏ

- HPRebar.Mcp.Server không còn phụ thuộc trực tiếp `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Contracts` (khác HPRebar folder).
- Hard-code "HPRebar"/"Revit" ở vài chỗ error string (engine chỉ biết host qua HostProfile).

### Hoãn

- AutoCAD bridge + server implementation (phase 01–02 chưa làm).

### Giới hạn

- McpShared test chạy từ McpShared cwd (global.json pin), HPRebar test chạy từ HPRebar cwd.
- Revit bridge DLL chưa rebuild → vẫn dùng được exe mới (backward compatible).
- Deploy builder auto-generate `Solutions.HPRebar` via Sourcy thay cho scan repo — phần này cần verify HPAutoCad tương tự.

## 2026-09-04 — Tích hợp NotebookLM (`notebooklm-py`)

Nối `teng-lin/notebooklm-py` 0.8.2 (MIT, unofficial) vào repo trên ba mặt, kèm pipeline nạp tài liệu Revit của repo lên notebook rồi kéo học liệu về.

Plan: [`plans/260904-1038-install-notebooklm-py-integration/`](../plans/260904-1038-install-notebooklm-py-integration/plan.md) · Docs: [`docs/notebooklm-integration.md`](notebooklm-integration.md)

### Thêm

- **Agent skill** `.claude/skills/notebooklm/` + mirror `.agents/skills/notebooklm/` — SKILL.md 60 → 61 ở cả hai cây.
- **MCP server** — `.mcp.json` đầu tiên của repo, 38 tool qua `uvx --from "notebooklm-py[mcp]"`.
- **Python surface** — venv riêng `scripts/.venv`, wrapper `scripts/notebooklm_client.py`, pipeline `scripts/build-notebooklm-course-assets.py` + manifest allow-list.
- **`tests/notebooklm/`** — 24 test `unittest`, chạy được bằng system Python (không cần `notebooklm`, không chạm mạng).
- **`docs/notebooklm-integration.md`** — cài lại từ máy trắng, version pin, bẫy đã dính.

### Sửa

| Vấn đề | Xử lý |
|---|---|
| `privacy-block` chỉ khớp `.env*` và `/credentials/i` — cookie Google đọc thoải mái | Thêm `storage_state.json` + `master_token.json` vào `privacy-checker.cjs` |
| `npx skills add` clone cả repo upstream (111 MB) kèm `CLAUDE.md`/`AGENTS.md` lồng trong `.claude/skills/` | Cắt còn `SKILL.md` + `LICENSE` (28K); ghi bước cắt vào plan |
| `generate_quiz`/`generate_flashcards` không nhận `language` → `TypeError` giữa chừng sau khi quota đã tiêu | `generate_and_wait` lọc kwarg theo chữ ký thật, log cảnh báo khi bỏ |

### Ghi chú

- Credential nằm ngoài worktree (`~/.notebooklm/`) — cố ý, để commit nhầm là bất khả thi về cấu trúc.
- Tài khoản là tier Pro (đo được: 300 source/notebook, 500 notebook), không phải free như plan giả định ban đầu.
- `AGENTS.md` đang lệch `CLAUDE.md`; re-sync là việc của plan `260823`.

## 2026-09-04 — Column Rebar (port từ `R01_ColumnsRebar`)

Feature đầu tiên của HPRebar. Port tool `R01_ColumnsRebar` (~19.5k dòng, .NET 4.8, Revit 2021, MVVM tự viết) sang Nice3point SDK + CommunityToolkit.Mvvm, đa version R23–R27.

Plan: [`plans/260903-2307-port-column-rebar-to-hprebar/`](../plans/260903-2307-port-column-rebar-to-hprebar/plan.md)

### Thêm

- **`HPRebar.Core`** (netstandard2.0) — toán thuần, không reference Revit. 25 file / ~1.5k dòng. 99 test xUnit.
- **`HPRebar.Core.Tests`** — xUnit v3 + Microsoft.Testing.Platform.
- **`HPRebar.Tests`** — TUnit chạy in-process trong Revit. 16 test, hiện skip hết vì thiếu model mẫu.
- **Feature `Column Rebar`** — 77 file / ~6.5k dòng: đọc hình học, 14 rule kiểm tra, tạo thép chủ + đai + đai phụ, tạo view/dimension/bảng thống kê, UI 8 tab, 4 control preview.
- **Theme** — `Resources/Themes/` 8 file, Dark + Light, theo `UIThemeManager` của Revit.
- **Song ngữ EN/VN** — đổi runtime, không đóng dialog.
- **Serilog file sink** → `%LocalAppData%\HPRebar\logs\`, xoay theo ngày, giữ 7 file.
- **`.gitattributes`** — git-lfs cho `*.rvt`/`*.rfa`/`*.rte`; ép LF cho fixture skill-sync.

### Sửa (bug có trong tool gốc)

| # | Bug | Ảnh hưởng |
|---|---|---|
| B1 | `GetErrorColumns` gán `error =` 15 lần không `return` | Báo lỗi **cuối cùng** thay vì lỗi đầu tiên user gặp |
| B2 | `IsnotVerticalColumns` `return` trong vòng `for` | Chỉ kiểm tra cột đầu tiên |
| B3 | 5 chỗ `OrderBy` không gán kết quả | `zb`/`hb` đọc từ mặt dầm bất kỳ, không phải mặt thấp nhất |
| B4 | `BarMainModel.cs:61` `Bar = Bar;` tự gán | Tham số `rebarBarModel` bị vứt |
| B5 | `GetWallBoudingBoxOneColumn` so `ElementId` với `Level` | Luôn true → không bao giờ tìm thấy tường đỡ cột |
| B6 | `double.Parse(UnitFormatUtils.Format(...))` ở 7 chỗ | Đổi đơn vị qua chuỗi, phụ thuộc culture |
| — | `CreateAddVerticalStirrupRectangleType1Item` dùng `BarH` thay `BarV` ở cả 4 chỗ | Đai phụ dọc dựng bằng shape của đai phụ ngang |

### Bỏ

- `WpfCustomControls` + `DSP` (dependency ngoài) — thay bằng CommunityToolkit.Mvvm + `RevitUnits`.
- `System.Windows.Forms.MessageBox` → `TaskDialog`.
- TaskBar (logo / YouTube / Account).
- Start/End section view (bản gốc đã comment out).
- Combobox chọn rebar shape ở tab Setting — bản gốc ép về `M_T1`/`M_T3` nên là UI chết.
- `ViewSchedule` cho detail component — bản gốc lọc field theo tên chuỗi tiếng Anh, hỏng trên Revit khác ngôn ngữ; chỉ phục vụ detail shop vốn chưa làm.

### Hoãn

- **Đường `IsRebar = false`** (thay thép thật bằng Detail Item `DT00..DT07A`) — cần family riêng của tác giả gốc mà repo không có.
- **Detail shop** (`DS*` family) — cùng lý do.
- **Thép gia cường (AddBar)** — chưa có ở Core.
- **Nav icon PathGeometry** — nav hiện chỉ có text.

### Giới hạn — đọc trước khi dùng

**Chưa có phiên bản Revit nào được verify runtime.** Build sạch 5/5 config (Debug + Release, 0 error, 0 warning CS) là bằng chứng duy nhất hiện có.

- Máy dev chỉ cài Revit 2025 + 2026 → R23/R24/R27 **build-only**.
- 16 TUnit test skip hết vì thiếu `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`.
- Rủi ro cụ thể chưa đo được: hack `SURFACE→LINEAR` trong `DimensionCreator` (undocumented, chưa chạy lần nào), `FormattedText` trên net48, Polyfill `MinBy` trên net48, .NET 10 runtime của R27.
