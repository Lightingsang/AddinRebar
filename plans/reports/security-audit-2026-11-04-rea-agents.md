# Audit bảo mật: morluto/rea (npm `rea-agents` 6.8.0)

Ngày 2026-11-04. Audit tĩnh, không chạy code của REA trong lúc audit.
Trạng thái: ĐÃ CÀI global `rea-agents@6.8.0` + entry `rea` trong `.mcp.json` cục bộ. User chọn bỏ quét `dist/` và `.node` (R2).

## Kết luận

Không thấy mã độc, telemetry hay gửi dữ liệu tự động trong phần đã đọc. Kết luận có giới hạn:
`dist/` (JS thực sự chạy) và `rea-windows-x64.node` CHƯA đọc trực tiếp vì hook `scout-block` chặn.
Hai phần đó chỉ được tin qua chuỗi provenance, không phải bằng đọc nội dung.

Là công cụ dịch ngược hợp pháp nhưng quyền lớn theo thiết kế: agent có thể chạy, attach, chặn traffic của target.

## Đối tượng

| Mục | Giá trị |
|---|---|
| Bản sẽ cài | `rea-agents@6.8.0`, tag `rea-agents-6.8.0` = commit `160d821ac` |
| HEAD `main` lúc audit | `47cedcc93`, hơn tag 955 commit, CHƯA phát hành |
| Tarball | 2 398 file, 9,9 MB, sha512 `f500de46…` |
| License | MIT |

Quét source đã chạy lại trên đúng tag, không chỉ HEAD.

## Bằng chứng tốt

| Kiểm tra | Kết quả |
|---|---|
| Publish | npm Trusted Publisher (OIDC GitHub Actions), không phải token cá nhân |
| SLSA provenance | Subject sha512 khớp tarball tải về; commit nguồn = tag 6.8.0; workflow `release.yml`; runner GitHub-hosted |
| `npm audit signatures` | 149/149 package chữ ký hợp lệ, 36 có attestation |
| Lifecycle của gói | Không có `preinstall`/`install`/`postinstall`; `prepare` chỉ làm việc khi có `src/` (bản npm không có) |
| Script cài của gói phụ thuộc | 0 khi `--omit=optional`; chỉ `frida` (optional) có script cài |
| Lockfile repo | 963/963 resolved về `registry.npmjs.org` |
| Telemetry SDK | Không có (posthog/sentry/amplitude/mixpanel/otlp: mọi hit đều là `...sEntry`) |
| Đọc credential | Không đọc `.ssh`, `.aws`, `.netrc`, `.git-credentials`, Login Data, API key/token |
| Code động | Không `eval`, `new Function`, `vm.runIn` (hit duy nhất là comment) |
| Blob base64 dài | Không có trong source |
| Persistence | Không crontab/schtasks/Run key/launchctl (hit duy nhất là parser bundle Apple) |
| Tự cập nhật | Không có trong source ở HEAD; tag chỉ quét host, không thấy registry.npmjs.org |
| Skill text | Không có lệnh ẩn, không `curl`/`wget`, không prompt injection theo mẫu đã quét |
| Plugin manifest | Không có hooks, chỉ skill + MCP server, ghim version |
| Addon Windows (source) | 2 277 dòng C++: không socket, HTTP, inject, registry |
| Cổng HTTP MCP | Chỉ khi `--http`, mặc định `127.0.0.1` |

## Kết nối mạng trong source (tag 6.8.0)

Không gọi nào chạy nền. Mọi URL do người dùng hoặc target quyết định, trừ Hopper.

| Code | Đích | Khi nào |
|---|---|---|
| `HopperDownload` | `www.hopperapp.com` | Khi setup Hopper |
| `WebSourceMapFetcher`, `PlaywrightScenarioRequest` | URL target | Khi phân tích web target |
| `CdpConnection` | Cổng DevTools | Khi attach browser |
| `IdaMcpConnection` | IDA MCP đã cấu hình | Khi dùng provider IDA |
| `stateIntake` | URL https do caller đưa, cấm redirect | Khi gọi `data/add` |

`hopperapp.com` là host chạy runtime duy nhất; các host còn lại là link tài liệu, schema, repo upstream.

Dữ liệu đi ra thực tế: kết quả tool vào agent, tức gửi lên Anthropic theo policy của model.
Nội dung binary, model, DLL công ty được phân tích sẽ đi đường này. README xác nhận.

## Rủi ro

