---
phase: 0
title: "Preflight + cài CLI"
status: pending
priority: P1
effort: "0.5h"
dependencies: []
---

# Phase 0: Preflight + cài CLI

## Overview
Cài `notebooklm-py` như một CLI isolated bằng `uv`, pin version, verify `notebooklm` và `notebooklm-mcp` lên được PATH. Chưa đăng nhập ở phase này.

## Requirements
- Functional: `notebooklm --version` và `notebooklm-mcp --help` chạy được từ shell bất kỳ.
- Non-functional: không đụng system Python 3.14.5; version cài được ghi lại để tái lập.

## Architecture

Ba surface user chọn cần hai kênh cài khác nhau:

```
uv tool install "notebooklm-py[browser,markdown]"
        └── notebooklm        → Phase 1 (auth), Phase 2 (agent skill gọi CLI)

uv venv scripts/.venv + uv pip install notebooklm-py   → Phase 4 (library)
        └── import notebooklm → Phase 4, Phase 5

uvx --from "notebooklm-py[mcp]" notebooklm-mcp          → Phase 3 (MCP server)
        └── env ephemeral riêng, KHÔNG dùng tool env ở trên
```

Ba env tách rời, có chủ đích. Tool-install cấp binary trên PATH; venv riêng để `import` trong `scripts/`; `uvx` tự resolve env riêng cho MCP.

**Extra `mcp` cố tình KHÔNG cài ở đây.** `.mcp.json` của Phase 3 gọi `uvx --from "notebooklm-py[mcp]"`, thứ này resolve một env ephemeral riêng — binary `notebooklm-mcp` cài ở tool env sẽ không bao giờ được gọi tới. Cài nó chỉ tạo ảo giác là MCP phụ thuộc Phase 0.

Extras chọn: `browser` (auth interactive — user đã chốt), `markdown` (export artifact dạng `.md` cho Phase 5). Bỏ `cookies` (không dùng cookie import), `headless` (không dùng master token), `mcp` (lý do ngay trên).

## Related Code Files
- Create: không có file repo nào ở phase này (cài ra ngoài worktree)
- Modify: không có

## Implementation Steps

1. Xác nhận tiền đề — `uv --version`, `python --version` (cần 3.10–3.14; local 3.14.5 ✅), `node --version` cho `npx` ở Phase 2.
2. Cài CLI:
   ```bash
   uv tool install "notebooklm-py[browser,markdown]"
   ```
3. Nếu shell chưa thấy binary, chạy `uv tool update-shell` rồi mở terminal mới. Trên Windows/Git Bash đường dẫn là `~/.local/bin`.
4. Ghi lại version đã cài — dùng cho mục Pin ở Phase 6:
   ```bash
   notebooklm --version
   uv tool list
   ```
5. Verify entry point:
   ```bash
   notebooklm --help
   ```
   Không verify `notebooklm-mcp` ở đây — Phase 3 dùng env `uvx` riêng và tự warm cache ở bước 1 của nó.
6. Xem đường dẫn config sẽ dùng (chưa có file vì chưa login) — xác nhận nó nằm **ngoài** worktree:
   ```bash
   notebooklm status --paths
   ```

## Success Criteria
- [ ] `notebooklm --version` in ra version, ghi lại số cụ thể
- [ ] `notebooklm --help` chạy, không lỗi import
- [ ] `notebooklm status --paths` trỏ vào `~/.notebooklm/...`, không trỏ vào đường dẫn repo
- [ ] `git status` sạch — phase này không tạo file nào trong worktree

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| `uv tool install` không đưa binary lên PATH trên Windows | `uv tool update-shell`, mở terminal mới; fallback gọi bằng `uvx --from "notebooklm-py[browser,mcp]" notebooklm` |
| Extra `browser` kéo Playwright nặng | Chấp nhận — user chọn interactive login. Chromium chưa tải ở phase này, chỉ tải ở lần `login` đầu (Phase 1) |
| Sau này ai đó thêm lại extra `mcp` vì tưởng Phase 3 cần | Đã ghi lý do trong mục Architecture; `.mcp.json` là nơi duy nhất quyết định env của MCP |
| Upstream là unofficial, version drift | Ghi version ngay bước 4; Phase 6 pin lại trong docs |
