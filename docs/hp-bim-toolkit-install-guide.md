# HP BIM Toolkit — hướng dẫn cài đặt và phát triển (Revit 2026)

Add-in riêng `HPRebar/HPBimToolkit` (ADR-0008): Dockable Pane **HP BIM Toolkit** với giao diện web (React + TypeScript + Vite + Tailwind) chạy trong WebView2. Module 1 = Dashboard.

## Yêu cầu

| Thành phần | Phiên bản | Ghi chú |
|---|---|---|
| Revit | 2026 | Dùng WebView2 1.0.2478.35 có sẵn trong thư mục cài Revit — add-in không mang theo WebView2 |
| Microsoft Edge WebView2 Runtime | bất kỳ bản Evergreen | Có sẵn trên Windows 11 |
| .NET SDK | 10.0.300 (theo `HPRebar/global.json`) | build cho net8.0-windows |
| Node.js | ≥ 24 (npm ≥ 11) | chỉ cần khi build; MSBuild tự chạy `npm ci` + `npm run build` |

## Build và cài cho máy dev

```bash
dotnet build HPRebar/HPBimToolkit/HPBimToolkit.csproj -c Debug.R26
```

- Build frontend (`HPRebar/HPBimToolkit.Frontend/dist`) rồi nhúng vào `HPBimToolkit.dll` (resource `wwwroot/...`). Không có file web rời.
- Debug tự deploy vào `%AppData%\Autodesk\Revit\Addins\2026\` (`HPBimToolkit.addin` + thư mục `HPBimToolkit\`).
- Revit đang mở khóa DLL → đóng Revit rồi build lại, hoặc `-p:DeployAddin=false` chỉ để kiểm tra biên dịch.
- Không có Node: build frontend một lần ở máy khác rồi `-p:SkipFrontendBuild=true` (nhúng `dist/` có sẵn). `dist/` trống → pane hiện trang "chưa build".
- Lần đầu Revit hỏi "Security - Unsigned Add-In" (DLL chưa ký) → chọn *Always Load* (hoặc *Load Once*).

## Cài thủ công (máy không build)

Copy từ `HPRebar/HPBimToolkit/bin/Release.R26/publish/` (hoặc Debug): `HPBimToolkit.addin` và thư mục `HPBimToolkit\` vào `%AppData%\Autodesk\Revit\Addins\2026\`. Gỡ: xoá hai mục đó.

## Sử dụng

Ribbon **HPRebar ▸ BIM Toolkit ▸ Dashboard** → pane mở (dock phải, Revit nhớ vị trí). Dashboard: tài liệu + view hiện hành, chỉ số mô hình, phần tử theo category (bấm dòng = chọn trong Revit, kính lúp = chọn + phóng), số phần tử đang chọn, công cụ nhanh (Cảnh báo, Thông tin dự án, Đơn vị, Visibility/Graphics, Quản lý link, Worksets, Purge). Thống kê chỉ chạy khi mở pane, bấm **Làm mới**, đổi phạm vi hoặc đổi tài liệu; sửa mô hình chỉ hiện nhãn "Dữ liệu cũ".

## Dữ liệu trên máy

| Đường dẫn | Nội dung |
|---|---|
| `%LocalAppData%\HPBimToolkit\logs\toolkit-YYYYMMDD.log` | log riêng (giữ 7 ngày) |
| `%LocalAppData%\HPBimToolkit\WebView2\` | profile trình duyệt WebView2 |

## Phát triển giao diện

```bash
cd HPRebar/HPBimToolkit.Frontend
npm ci
npm run dev        # http://localhost:5173 với MockBridge (dữ liệu mẫu)
npm test           # vitest
npm run typecheck
npm run lint
```

- Biến môi trường trước khi mở Revit: `HPBIM_TOOLKIT_DEV_URL=http://localhost:5173/` → pane tải từ Vite dev server (chỉ chấp nhận localhost/127.0.0.1) để hot reload với Revit thật.
- `HPBIM_TOOLKIT_DEBUG_PORT=9333` → bật DevTools + cổng remote debugging (dùng cho harness), không bật ở máy người dùng.

## Kiểm thử

```bash
dotnet test HPRebar/HPBimToolkit.Core.Tests
```

Live (Revit thứ hai trên **bản sao** model, không đụng Revit đang mở):

```bash
pwsh HPRebar/HPBimToolkit/tools/harness/run-live-verify.ps1 -Runs 3
```

Sự kiện và đường lỗi (hộp thoại modal → TIMEOUT, đổi view, xoá/undo → "Dữ liệu cũ"): mở Revit thử nghiệm trên một model đã lưu ở view mặt bằng, rồi chạy (thay `<pid>` bằng `REVIT_PID` script in ra):

```bash
node HPRebar/HPBimToolkit/tools/harness/live-verify-events.mjs 9333 <pid>
```

Hiệu năng trên model đang mở: `node HPRebar/HPBimToolkit/tools/harness/perf-statistics.mjs 9333`.

## Mở rộng module mới

1. C#: thư mục `HPBimToolkit/Modules/<Tên>/` với lớp `: IToolkitModule` đăng ký lệnh `<module>.<verb>` trên `CommandDispatcher`; đọc/ghi Revit qua `RevitRequestQueue`; thêm vào mảng `modules` trong `ToolkitApplication.Compose` (trước `AppModule`).
2. Contracts: DTO trong `HPBimToolkit.Core/Contracts`, tên lệnh trong `BridgeNames.cs`, test hình dạng JSON trong `WireFormatTests`.
3. Frontend: kiểu trong `src/bridge/protocol.ts` (`CommandMap`/`EventMap`), trang trong `src/modules/<tên>/`, bật `isAvailable` trong `src/modules/registry.ts` và thêm route trong `App.tsx`.
