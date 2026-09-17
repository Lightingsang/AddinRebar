# ADR-02 — Bundle riêng `HPCivil3d.McpBridge.bundle`, `Platform="Civil3D"`, pipe `hpcivil3d-mcp-2026`, hai product chạy song song không đụng nhau

**Ngày:** 2026-09-17 · **Status:** Proposed (spike S-01…S-03 phase 1 quyết Accepted) · **Owner:** HPCivil3d
**Kế thừa:** [AutoCAD ADR-05 §2 (`Platform="AutoCAD"` để né Civil 3D)](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · [ADR-06 định danh](adr-06-server-profile-client-wiring-ribbon-identity.md)
**Bằng chứng:** [E1, E4, E13, E14, E15](../research/evidence-on-machine-2026-09-17.md) · [researcher-02 §1, §6](../research/researcher-02-civil3d-autoloader-launch-rebuild-facts.md) · `HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml:13–15` (comment giải thích `Platform="AutoCAD"`) · `HPAutoCad/tools/harness/run-live-verify.ps1:116–132` (isolation 2: Civil 3D không nạp bundle AutoCAD — pass 2026-09-14) · `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:41–48`.

## Context
- Ba product R25.1 chung `acad.exe` và thư mục cài (E1); autoloader phân biệt **chỉ** bằng `RuntimeRequirements.Platform` (E15). Bundle AutoCAD dùng `Platform="AutoCAD"` chính vì để không nạp vào Civil 3D và tranh pipe (ADR-05 AutoCAD). Bundle Civil phải làm điều ngược lại — và **cả hai** phải sống chung trong `%AppData%\Autodesk\ApplicationPlugins\` (AutoCAD 2026 chỉ quét `%AppData%` — researcher-02 §6 `[chưa xác minh]`; trên máy này mọi bundle AutoCAD-family đều ở `%AppData%` — E4).
- Autodesk Civil 3D DevGuide nói thẳng: *`Platform="Civil3D"` + SeriesMin/Max = "loaded and only loaded in Civil 3D"* (E15). Token list chính thức: `AutoCAD, AutoCAD*, ACA/ADT, ACADE, ACADM, Civil, Civil3D, LDT, Map, MEP, Plant3D, PNID…` — **`Civil3D`**, không phải `C3D` (đó là `/product`), không phải `Civil` (Autodesk Civil cũ).
- Không bundle nào trên máy dùng `Civil3D` (E4) → chiều "AutoCAD thuần/Advance Steel **không** nạp" chưa có precedent local → spike.
- Hai plugin MCP cũ của user (`Civil3dMcp.bundle`, `AutoCadMcp.bundle`, `Platform="AutoCAD*"`) nạp vào **cả hai** product (E4, E14) — ngoài kiểm soát của ta, không đụng.

## Decision

### 1. `PackageContents.xml` (NEW `HPCivil3d/HPCivil3d.McpBridge.Loader/Bundle/PackageContents.xml`)
```xml
<ApplicationPackage SchemaVersion="1.0" AutodeskProduct="AutoCAD" ProductType="Application"
                    Name="HPCivil3d MCP Bridge" AppVersion="0.1.0"
                    Description="MCP bridge: AI-generated C# runs inside Civil 3D after the user opts in"
                    ProductCode="{<GUID mới, sinh ở phase 1, cố định>}">
  <CompanyDetails Name="HPRebar" />
  <Components Description="Civil 3D 2026 (.NET 8)">
    <RuntimeRequirements OS="Win64" Platform="Civil3D" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="HPCivil3d.McpBridge" Version="0.1.0"
                    ModuleName="./Contents/HPCivil3d.McpBridge.Loader.dll" AppType=".NET" LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
```
- `AutodeskProduct="AutoCAD"` giữ (thuộc tính mô tả, Autodesk sample Civil cũng dùng — researcher-02 §1); `Platform` mới là bộ lọc.
- Deploy `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\{PackageContents.xml, Contents\HPCivil3d.McpBridge.Loader.dll, Contents\Bridge\**}` bằng target `DeployBundle` copy từ Loader csproj AutoCAD (`-p:DeployBundle=false` khi Civil 3D mở). Xoá folder = gỡ.
- SECURELOAD: AutoCAD hỏi "Unsigned Executable" cho `Loader.dll` lần đầu (mỗi hash mới cho tới khi *Always Load* — hành vi đã đo ở AutoCAD ADR-05 §2); Civil 3D dùng cùng cơ chế `[chưa xác minh trong Civil]` → harness `Answer-SecureLoad` giữ nguyên.

### 2. Pipe / method / cách ly runtime
- Pipe `hpcivil3d-mcp-2026` (`PipeNaming.For("civil3d", 2026)` — nhánh mặc định đã sinh đúng; phase 0 thêm hằng cho tên), method prefix `civil3d.` (`JsonRpcMethods.Civil3dPrefix`), dispatcher theo suffix nên `civil3d.execute` đi cùng đường `autocad.execute` (`RequestDispatcher` không sửa).
- Hai `acad.exe` (AutoCAD 2026 + Civil 3D 2026) chạy cùng lúc = hai bundle khác nhau, hai pipe khác tên, hai settings/log/audit root (`HPAutoCad\…` vs `HPCivil3d\…`), hai server exe, hai registry root. **Không có tài nguyên chung** ngoài `%AppData%\Autodesk\ApplicationPlugins\` (đọc) và Roslyn cache trong RAM mỗi process.
- Instance Civil 3D **thứ hai** → `PipeListener` fail-fast → cửa sổ trạng thái "already in use — another Civil 3D 2026 instance" (text từ `RequestDispatcher.HostName/HostVersion`, như AutoCAD F) + log sink `shared: true`.

### 3. Loader ALC (copy `BridgeLoadContext.cs`, một dòng đổi)
`HostAssemblyPrefixes = ["Ac", "Ad", "Aec", "Autodesk."]` — thêm `"Aec"` để **không bao giờ** nạp bản thứ hai của `AeccDbMgd`/`AecBaseMgd`/`AecPropDataMgd` từ `Contents\Bridge\` dù deps.json có ghi (bridge reference `Private=false` nên không copy; đây là belt-and-braces như comment gốc `BridgeLoadContext.cs:29–33`). `Autodesk.AECC.Interop.*` cũng rơi vào `"Autodesk."`.

### 4. Fallback nếu autoloader không lọc đúng (spike S-01/S-02 fail)
Demand-load theo product key: `HKCU\Software\Autodesk\AutoCAD\R25.1\ACAD-9100:409\Applications\HPCivil3d.McpBridge` (`LOADER`, `LOADCTRLS=2`, `MANAGED=1`) — key này tồn tại và autoloader tự ghi bundle vào đó (E14) → chỉ Civil 3D (9100) đọc, AutoCAD (9101)/Advance Steel (9126) không. Không dùng cho MVP nếu S-01/S-02 pass (bundle là chuẩn Autodesk; registry cần installer).

### 5. Multi-version
| Đích | Trạng thái | Cách mở |
|---|---|---|
| **Civil 3D 2026 (R25.1, .NET 8)** | **MVP** | — |
| Civil 3D 2025 (R25.0, .NET 8) | ngoài MVP | `SeriesMin="R25.0"`, `AutoCAD.NET [25.0.1]`, `AeccDbMgd` 2025 từ `C:\Program Files\Autodesk\AutoCAD 2025\C3D\`, pipe `hpcivil3d-mcp-2025`, `ValidVersions [2025, 2026]` |
| 2026 Update .NET 10 / 2027 | ngoài scope | TFM `net10.0-windows`, `[25.1.1]`/`[26.0.0]`; ALC riêng vẫn cần (AutoCAD ship Roslyn 4.10) |

## Alternatives rejected
- `Platform="AutoCAD*"` hoặc `"AutoCAD|Civil3D"` cho bundle Civil: nạp vào AutoCAD thuần → `AeccDbMgd` không có → `FileNotFoundException` khi JIT `BridgeEntry`, hoặc phải reflection mềm; và tranh pipe với bundle AutoCAD. Loại (ADR-01 §C).
- Mở rộng bundle AutoCAD sang `"AutoCAD|Civil3D"` và chọn pipe runtime theo `CivilApplication.ActiveProduct`: bridge AutoCAD phải reference Civil API — phá "AutoCAD MCP chạy trên máy không có Civil 3D". Loại.
- Sửa bundle lạ `Civil3dMcp.bundle` của user để tránh nhiễu harness: **không** — quy tắc "never touches the foreign plugin" (Navis).

## Consequences
- Spike phase 1 (gate): S-01 Civil 3D nạp (loader.log + self-check OK); S-02 AutoCAD 2026 thuần (`/product ACAD`) **không** nạp (loader.log không tăng dòng); S-02b Advance Steel (`/product "ADVS" /p "<<ADVS>>"`) không nạp; S-03 AutoCAD 2026 + Civil 3D 2026 cùng mở → `get_autocad_context` và `get_civil3d_context` cùng trả lời, mỗi bên host đúng. Fail S-01/S-02 → §4 fallback + cập nhật ADR.
- Harness AutoCAD isolation 2 (Civil 3D không nạp bundle AutoCAD) **vẫn phải pass** sau khi bundle Civil tồn tại — loader.log của AutoCAD là file khác (`%LocalAppData%\HPAutoCad\McpBridge\logs\loader.log`); harness Civil kiểm chiều ngược.
- Người dùng có 2 plugin MCP cũ nạp vào Civil 3D: harness ghi nhận dialog/log lạ nếu có, không xử lý.

## Open items `[chưa xác minh]`
- Chiều "không nạp vào AutoCAD thuần / Advance Steel" (semantics docs, chưa có precedent local) — S-02/S-02b.
- AutoCAD 2026 chỉ quét `%AppData%` (bỏ `%ProgramData%`) — researcher-02 dẫn forum; không ảnh hưởng vì ta deploy `%AppData%`.
- SECURELOAD prompt trong Civil 3D giống AutoCAD — S-01 quan sát.
