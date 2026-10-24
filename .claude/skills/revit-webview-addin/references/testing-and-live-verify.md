# Kiểm thử và live verify

## Ba tầng

| Tầng | Lệnh | Bắt cái gì |
|---|---|---|
| Core (xUnit v3) | `cd HPRebar && dotnet test HPBimToolkit.Core.Tests` (chạy từ gốc repo = 0 test, exit 0: runner MTP chỉ ghim trong `HPRebar/global.json`) | protocol, dispatcher, timeout, Domain, Application, kiểu TS lệch |
| Frontend (vitest) | `cd HPRebar/HPBimToolkit.Frontend && npm run typecheck && npm run lint && npm test && npm run build` | state, reducer, trang với MockBridge |
| Live (Revit thật) | `HPRebar/HPBimToolkit/tools/harness/` | API thread, event, modal, vòng đời, hiệu năng |

Build gate: `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` (Revit đang mở khóa DLL).

## Live harness

```bash
pwsh HPRebar/HPBimToolkit/tools/harness/run-live-verify.ps1 -Runs 1     # protocol (22 check), tự mở/đóng Revit thứ hai
pwsh HPRebar/HPBimToolkit/tools/harness/start-revit-with-toolkit.ps1 -Model <model.rvt>   # in REVIT_PID, MODEL_COPY
node HPRebar/HPBimToolkit/tools/harness/live-verify-events.mjs 9333 <pid>                 # modal → TIMEOUT, đổi view, xoá/undo
node HPRebar/HPBimToolkit/tools/harness/live-verify-lifecycle.mjs 9333 <pid> <copy> [--theme]
node HPRebar/HPBimToolkit/tools/harness/perf-statistics.mjs 9333
```

- `run-live-verify.ps1` luôn dùng bản sao `DynamoSampleModel.rvt` (không có `-Model`) và kill Revit nó mở khi xong.
- `live-verify-events.mjs` cần model đã lưu ở **view mặt bằng** (bước đổi sang `{3D}` mới có ý nghĩa): dùng `%LocalAppData%\HPBimToolkit\live-verify\prepared-site-plan.rvt` hoặc tự lưu một bản sao ở view Site.
- Revit thứ hai mở bằng `cmd /c start` (không phải con của script → dừng harness không giết Revit), biến `HPBIM_TOOLKIT_DEBUG_PORT` bật cổng CDP.
- Trang điều khiển qua CDP (WebSocket có sẵn trong Node 24); gửi request bằng `chrome.webview.postMessage` trong trang.
- Win32/UIA luôn lọc theo pid của Revit thứ hai: `revit-window.ps1` (wait/close/fill dialog, phím chỉ khi Revit đó ở trước), `revit-uia.ps1` (không đi vào cây Chromium), `revit-options.ps1` (hộp thoại Options bằng window message).
- Nút ribbon không lộ cho UIA (vd Dynamo): click toạ độ chỉ sau khi `WindowFromPoint` thuộc đúng pid.
- Ảnh trang: CDP `Page.captureScreenshot` (+ `Emulation.setDeviceMetricsOverride` 380 px), pane thật có thể nằm sau tab khác.

## An toàn (bắt buộc)

- Chỉ chạy trên **bản sao** model trong `%LocalAppData%\HPBimToolkit\live-verify\`; không bao giờ Save; đóng thì trả lời *No*.
- Không đụng Revit người dùng đang mở; DLL bị khóa → đổi tên file `.locked-<giờ>` rồi deploy.
- Không gõ phím vào hộp thoại chọn file (từng mở nhầm file dự án thật): điền bằng `WM_SETTEXT` + `BM_CLICK`.
- Cài đặt dùng chung với Revit của người dùng (theme, Options) → hỏi trước, `try/finally` trả lại.
- Ghi kết quả vào `plans/<plan>/reports/phase-NN-live-verify.md`: số check, số đo, sự cố, CHƯA TEST.
