# Kiến trúc phân lớp — HP BIM Toolkit

## Sơ đồ chạy

```text
React page (WebView2, https://toolkit.hpbim.local, CSP chặt, asset nhúng trong DLL)
   │ postMessage JSON v1
WebMessageTransport (HPWebHost/Hosting/) ── kiểm origin, tạm dừng khi pane ẩn
   │
CommandDispatcher (HPWebHost.Core/Protocol) ── parse, timeout, mã lỗi
   │
<Module>Module (Modules/) ── validate (Core/Domain) → điều phối (Core/Application)
   │
RevitRequestQueue (ExternalEvents/, 1 ExternalEvent) ──► Services/<Reader|Navigator|Writer> trên API thread ──► DTO
   ▲
RevitEventPublisher (Bridge/) ── SelectionChanged / ViewActivated / DocumentChanged → event coalesce
```

## Project và hướng phụ thuộc

| Project | TFM | Phụ thuộc | Ghi chú |
|---|---|---|---|
| `HPWebHost.Core` | net8.0 | không gì của Revit | protocol v1 dùng chung: envelope, `RequestParser`, `CommandDispatcher`, `AssetRoutes`, `PageOrigins` (ADR-0010) |
| `HPWebHost` | net8.0-windows | HPWebHost.Core, WebView2 (compile-only), Serilog | `WebViewHost`, `EmbeddedAssetServer`, `WebMessageTransport` |
| `HPWebHost.Tests` | net8.0-windows | HPWebHost | xUnit v3 (protocol + SVG writer) |
| `HPBimToolkit.Core` | net8.0 | HPWebHost.Core | Contracts, Domain, Application, Statistics; xUnit được |
| `HPBimToolkit` | net8.0-windows, `Debug.R26`/`Release.R26` | Core, HPWebHost, Revit API, WebView2 (compile-only) | Host, Bridge, ExternalEvents, Services, Modules, Infrastructure |
| `HPBimToolkit.Core.Tests` | net8.0 | Core | xUnit v3 + generator TS |
| `HPBimToolkit.Frontend` | npm | — | mỗi cửa sổ một trang (`index.html` toolkit, `kata.html` Kata Export của HPRebar); `embed-frontend.targets` nhúng bản build vào DLL, `tools/build-frontend-once.mjs` khoá để 2 project không chạy npm cùng lúc |

Frontend → (JSON) → contracts ← add-in. Không add-in nào tham chiếu add-in khác. `HPRebar/HPRebar` (add-in thép) chỉ dùng chung `HPWebHost*` + frontend cho cửa sổ Kata Export R26 (ADR-0010, ADR-0011): giữ `KataExportViewModel` làm máy trạng thái, Core dựng bản ghi hình vẽ thuần (mm) → trang tự vẽ SVG/three.js, zoom/pan/chọn ở trang, chỉ lệnh sửa quay về host — mẫu cho cửa sổ WPF có sẵn muốn chuyển sang web mà không viết lại nghiệp vụ.

## Đặt code mới

| Loại code | Ví dụ có sẵn | Đặt ở |
|---|---|---|
| DTO request/response/event | `DashboardContracts.cs` | `Core/Contracts/<Module>Contracts.cs` |
| Quy tắc thuần, validate | `Domain/Dashboard/ShowElementsRules.cs`, `Statistics/CategoryTally.cs` | `Core/Domain/<Module>/` |
| Điều phối không cần Revit | `Application/SingleFlightRun.cs` | `Core/Application/` |
| Đăng ký lệnh + nối các lớp | `Modules/Dashboard/DashboardModule.cs` | `HPBimToolkit/Modules/<Module>/` |
| Đọc/ghi Revit | `Services/ModelStatisticsReader.cs`, `ElementNavigator.cs` | `HPBimToolkit/Services/` (static, nhận `UIApplication`, trả DTO) |
| Trang | `src/modules/dashboard/` | `src/modules/<module>/` |

Quy tắc tách: logic test được mà không cần `Document` → Core. Adapter chỉ còn gọi API + map DTO. Không tạo interface/port cho adapter chỉ để có interface: chỉ khi một use case trong Core cần gọi nó và có test thật dùng bản giả.

## Thêm module (checklist file)

1. `Core/Contracts/<Module>Contracts.cs`, tên lệnh trong `BridgeNames.cs`.
2. `Core.Tests/TypeScript/ProtocolCatalog.cs` → sinh lại `contracts.generated.ts`.
3. `Core/Domain/<Module>/*` + test.
4. `HPBimToolkit/Modules/<Module>/<Module>Module.cs` → thêm vào mảng `modules` trong `Host/ToolkitApplication.Compose` (thứ tự không quan trọng: `app.hello` đọc danh sách lệnh lúc được gọi).
5. `HPBimToolkit/Services/*`.
6. Frontend: `src/modules/<module>/`, `registry.ts` (`isAvailable`), route trong `App.tsx`, dữ liệu mẫu trong `mockBridgeClient.ts`/`mockData.ts`.
7. Harness: check mới trong `tools/harness/live-verify*.mjs`.

## Add-in Revit mới (không phải module của toolkit)

Điều kiện trước: ADR-0008 và `.claude/rules/development-rules.md` cho phép WebView2 + React với HP BIM Toolkit (và ADR-0010/0011: cửa sổ Kata Export của HPRebar, chỉ R26); add-in mới mặc định WPF + MaterialDesign. Muốn add-in mới dùng web → người dùng duyệt + ADR mới mở rộng ngoại lệ. Ưu tiên thêm module vào toolkit thay vì add-in mới.

Khi đã được duyệt: copy cấu trúc 4 project của toolkit, đổi tên + GUID `.addin` + `ContextName` + virtual host + pane id + thư mục log/WebView2 riêng (hai add-in dùng chung profile WebView2 với option khác nhau sẽ không tạo được environment). Giữ nguyên: queue, dispatcher, transport, asset server, preload, logger riêng.
