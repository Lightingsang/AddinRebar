# ADR-05 — Tool cố định = script template trong library, không phải command native

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed: "mở rộng mcp bridge thành hệ thống AI BIM tự tạo và ghi nhớ tool") · **Owner:** HPRebar

## Context

- Phase 2 chốt 4 tool, không `create_*`. User sau đó yêu cầu (1) thêm 21 tool của `mcp-servers-for-revit`, (2) hệ thống tự sinh + ghi nhớ tool. Hai yêu cầu chung một câu hỏi: **tool cố định sống ở đâu?**
- Repo tham khảo: mỗi tool = class `ExternalEventCommandBase` trong DLL commandset, nạp qua `command.json` bằng `Assembly.LoadFrom` (không unload), chạy ngoài guard/audit/dryRun. Thêm tool = build + redeploy + mở lại Revit.
- Bridge của mình đã có một đường thực thi hoàn chỉnh: guard → compile (cache SHA-256) → ExternalEvent → TransactionGroup + dryRun + timeout + audit.

## Decision

1. **Tool = dữ liệu**: `tool.json` (metadata, `inputSchema`, `transaction`, `timeoutSeconds`) + `code.cs` (thân script với globals của `execute_revit_code`) + `examples.json`. Chạy tool = `revit.execute(code, args)` — đường cũ, không đổi.
2. **Tham số là data, không phải code**: `ExecuteRequest.Args` (JSON) → `ScriptGlobals.args` (`ScriptArgs`: `Double/Int/Str/Bool/List/Obj/Require`, case-insensitive, coerce). Text script không đổi giữa các lần gọi → compile **1 lần / phiên Revit**.
3. **Không command native nào cho tool** trong `HPRebar.McpBridge`. Bridge chỉ thêm `revit.analyze` (guard + compile + literal/args-key, pipe thread) phục vụ validate/propose; không đụng Revit thread.
4. 21 tool tham khảo → **seed** nhúng trong server (`Registry/SeedLibrary/**`), cài vào library lần đầu; `_seeds.json` ghi checksum để nâng cấp seed **chưa bị user sửa**, không bao giờ ghi đè seed đã sửa.
5. Script có thể gọi tên kiểu `ScriptArgs` (import mặc định `HPRebar.McpBridge.Core.Scripting` từ lần deploy sau); seed dùng tên đầy đủ để chạy được với bridge cũ.

## Consequences

- Thêm/sửa tool: sửa file, không build, không đóng Revit. Mọi tool đều có dryRun/timeout/Undo group/audit miễn phí.
- Test không cần Revit: mỗi seed được compile bằng `ref/net8.0-windows7.0/RevitAPI.dll` (metadata) trong xUnit; wrapper test **phải** khớp import mặc định của bridge (bài học: `ScriptArgs` CS0246 chỉ lộ trong Revit).
- Giới hạn: tool = 1 script ≤ 32 KB, không chia file, không tham chiếu NuGet; `inputSchema` trong subset (string/number/integer/boolean/array/object, sâu ≤ 3).
- Đảo quyết định phase 2 có chủ ý: bề mặt MCP tăng (34 tool), nhưng `execute_revit_code` vẫn là lối thoát chung; `search_tools` được ép gọi trước.