| # | Mức | Vấn đề | Xử lý |
|---|---|---|---|
| R1 | Đã xử lý | Node máy `24.15.0` < yêu cầu `≥ 24.22.1` | Đã nâng lên `24.22.2` ngày 2026-11-04 (MSI chính thức, SHA256 + chữ ký OpenJS đã kiểm) |
| R2 | Chấp nhận | `dist/` và `.node` chưa đọc. Agent sửa `.ckignore` bị auto mode từ chối (file guard của chính agent) | User quyết định 2026-11-04: bỏ quét, tin provenance + chữ ký npm |
| R10 | Trung bình | MCP có 229 tool, schema 3,8 MB; 223 tool không đánh dấu read-only, có tool chạy lệnh GDB/Rizin không giới hạn | Ghi nhận lúc cài. Xét thu hẹp bằng `REA_MCP_TOOL_FAMILIES` (CHƯA TEST) hoặc tắt server khi không dùng |
| R3 | Cao | Không phải sandbox (SECURITY.md). Agent chạy target với quyền user; REA cũng cho `frida`, mitmproxy, browser | Chỉ chạy target tin cậy, target lạ dùng VM |
| R4 | Trung bình | Tarball không có `npm-shrinkwrap.json` dù khai trong `files`: gói gián tiếp không khóa, cài mỗi lần có thể khác | Cài một lần, không dùng `npx ...@latest` |
| R5 | Trung bình | Tốc độ đổi code rất cao, 374 contributor, 955 commit trong 10 ngày | Ghim 6.8.0, audit lại khi nâng |
| R6 | Trung bình | `npm audit`: 6 cảnh báo, gốc thật là zod 4.4.3 (DoS bộ nhớ, high), minimatch/glob (moderate); không có bản fix | Chấp nhận với dùng local |
| R7 | Thấp | 428 885 sao, 106 680 fork, nhưng tác giả 12 697 follower và repo mới từ 04/2026. Lịch sử sao CHƯA kiểm được (API cần đăng nhập). Tải npm 1,5 triệu/tháng | Không dùng sao làm tín hiệu tin cậy |
| R8 | Thấp | Addon Windows build trên Linux MinGW trong CI; đường artifact sang job publish không lần ra hết trong `release.yml`, chỉ thấy `download-artifact` ở dòng 425 | Gộp vào R2 |
| R9 | Pháp lý | DISCLAIMER: chỉ dùng trên phần mềm sở hữu hoặc được phép. EULA Autodesk, Kata Pro… có thể cấm dịch ngược | User tự xác nhận |

## Chưa kiểm

- Nội dung `dist/`, `.node` (R2).
- 149 gói phụ thuộc: chỉ xác thực chữ ký, không đọc code.
- Script cài của `frida`: chưa đọc, không rõ tải binary từ đâu.
- Quét antivirus; chạy thật và theo dõi mạng runtime.
- Lịch sử sao (R7).

## Khả năng dùng trên Windows

Hopper không chạy Windows; Ghidra trên Windows còn experimental (cần Ghidra + JDK tự cài). Nhiều mục chỉ Linux x64.
Dùng được ngay: phân tích tĩnh có sẵn (app JavaScript, thư mục cài đặt desktop, Go binary).

## Đã cài

1. Node `24.22.2` (MSI chính thức).
2. `npm install --global rea-agents@6.8.0 --omit=optional`. Gói `frida` vẫn nằm trên đĩa nhưng npm 11.19 không chạy script cài của nó, nên chưa có binary Frida.
3. Entry `rea` trong `.mcp.json` (`cmd /c <AppData>\npm\rea.cmd mcp`): KHÔNG commit, file tracked chứa đường dẫn máy.
4. Không chạy `rea setup`: lệnh này sửa cấu hình nhiều AI client.

Kiểm tra: `rea --version` = 6.8.0; integrity khớp hash đã audit; `rea mcp doctor` 5/5; handshake stdio đúng lệnh trong `.mcp.json`: 229 tool.
Đã test trong Claude Code sau khi khởi động lại (Node 24.22.2): `inspect_pe_imports`, `inspect_pe_certificates` trên `HPRebar.Mcp.Server.exe`; `inspect_managed_artifact` trên `HPRebar.Core.dll`. Đều chỉ đọc tĩnh, không thực thi target.
Gotcha Windows: `inspect_managed_artifact` chỉ nhận path bắt đầu `/` và ghép vào ổ hiện tại (`/1-CONG VIEC/...` được, `F:/...` bị schema chặn, `/F:/...` sai), nên file ở ổ khác chưa thấy cách đọc. Tool PE nhận `F:/...` bình thường.
CHƯA TEST: decompile (Ghidra chưa cài), tool động, Frida.

Gỡ: xóa entry `rea` khỏi `.mcp.json`, rồi `npm uninstall --global rea-agents`.
