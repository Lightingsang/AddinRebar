---
phase: 1
title: "Auth interactive + vệ sinh secret"
status: pending
priority: P1
effort: "0.5h"
dependencies: [0]
---

# Phase 1: Auth interactive + vệ sinh secret

## Overview
Đăng nhập Google bằng Playwright browser login, verify session sống, và dựng hàng rào để cookie/token không bao giờ lọt vào git.

## Requirements
- Functional: `notebooklm auth check --test --json` trả về trạng thái hợp lệ; `notebooklm list` gọi được API thật.
- Non-functional: không file secret nào nằm trong worktree; `.gitignore` có lưới an toàn cả khi ai đó cố tình đổi `NOTEBOOKLM_HOME`.

## Architecture

Mô hình bảo mật ở đây là **cấu trúc, không phải quy ước**: credential mặc định nằm ở `~/.notebooklm/`, hoàn toàn ngoài worktree, nên `git add -A` không thể chạm tới. `.gitignore` chỉ là lưới thứ hai.

```
~/.notebooklm/                       ← NGOÀI repo. Không đổi.
├── config.json
└── profiles/default/
    ├── storage_state.json           ← cookie Google sống
    ├── context.json                 ← notebook đang active
    └── browser_profile/             ← Chromium user-data dir
```

Doc upstream nói thẳng: *"Treat both as secrets: keep them out of source control, logs, bug reports, shared artifact stores, and untrusted process environments."* Perm là `0700`/`0600`; trên Windows dựa vào ACL kế thừa thay cho mode bit POSIX.

**Không** set `NOTEBOOKLM_HOME`. Đây là quyết định D5 — mọi lợi ích của việc gom config vào repo đều không bù nổi rủi ro commit nhầm cookie.

## Related Code Files
- Modify: `.gitignore` — thêm khối lưới an toàn
- Modify: `.claude/hooks/lib/privacy-checker.cjs` — thêm tên file credential vào pattern nhạy cảm

## Implementation Steps

1. Login (lần đầu sẽ tải Chromium ~170 MB, 30–90 s, **không có progress bar** — đừng tưởng treo):
   ```bash
   notebooklm login
   ```
2. Verify session gọi được API thật, không chỉ đọc file:
   ```bash
   notebooklm auth check --test --json
   notebooklm list --json
   ```
3. Xác nhận đường dẫn thực tế nằm ngoài repo:
   ```bash
   notebooklm status --paths
   ```
4. Thêm lưới an toàn vào `.gitignore` (đặt dưới khối `# Caches & System-locked agent files`):
   ```gitignore
   # NotebookLM credentials — never commit
   .notebooklm/
   storage_state.json
   master_token.json
   ```
5. Chứng minh lưới hoạt động, không chỉ tin là nó hoạt động:
   ```bash
   git check-ignore -v storage_state.json master_token.json .notebooklm/
   ```
6. Bịt lỗ hổng **đọc** (khác với lỗ hổng commit ở bước 4). `.gitignore` chặn commit, nhưng không chặn agent `cat` file cookie ra transcript. Hook privacy-block của repo hiện chỉ khớp `.env*` và `/credentials/i` (`.claude/hooks/lib/privacy-checker.cjs:29-33`) — `storage_state.json` và `master_token.json` không khớp pattern nào. Thêm vào danh sách pattern nhạy cảm:
   ```js
   /^storage_state\.json$/,   // NotebookLM: cookie Google sống
   /^master_token\.json$/,    // NotebookLM: credential dài hạn
   ```
   Rồi verify hook thật sự chặn:
   ```bash
   node -e "const p=require('./.claude/hooks/lib/privacy-checker.cjs'); console.log(p.isPrivacySensitive('storage_state.json'), p.isPrivacySensitive('master_token.json'))"
   ```
   Kỳ vọng `true true`. `isPrivacySensitive` là tên export đã xác nhận ở `.claude/hooks/privacy-block.cjs:28`; nếu chữ ký khác, đọc file lấy tên đúng.
7. Kiểm tra tier tài khoản để chốt ngân sách quota cho Phase 5 — trả lời câu hỏi mở #1 trong `plan.md`:
   ```bash
   notebooklm metadata --json
   ```

## Success Criteria
- [ ] `notebooklm auth check --test --json` báo authenticated
- [ ] `notebooklm list --json` trả về JSON hợp lệ (kể cả list rỗng)
- [ ] `git check-ignore -v` khớp cả 3 pattern
- [ ] `git status --porcelain` chỉ hiện đúng `.gitignore` bị sửa
- [ ] `isPrivacySensitive('storage_state.json')` và `('master_token.json')` trả về `true`
- [ ] Tier tài khoản đã xác định và ghi vào `plan.md`

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| Login treo vì Chromium tải im lặng | Chờ đủ 90 s trước khi kết luận hỏng; kiểm tra network |
| Google chặn login do bot detection | `browser_profile/` là persistent Chromium dir sinh ra để tránh việc này. Nếu vẫn chặn: xóa `~/.notebooklm/profiles/default/browser_profile/` rồi login lại |
| Cookie hết hạn giữa chừng Phase 5 | `notebooklm auth refresh`; nếu hỏng hẳn thì `notebooklm login` lại. Interactive auth không tự hồi phục như master token — đây là đánh đổi đã biết của lựa chọn auth |
| Ai đó về sau set `NOTEBOOKLM_HOME` vào repo | `.gitignore` bước 4 bắt được `.notebooklm/` và tên file secret ở mọi cấp thư mục |
| Agent đọc cookie ra transcript/report rồi transcript bị chia sẻ | Bước 6 thêm pattern vào privacy-block. `.gitignore` chỉ chặn commit — không chặn đọc. Hai lớp khác nhau, cần cả hai |
