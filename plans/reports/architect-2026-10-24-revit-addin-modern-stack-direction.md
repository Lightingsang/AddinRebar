# Đề xuất hướng phát triển — Revit Add-in stack hiện đại (WebView2 + React + backend)

2026-10-24. Loại: phân tích / đề xuất — **Planned, chưa đổi code**. Nền thực tế: HP BIM Toolkit Module 1 (commit 9098fba, verified live trong Revit 2026; xem [phase-06-live-verify.md](../261024-0640-hp-bim-toolkit-dashboard/reports/phase-06-live-verify.md), [ADR-0008](../../docs/architecture/adr/0008-bim-toolkit-webview2-ui.md)).

## 1. Kết luận nhanh

- Giữ nền đã có: WebView2 (bản Revit ship) + React/TS/Vite/Tailwind trong dockable pane, bridge JSON v1, **một** hàng đợi ExternalEvent, Core .NET thuần có test.
- Angular: không (nặng, DI/RxJS thừa cho một pane; React đã chạy).
- NestJS: không mặc định. Đội C# → ASP.NET Core đơn giản hơn (một ngôn ngữ, auth Windows có sẵn, chung kiểu dữ liệu .NET).
- PostgreSQL + Docker: chỉ khi có nhu cầu dữ liệu **dùng chung nhiều người** (cổng quyết định GĐ5). Trước đó local-first.
- Thứ tự tính năng: Element Explorer → Parameter Manager (chuẩn hoá đường ghi) → Model Health/QA + Reports → server (chuẩn công ty, lịch sử, dashboard đa dự án).

## 2. Kiến trúc tổng thể

```text
React page (WebView2, https://toolkit.hpbim.local, CSP chặt, asset nhúng trong DLL)
   │ postMessage JSON v1 {request | response | event}
WebMessageTransport ── kiểm origin, cap kích thước
   │
CommandDispatcher (Core) ── parse, validate, timeout, map lỗi
   │
Modules/<X> ── đăng ký lệnh ──► Services (adapter Revit → DTO)
   │                                 │
RevitRequestQueue (1 ExternalEvent) ─┴─► Revit API thread
RevitEventPublisher ── Selection/View/DocumentChanged → event coalesce → page
   │ (GĐ5, tuỳ chọn)
ServerClient (HttpClient, token DPAPI) ──HTTPS/OpenAPI──► ASP.NET Core API ──► PostgreSQL
```

| Tầng | Được làm | Không được làm |
|---|---|---|
| Frontend | hiển thị, state UI, gọi lệnh bridge | gọi Revit, giữ token, gọi mạng ngoài |
| Bridge / Dispatcher (Core) | protocol, validate, timeout, mã lỗi | tham chiếu `Autodesk.*` |
| Module | ghép lệnh ↔ service | thuật toán (đưa xuống Core) |
| Service | đọc/ghi Revit, trả DTO | trả object Revit, chạy ngoài API thread |
| Core logic | tính toán, quy tắc, gom nhóm | I/O Revit |

## 3. So sánh phương án

| Hạng mục | Phương án | Chọn | Lý do |
|---|---|---|---|
| UI host | WPF+MaterialDesign / WebView2+web | WebView2 cho tool nhiều dữ liệu; WPF cho dialog nhỏ | bảng/biểu đồ/responsive dễ hơn; dùng lại cho web dashboard; ADR-0008 |
| Framework | React / Angular / Svelte | React | đã chạy; hệ sinh thái lớn; bundle nhỏ (209 KB) |
| CSS | Tailwind / MUI / Bootstrap | Tailwind | đã chạy; nhẹ; theme qua CSS variable theo Revit |
| State | hooks+reducer / Redux / Zustand | hooks+reducer | đủ cho module độc lập; thêm store nhỏ khi state chung giữa module |
| Bridge | WebMessage JSON / `AddHostObjectToScript` (COM) / HTTP localhost | WebMessage | async, không lộ COM, không mở cổng, dễ test |
| Backend | không / ASP.NET Core / NestJS | không → ASP.NET Core khi cần | một ngôn ngữ; Negotiate auth; EF Core + Npgsql |
| DB | JSON/SQLite local / PostgreSQL | local trước; Postgres trên server | Postgres: JSONB cho kết quả QA, đa người dùng |
| Deploy server | Docker compose / IIS | Docker compose | tái lập API + Postgres + proxy; IIS nếu IT bắt buộc |
| Realtime | polling / SignalR | không, rồi SignalR | chỉ khi có tính năng cộng tác trực tiếp |

## 4. ExternalEvent — quy tắc bắt buộc

