# ADR-01 — Chia sẻ mã với `HPAutoCad/`: (A) copy + mirror test · (B) folder `AcadShared/` · (C) một bundle cho cả hai — **cần user quyết**

**Ngày:** 2026-09-17 · **Status:** **Accepted (A) — user xác nhận 2026-09-17** ("có" theo khuyến nghị: A + MirrorTests cho plan này; B = follow-up có điều kiện sau phase 4) · **Owner:** HPCivil3d
**Kế thừa:** [AutoCAD ADR-06 one MCP one folder](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-06-one-mcp-one-folder.md) · [AutoCAD ADR-05 loader + ALC](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · [Navis ADR-01 (B lặp ~2 000 dòng → chọn đa mục tiêu)](../../260915-0824-navisworks-mcp-2026/adr/adr-01-net48-host-multitarget-mcpbridge-core.md)
**Bằng chứng:** [E7](../research/evidence-on-machine-2026-09-17.md) (số dòng thô) · [researcher-03 §1–3, §8](../research/researcher-03-hpautocad-mirror-shareable-core-and-tests.md) (phân loại từng file, `verified by path:line`) · CLAUDE.md § Repository Layout: *"The only permitted dependency direction is MCP folder (`HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPEtabs/`) → `McpShared/`; the MCP folders never reference each other"* và hàng `McpShared/`: *"Never references `Autodesk.*`"*.

## Context

Civil 3D 2026 **là** AutoCAD 2026 (`acad.exe` chung, R25.1, .NET 8 — E1/E2) cộng `AeccDbMgd`. Mọi thứ bridge AutoCAD làm — loader + `AssemblyLoadContext` riêng cho Roslyn 5.9, `Application.Idle` + `PostMessage(WM_NULL)`, hai transaction outer/inner + dryRun `Abort`, `DatabaseChangeCounter` (HANDSEED + `ObjectOpenedForModify`), serializer, cửa sổ trạng thái, ribbon một nút, `DeployBundle` — đúng nguyên văn trong Civil 3D. Câu hỏi duy nhất: **copy** hay **tách thư viện chung**. User đặt quy tắc: *đo trước; nếu "shareable core" > ~1 500 dòng → khuyến nghị B, ngược lại A*.

## Đo (2026-09-17, `wc -l`, chỉ `.cs`/`.xaml` — không csproj/manifest)

| Nhóm | File (dòng) | "Identical hoặc chỉ đổi token giữa AutoCAD ↔ Civil 3D" | Per-product |
|---|---|---|---|
| Loader (482) | `BridgeLoaderApplication` 111 · `McpRibbonTab` 151 · `BridgeLoadContext` 48 · `RibbonIcons` 45 · `BridgeActions` 42 · `LoaderLog` 30 · `BridgeLoaderCommands` 28 · `RibbonCommandHandler` 27 | **482** (token: tên ALC, ids ribbon, tên lệnh, folder log; `HostAssemblyPrefixes` += `"Aec"`) | `PackageContents.xml` 28, csproj 55 |
| Bridge — AutoCAD-API pass-through (byte-identical) | `AutocadScriptRunner` 280 · `MainThreadExecutor` 238 · `AutocadResultSerializer` 210 · `DatabaseChangeCounter` 73 · `AutocadVersionMap` 27 · `AutocadThemeSwitcher` 43 · `View/*.xaml.cs` 25 | **896** (chỉ `HostName`, URI theme) | — |
| Bridge — có phần Civil | `BridgeEntry` 185 → ≈120 pipeline chung / 65 hằng + Aec + probe · `AutocadContextReader` 110 → ≈70 chung / 40 + ~60 Civil mới · `ScriptingSelfCheck` 59 → ≈40 / 19 probe · `AutocadScriptGlobals` 55 → base chung / +1 field `civil` | **≈285** | ≈125 + ~60 mới |
| XAML | `AutocadBridgeStatusView.xaml` 160 · `AutocadTheme.xaml` 123 · `…Light.xaml` 16 | 299 (đổi tên resource) | — |
| Server exe (305) | `Program` 8 chung; profile/tool/prompt/resource **per host** theo thiết kế engine | 8 | 297 |
| **Tổng C#** | | **≈1 663** | |
| **Tổng C# + XAML** | | **≈1 962** | |
| Cách đếm researcher-03 ("host-neutral", coi file AutoCAD-API là X) | | **1 021** | 1 002 |

Kết luận đo: theo cách đếm **đúng cho câu hỏi này** (Civil 3D dùng chung AutoCAD API nên file "AutoCAD-API-bound" chính là file chia sẻ được), core chia sẻ ≈ **1 663 dòng C#** — **vượt ngưỡng ~1 500 khoảng 10 %**. Theo cách đếm chặt của researcher-03 (chỉ mã host-neutral) là 1 021 — dưới ngưỡng. Quy tắc user áp dụng thẳng → **B** (mép); áp dụng cách đếm chặt → A.

## Options

### A — Copy vào `HPCivil3d/` + **mirror test** (khuyến nghị cho plan này)
- `HPCivil3d.McpBridge.Loader` và `HPCivil3d.McpBridge` = copy 100 % file trên với token đổi (bảng ADR-06 §1); `HPCivil3d/` chỉ `ProjectReference ../McpShared/*`. Quy tắc repo **nguyên**.
- **Mirror test** (mới, rẻ, cơ học): `HPCivil3d.McpBridge.Tests/MirrorTests.cs` đọc từng file trong danh sách "byte-identical" (896 + loader 482) ở cả hai folder, áp bảng thay token (`HPAutoCad`→`HPCivil3d`, `HPAUTOCAD`→`HPCIVIL3D`, `"AutoCAD"`→`"Civil 3D"`, `Autocad`→`Civil3d` trong tên type, ids ribbon, tên lệnh) rồi **assert bằng nhau** → sửa một bên mà quên bên kia = test đỏ. Drift **không thể im lặng** — đúng thứ B mua, với 0 thay đổi kiến trúc.
- Chi phí: copy ≈ 1 h; mirror test ≈ 2 h; mỗi fix engine-của-bridge sau này = port tay + test bắt (≈ 10 phút/fix).
- Rủi ro: hai bản `MainThreadExecutor`/`ScriptRunner` (đã qua 3 vòng review AutoCAD với fix tinh vi — wrapper finalizer crash, undo gộp) — mirror test giữ chúng đồng bộ; AutoCAD 2027 (.NET 10) đổi TFM/ALC **hai lần**.

### B — `AcadShared/` (thư viện AutoCAD-family) — **đúng ngưỡng nhưng đổi quy tắc repo, cần user**
- Folder top-level mới `AcadShared/` với 2 project: `HPAcad.McpBridge.Loader.Core` (net8.0-windows, ref `AutoCAD.NET [25.1.0]` ExcludeAssets=runtime; ALC + ribbon + actions + log; **không** phải assembly AutoCAD NETLOAD — mỗi product giữ loader mỏng ≈ 40 dòng: `[assembly: ExtensionApplication]`, `[CommandClass]`, `record AcadHostIdentity(HostId, DisplayName, ProductFolder, EnvPrefix, PipeHost, MethodPrefix, BundleName, RibbonTabId, PanelId, ButtonId, CommandName, AlcName)`) và `HPAcad.McpBridge.Runtime` (executor, runner, serializer, change counter, version map, theme switcher, status view + themes, `AcadScriptGlobals` base; nạp trong ALC riêng qua deps.json như hôm nay).
- `HPAutoCad/` và `HPCivil3d/` → `AcadShared/` → `McpShared/`. **Đổi CLAUDE.md**: chiều phụ thuộc thêm một tầng, và `AcadShared/` **tham chiếu `Autodesk.*`** (khác `McpShared/` — vẫn không phải MCP → MCP, nhưng là ngoại lệ mới).
- Chi phí: 12–16 h (researcher-03 §8) **trên sản phẩm đã verified**: refactor `HPAutoCad.McpBridge` + Loader, chạy lại toàn bộ harness AutoCAD (`run-bridge-unattended` 21, `run-live-verify` 65 + isolation, `run-aec-tools-live` 90, `run-aec-edit-tools-live` 109, `run-ribbon-check` 12) — harness có sẵn nên hồi quy **kiểm được**, nhưng phase 1 Civil bị **chặn** sau refactor này. Injection cần ở 5 file (BridgeEntry/ContextReader/SelfCheck/Globals/Serializer namespace check) — đúng những file có delta Civil.
- Lợi: fix một lần cho hai host (threading, transaction, .NET 10 2027); 1 663 dòng không lặp.

### C — Một bundle cho cả AutoCAD và Civil 3D (`Platform="AutoCAD*"` hoặc `"AutoCAD|Civil3D"`) — **loại**
Một bundle, một `PipeListener` `maxInstances=1`: AutoCAD 2026 và Civil 3D 2026 mở cùng lúc → instance thứ hai fail-fast "pipe in use" (đúng hành vi harness AutoCAD F đã đo). Muốn chọn pipe theo `CivilApplication.ActiveProduct` lúc chạy thì bundle AutoCAD phải reference `AeccDbMgd` (không có trong AutoCAD thuần → `FileNotFoundException` lúc JIT) hoặc reflection mềm — phức tạp, và hai registry root/seed set khác nhau vẫn phải tách. Loại (user cũng đã loại).

## Recommendation (Claude) — **A + mirror test cho plan này; B là follow-up có điều kiện**

Lý do, theo thứ tự nặng nhẹ:
1. **Trình tự.** B đặt refactor sản phẩm đã verified **trước** spike Civil; spike (ADR-02 token, ADR-04 rebuild/abort, `civil` global) là nơi có ẩn số thật. Repo đã đi đúng đường này với `McpShared/`: tách sau khi host thứ hai cụ thể và host thứ nhất verified (AutoCAD phase 0). YAGNI/KISS.
2. **Ngưỡng vượt mép (10 %) bởi đúng những file byte-identical** — thứ mirror test giữ đồng bộ rẻ hơn một assembly chung; delta Civil nằm ở 5 file mà B vẫn phải tiêm seam.
3. **Quy tắc repo là quyết định của user**, không phải kỹ thuật; A không chạm.
4. **Follow-up B rõ điều kiện:** sau phase 4 Civil (hai bridge cùng verified + hai harness xanh) → plan riêng "extract `AcadShared/`", gate = cả hai harness byte-identical `tools/list` + pass như trước. Khi đó mirror test trở thành **danh sách file cần tách** (đã có sẵn).

Nếu user chọn **B ngay**: phase 1 tách thành 1a (extract `AcadShared/` + hồi quy AutoCAD 5 harness) và 1b (spike Civil trên `AcadShared`); +12–16 h; CLAUDE.md sửa chiều phụ thuộc ở phase 5; mirror test không cần. Plan này ghi phase theo A; B chỉ đổi phase 1–2 (bảng ở [phase-01 §"Nếu ADR-01 = B"](../phase-01-scaffold-bundle-alc-spike-civil3d-isolation.md)).

## Consequences (A)
- `HPCivil3d/` = folder thứ năm, cùng khuôn `HPAutoCad/` (`.slnx`, `global.json`, `Directory.Build.props` mới cho `C3D\`); `../McpShared/*` là ProjectReference duy nhất ra ngoài.
- Mọi fix trong `HPAutoCad.McpBridge{,.Loader}` từ nay có mirror test nhắc port sang Civil (và ngược lại).
- Tên type giữ hậu tố host (`Civil3dScriptRunner`…) để grep phân biệt; namespace `HPCivil3d.McpBridge.*`.

## Open items
- 👤 **A hay B** (hoặc "A giờ, B sau phase 4" = khuyến nghị). Không có `[chưa xác minh]` kỹ thuật — số đo có thể tái lập bằng `wc -l` trên danh sách file ở bảng trên.
