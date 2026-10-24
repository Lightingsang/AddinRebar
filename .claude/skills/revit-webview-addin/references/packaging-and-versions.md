# Đóng gói, phiên bản Revit, backend

## WebView2

- Dùng bản Revit ship (Revit 2026: 1.0.2478.35 cạnh `Revit.exe`). `PackageReference Microsoft.Web.WebView2` đúng version đó, `ExcludeAssets="runtime;native;build;buildTransitive"` → không copy DLL nào, không ILRepack.
- Runtime = Edge WebView2 Evergreen của Windows.
- Profile riêng: `%LocalAppData%\<Add-in>\WebView2`; cổng debug → profile riêng `WebView2-debug-<port>`.
- Phiên bản Revit khác: kiểm bản WebView2 trong thư mục cài trước khi thêm cấu hình `R25`/`R27`; mỗi phiên bản cần live verify riêng.

## Frontend vào DLL

- MSBuild: `npm ci` khi lock đổi, `npm run build` khi source mới hơn `dist/index.html`, nhúng `dist/**` thành `wwwroot/<path>` (`WithCulture=false`); `-p:SkipFrontendBuild=true` dùng `dist/` có sẵn.
- Phục vụ qua `WebResourceRequested` trên virtual host + CSP chặt; không file web rời, không CDN.

## Gói phát hành

```bash
cd HPRebar/build && dotnet run -- pack    # → output/: *.bundle.zip + HPRebar-<ver>-{SingleUser,MultiUser}.msi
```

- Bundle riêng cho mỗi add-in (`CreateBundleModule.addins`). MSI: `CreateInstallerModule.wixTargets`; cây feature năm → add-in (WixSharp 2.14 bỏ hết Feature khi file ở cấp 3).
- `pack` dọn mọi `bin`/`obj` dưới git root (cả `node_modules/*/bin`) + `output/`, và project `DeployAddin=true` copy bản Release vào `%AppData%\Autodesk\Revit\Addins\<năm>\` → chạy trên clone/worktree sạch, Revit đóng. Lỡ chạy trên cây dev: `node_modules/typescript/bin`, `vite/bin` bị xoá mà `InstallFrontend` không chạy lại (lock không đổi) → `npm ci` trong `HPBimToolkit.Frontend` trước khi build. Kiểm thử pipeline: git worktree tạm + thêm `("DeployAddin","false"),("PublishAddin","true")` vào `CompileProjectModule` chỉ trong worktree.
- Kiểm gói: giải nén bundle (manifest `SeriesMin/Max`, DLL có `wwwroot/index.html`), `wix msi decompile` xem `Feature`.
- DLL chưa ký → Revit hỏi "Unsigned Add-In"; ký số cần chứng thư công ty.

## Backend (chỉ khi cần)

Cổng: ≥ 2 nhu cầu dữ liệu dùng chung thật (chuẩn công ty tập trung, lịch sử QA, dashboard đa dự án) + có người vận hành server.

| Phần | Chọn | Lý do |
|---|---|---|
| API | ASP.NET Core minimal API (NestJS chỉ khi có đội TS backend) | cùng ngôn ngữ với add-in, auth Windows/Entra có sẵn |
| DB | PostgreSQL (EF Core + Npgsql) | đa người dùng, JSONB cho kết quả QA |
| Triển khai | Docker compose (api + postgres + reverse proxy) trên server nội bộ | tái lập, backup/restore thử được |
| Gọi từ add-in | C# `HttpClient`, token MSAL/DPAPI, trang gọi qua lệnh `server.*` | token không vào JS, CSP giữ `connect-src 'self'` |

Trước cổng: lưu cục bộ (`%AppData%` JSON, Extensible Storage theo model, SQLite khi cần truy vấn).