1. Một ExternalEvent, hàng đợi FIFO, kiểm kết quả `Raise()`.
2. Timeout phía host (đọc 30 s, thống kê 120 s); hết hạn → huỷ token **trước** khi trả lời, request chưa chạy bị bỏ.
3. Đọc lớn: collector một lượt, filter nhanh (`OfCategory`/`OfClass`, `ElementParameterFilter`) trước filter chậm, kiểm cancel ~2 000 phần tử, paging `limit ≤ 500`.
4. Ghi: preview (đọc + diff, không transaction) → apply trong `TransactionGroup "HP: <tác vụ>"` = 1 Undo; nhiều phần tử → chia lô qua nhiều tick, event `progress` + Huỷ; chính sách lỗi lô ghi rõ từng tool (rollback cả nhóm hay giữ phần đã ghi).
5. Tránh API mở dialog modal: `ShowElements` với phần tử không view nào thấy, `SetElementIds` > 5 000 (đã gặp live — treo 18 s / chặn API thread).
6. Event Revit chỉ O(1) (đếm id), coalesce 150–400 ms, dừng khi pane ẩn, replay khi hiện.
7. Không chờ Task trên UI thread; xử lý message ngoài COM callback (`Dispatcher.BeginInvoke`); preload dependency ở `OnStartup` (đã sửa deadlock thật).

## 5. Quản lý dữ liệu

- Nguồn sự thật = model Revit. Frontend chỉ giữ snapshot DTO + cờ `stale`.
- Định danh: `ElementId` (long) trong phiên; `UniqueId` khi lưu ngoài phiên / gửi server; document key = đường dẫn central/cloud (GUID Extensible Storage chỉ khi user đồng ý ghi vào model).
- Lưu cục bộ: setting user → `%AppData%\HPBimToolkit\settings.json`; setting theo model → Extensible Storage; lịch sử cần query → SQLite (repo đã dùng ở MCP registry).
- Quy tắc công ty (đặt tên, tham số bắt buộc, QA) → JSON có version: mặc định nhúng + override thư mục chia sẻ; GĐ5 chuyển lên server.
- Contract: C# record là nguồn → sinh types TS bằng script nhỏ hoặc fixture JSON test cả hai phía; `v` + `capabilities` trong `app.hello`; thêm field = additive, đổi nghĩa = v2.

## 6. Hiệu năng

- Đã đo: model 73k phần tử — thống kê 2,6 s lạnh / 0,7 s ấm; event selection O(1).
- Cache theo document, vô hiệu bằng `ModelChangeTally`; không quét lại khi selection đổi.
- Frontend: lazy route theo module; bảng lớn dùng virtual list (thêm thư viện nhỏ khi Element Explorer cần).
- Log thời gian mỗi lệnh (đã có) → ngưỡng cảnh báo trong report.

## 7. Bảo mật

Add-in:
- Trang chỉ từ resource nhúng, virtual host, CSP `default-src 'self'`, không `eval`, chặn điều hướng / cửa sổ mới, kiểm origin mọi message (đã có).
- Allow-list lệnh; validate payload trong Core; lệnh ghi: preview + xác nhận; audit log (user, giờ, lệnh, số phần tử).
- Token server giữ ở C# (MSAL cache / DPAPI), không đưa vào JS; page gọi server qua lệnh proxy `server.*` → CSP giữ `connect-src 'self'`.
- Ký số DLL → bỏ prompt "publisher could not be verified", chống thay DLL.

Server:
- HTTPS; auth Windows (Negotiate) trên LAN hoặc Entra ID OIDC; phân quyền theo dự án/vai trò.
- Postgres user quyền tối thiểu; secrets qua env / Docker secrets; backup định kỳ + **thử restore**; migration có version.
- Chỉ lưu số liệu cần thiết (chỉ số, UniqueId, kết quả QA), không lưu model.

## 8. Mở rộng & bảo trì

