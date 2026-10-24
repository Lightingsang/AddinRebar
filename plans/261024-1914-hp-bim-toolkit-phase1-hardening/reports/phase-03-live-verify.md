# GĐ1 — kết quả kiểm tra (2026-10-24)

## Tự động
| Check | Result |
|---|---|
| `dotnet test HPBimToolkit.Core.Tests` | 71/71 (4 test mới: file sinh khớp contract, catalog đủ lệnh, đủ event, kiểu nullable/optional/union) |
| Frontend `typecheck`, `lint`, `vitest`, `build` | pass, 28/28, bundle 210.8 KB js |
| `Build.csproj`, `Installer.csproj`, `HPRebar.slnx -c Release.R26 -p:DeployAddin=false` | 0 lỗi |
| Installer.exe trực tiếp (HPRebar R23–R27 + toolkit R26) | 2 MSI; cây 2023…2027 → HPRebar, 2026 → HPRebar + HPBimToolkit; file toolkit vào `Addins\2026\` (per-user) / `CommonAppData\…\2026\` (per-machine) |
| `dotnet run -- pack` trong git worktree tạm (DeployAddin=false, PublishAddin=true chỉ ở worktree) | lần 1: 3 bundle OK, MSI lỗi WIX0103 (icon `install\Resources\…` tương đối git root — lỗi có sẵn); sửa → lần 2 exit 0: `HPRebar.bundle.zip`, `HPRebar.McpBridge.bundle.zip`, `HPBimToolkit.bundle.zip` (manifest R2026, `Contents/2026/HPBimToolkit.addin`, DLL chứa `wwwroot/index.html` + assets), 2 MSI cùng cây feature |

## Live (Revit 2026.5 thứ hai, bản sao DynamoSampleModel)
`node live-verify-lifecycle.mjs 9333 <pid> <copy>` → **8/8 PASS**: theme dark → light (`theme.changed`, `html.dark=false`, `app.hello` = light), trả về dark; đóng tài liệu (đóng view cuối) → `document.changed {hasDocument:false}`, `getDocumentInfo` hasDocument false, `getStatistics` NO_DOCUMENT, trang "Chưa mở tài liệu"; mở lại qua Open → `document.changed` đúng path, thống kê 430 phần tử.

Dynamo 3.7.0 mở trong cùng Revit (nút ribbon không lộ cho UIA → click toạ độ sau khi kiểm `WindowFromPoint` thuộc Revit đó): `app.hello` 1 ms, `getDocumentInfo` 15 ms, `getStatistics` 1031 ms, `getSelection` 22 ms, `showElements` Curtain Wall Mullions 118/118; Library WebView2 của Dynamo "Browser Loaded", Revit responding. Log Dynamo có `FileLoadException` của `DSPythonNet3Extension` (DynamoCore 4.0) — có trong log 21/10 và 23/10, trước khi có toolkit → không liên quan.

Ảnh chụp trang trong WebView2 của Revit (CDP, 380 px): sáng + tối đúng palette; pane thật đang tab sau Properties nên ảnh cửa sổ không thấy.

## Sự cố trong lúc test
Lần mở lại đầu dùng SendKeys gõ đường dẫn vào hộp thoại Open: chữ lọt vào danh sách file (thư mục gần nhất `Desktop\Kata`), Enter mở `THCSTH-HPC-LH_BM_HC-ZZ-M3-ES-0001_detached.rvt` (file dự án thật) trong Revit thử nghiệm. Đã chọn "Ignore and continue" (link thiếu), đóng không lưu — không có hộp thoại lưu; file giữ LastWriteTime 21/10/2026 08:14. Sửa harness: `revit-window.ps1 fill-dialog` (WM_SETTEXT + BM_CLICK theo handle), không gõ phím vào hộp thoại chọn file nữa.

## Phát hiện phụ (không sửa trong GĐ này)
- `pack` dọn mọi `bin`/`obj` dưới git root + `output/` (trừ CI), và project `DeployAddin=true` copy bản Release vào `%AppData%\…\Addins\<năm>\` → ghi vào CLAUDE.md + install guide.
- `document.changed` khi mở lại tài liệu được đẩy 2 lần liền nhau (DocumentOpened + ViewActivated) — vô hại, trang xử lý idempotent.
- MSI cũ (HEAD) có cây "Revit Add-in" → năm; nay năm → add-in (WixSharp 2.14 bỏ hết Feature khi file ở cấp 3).

## Review round (code-reviewer, DONE_WITH_CONCERNS → fixed)
- High: header raw string mang CRLF của file nguồn → drift test hỏng trên checkout Windows. Sửa: `ReplaceLineEndings("\n")`; thử lại với writer + file sinh đều CRLF → 72/72.
- Medium đã sửa: kiểu không map được (Guid, DateTime, dictionary, generic, value type khác) → `NotSupportedException` (test mới); dictionary không còn bị coi là list; theme trong `try/finally` + chỉ chạy với `--theme`; `fill-dialog` nhắm ô File name (cmb13 0x47C) và chỉ Edit đang hiển thị; `CreateInstallerModule` bắt buộc từng add-in có publish.
- Low đã sửa: optional chỉ cho payload request; update mode fail một lần khi file đổi; null-check trong `revit-options.ps1`; so path đã resolve; header ghi en-US; điều kiện xem lại workaround WixSharp.
- Low để lại: TSDoc từ XML doc C# (mất chú thích scale/documentKey), nullability phần tử list, kiểm trùng tên kiểu, Manufacturer = user build (có từ trước), BM_CLICK đồng bộ, tách commit theo concern.
- Lifecycle chạy lại sau sửa (không `--theme`): 5/5 PASS.
