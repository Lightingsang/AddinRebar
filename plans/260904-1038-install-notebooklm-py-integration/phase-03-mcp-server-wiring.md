---
phase: 3
title: "Đấu nối MCP server"
status: pending
priority: P2
effort: "0.5h"
dependencies: [1]
---

# Phase 3: Đấu nối MCP server

## Overview
Khai báo `notebooklm-mcp` trong `.mcp.json` ở project root để 38 tool NotebookLM xuất hiện native trong session, thay vì agent phải shell ra CLI.

## Requirements
- Functional: server `notebooklm` connect được, `ListMcpResources`/tool list thấy đủ tool.
- Non-functional: config nằm trong git, scope đúng repo — không ghi `~/.claude.json`.

## Architecture

Repo chưa có `.mcp.json` (đã verify), dù `docs/mcp-architecture.md` có bàn về MCP. Phase này tạo file đó lần đầu.

Upstream có `notebooklm mcp install claude-code` nhưng nó ghi vào **`~/.claude.json`** (user-global) — cùng vấn đề với skill installer ở Phase 2. Nên viết tay block config vào project file (D4).

Transport chọn `stdio`: đây là desktop client, một process, không cần chia sẻ server giữa nhiều client. `--transport http` chỉ đáng dùng khi nhiều client cùng máy phải dùng chung — không phải trường hợp này, và nó kéo theo phải quản `NOTEBOOKLM_MCP_TOKEN`.

Dùng `uvx --from` thay vì gọi thẳng `notebooklm-mcp`: config không phụ thuộc PATH, chạy được cả khi `uv tool` env bị dọn.

Server **bind profile đang active lúc khởi động** — đổi profile thì phải restart server.

## Related Code Files
- Create: `.mcp.json`

## Implementation Steps

1. Verify server chạy độc lập trước khi đấu vào config:
   ```bash
   uvx --from "notebooklm-py[mcp]" notebooklm-mcp --help
   ```
2. Tạo `.mcp.json` ở project root:
   ```json
   {
     "mcpServers": {
       "notebooklm": {
         "command": "uvx",
         "args": ["--from", "notebooklm-py[mcp]", "notebooklm-mcp"]
       }
     }
   }
   ```
3. Xác nhận **không** có file user-global nào bị ghi:
   ```bash
   git status --porcelain          # chỉ .mcp.json mới
   ls -la ~/.claude.json 2>/dev/null && echo "kiểm tra mtime — không được đổi ở phase này"
   ```
4. Restart session, approve server khi được hỏi, rồi verify tool list thấy các nhóm: notebooks, sources, chat, notes, studio, research, sharing, server.
5. Smoke test một tool **read-only** (không tốn quota): tool `server info` hoặc `notebooks list`.

## Success Criteria
- [ ] `.mcp.json` tồn tại ở project root, JSON hợp lệ
- [ ] Server `notebooklm` connect thành công sau restart
- [ ] Tool list hiện đủ 38 tool
- [ ] Một tool read-only trả về kết quả thật
- [ ] `~/.claude.json` không bị sửa
- [ ] Server `notebooklm` **không** được thêm vào allowlist permission — mọi tool ghi/xoá vẫn phải hỏi

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| `uvx` resolve chậm lần đầu (tải package) → server timeout khi connect | Bước 1 warm cache trước; lần chạy sau `uvx` dùng cache |
| MCP server và CLI dùng khác profile | Cả hai đọc `default_profile` từ `~/.notebooklm/config.json`; không set `NOTEBOOKLM_PROFILE` ở đâu cả để tránh lệch |
| 38 tool làm loãng tool namespace của session | Chấp nhận — user chọn MCP surface. Có thể tắt bằng cách xóa entry trong `.mcp.json` |
| **Tool MCP không đi qua hook nào của repo** | `.claude/settings.json:36` chỉ khớp `Bash\|Glob\|Grep\|Read\|Edit\|Write` — `scout-block` và `privacy-block` không nhìn thấy lời gọi MCP. Nghĩa là 38 tool này (gồm delete notebook/source, đổi sharing) chạy ngoài mọi guard tự động của repo |
| Tool MCP gọi thao tác phá hủy (delete notebook/source) | Phòng thủ chỉ còn 2 lớp, cả hai đều không phải hook: (1) permission prompt của host — **giữ bật, không allowlist server này**; (2) "authorization boundaries" trong skill Phase 2, vốn là văn bản hướng dẫn chứ không phải cưỡng chế. Chấp nhận có ý thức, không phải bỏ sót |