- Module mới = `Modules/<X>/` (C#) + `src/modules/<x>/` (React) + một dòng đăng ký mỗi phía; dùng chung transport, queue, services, UI kit.
- Test 3 tầng: Core xUnit, frontend vitest, live harness CDP trên Revit thứ hai + bản sao model (đã có).
- Đa phiên bản: R26 trước; R25 (.NET 8) / R27 (.NET 10) cần kiểm bản WebView2 Revit ship — GIẢ ĐỊNH CHƯA XÁC MINH.
- Đóng gói: toolkit chưa có trong `dotnet run -- pack` (bundle/MSI) — việc đầu tiên của GĐ1.
- Governance repo (ADR-0007, CODE_REVIEW_CHECKLIST) cho C#; ESLint + typecheck cho TS.

## 9. UI/UX

- Pane 280–900 px: 1 cột < 480 px, 2 cột khi rộng; mật độ compact giống Revit; sáng/tối theo Revit.
- Mỗi màn có đủ: loading (skeleton), empty, error + Thử lại, badge "dữ liệu cũ", toast kết quả.
- Ghi: Xem trước (bảng cũ → mới) → Áp dụng → toast + tên Undo; số lớn → xác nhận; lâu → progress + Huỷ.
- Click hàng = chọn trong Revit; double-click = zoom; isolate tạm thời.
- UI kit riêng nhỏ (Card, StatTile, StateView, Toast đã có); Radix primitives chỉ cho dialog/combobox cần a11y; không MUI.
- Nhãn tiếng Việt, số theo vi-VN, đơn vị rõ (mm, m³, kg); phím tắt Ctrl+F lọc, Esc đóng, Enter áp dụng.

## 10. Tính năng đề xuất

| # | Tính năng | Giá trị | Server |
|---|---|---|---|
| 1 | Element Explorer: lọc category/tham số, bảng ảo, chọn/zoom/isolate, xuất Excel | tra cứu nhanh | Không |
| 2 | Parameter Manager: sửa hàng loạt + preview + 1 Undo; nhập/xuất Excel; quy tắc điền (Mark theo tầng/trục) | tiết kiệm giờ công | Không |
| 3 | Model Health/QA: warning theo loại + chọn, in-place family, CAD import, view thừa, kiểm đặt tên theo JSON, điểm sức khoẻ | chất lượng hồ sơ | Lịch sử: có |
| 4 | Reports: khối lượng bê tông/thép theo tầng, cấu kiện → xlsx | bóc khối lượng | Không |
| 5 | View/Sheet Manager: đổi tên hàng loạt, tạo sheet, đặt view | hồ sơ | Không |
| 6 | Launcher HPRebar: mở Column/Beam/Foundation/Kata từ pane | một chỗ làm việc | Không |
| 7 | Chuẩn công ty tập trung + lịch sử QA + dashboard web cho BIM manager (dùng lại component React) | quản lý | Có |
| 8 | Issue gắn UniqueId (sau có thể BCF) | phối hợp | Có |

## 11. Cấu trúc dự án

```text
HPRebar/
├── HPBimToolkit/            add-in: Host/ Bridge/ ExternalEvents/ Services/ Modules/<X>/ Infrastructure/
├── HPBimToolkit.Core/       contracts, protocol, logic thuần theo module (Explorer/, Parameters/, Health/)
├── HPBimToolkit.Core.Tests/
└── HPBimToolkit.Frontend/   src/bridge/ src/ui/ src/modules/<x>/ (lazy)
HPBimServer/                 chỉ khi qua cổng GĐ5 — folder riêng, slnx + global.json riêng
├── HPBimServer.Api/         ASP.NET Core minimal API + OpenAPI
├── HPBimServer.Data/        EF Core + Npgsql, migrations
├── HPBimServer.Tests/       xUnit + Testcontainers Postgres
└── deploy/                  docker-compose.yml (api, postgres, reverse proxy), backup
```

Phụ thuộc: Frontend → bridge → Core; add-in → Core; add-in ↔ server chỉ qua HTTP/OpenAPI, không project reference chéo folder.

## 12. Lộ trình

| GĐ | Nội dung | Cỡ | Cổng ra |
|---|---|---|---|
| 0 | Nền + Dashboard | — | ✅ Verified (9098fba) |
| 1 | Hardening: pack bundle/MSI, ký số, test còn thiếu (theme, Dynamo, đóng tài liệu), types TS sinh tự động, lazy module | S | cài từ MSI trên máy sạch, harness xanh |
| 2 | Element Explorer | M | model 73k: lọc < 1 s ấm, paging, chọn/zoom không treo |
| 3 | Parameter Manager — chuẩn đường ghi (preview → lô → Undo → progress/cancel) | M–L | 10k phần tử ghi có Huỷ, 1 Undo, audit log |
| 4 | Model Health/QA + Reports | M | quy tắc JSON, xlsx khớp schedule Revit |
| 5 | Cổng server: ≥ 2 nhu cầu dùng chung thật? Có → API + Postgres + Docker compose, auth Windows/Entra, proxy qua host | M | IT có người vận hành, backup/restore đã thử |
| 6 | Đa phiên bản R25/R27, web dashboard dùng lại component | M | build + live mỗi bản |

## 13. Rủi ro

| Rủi ro | Ảnh hưởng | Xử lý |
|---|---|---|
| WebView2 gắn bản Revit ship (1.0.2478.35 ở R26) | nâng Revit đổi SDK | compile-only theo bản, harness live mỗi bản |
| Xung đột DLL giữa add-in (Serilog loose, WebView2 env) | log lẫn, pane không mở | logger riêng, user data folder riêng, ContextName |
| Deadlock UI thread / dialog modal | Revit treo | preload, BeginInvoke, tránh API mở dialog, timeout |
| Model lớn | chậm / treo | paging, cancel, xác nhận > 5 000 |
| Server tăng gánh vận hành | chi phí, bề mặt tấn công | cổng GĐ5, local-first |
| Hai stack UI song song | bảo trì đôi | WebView2 chỉ cho tool nhiều dữ liệu; dialog nhỏ giữ WPF + MaterialDesign |

## Câu hỏi mở (chỉ khi tới GĐ5)

- Backend .NET (khuyến nghị) hay NestJS (nếu có dev TS backend riêng)?
- Hạ tầng: server nội bộ Docker hay IIS; auth Windows hay Entra ID?
