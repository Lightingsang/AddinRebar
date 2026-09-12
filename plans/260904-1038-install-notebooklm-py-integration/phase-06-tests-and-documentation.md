---
phase: 6
title: "Test + cập nhật docs"
status: pending
priority: P2
effort: "1.5h"
dependencies: [2, 3, 5]
---

# Phase 6: Test + cập nhật docs

## Overview
Test phần logic thuần của pipeline (không gọi mạng), verify end-to-end cả 3 surface, và ghi lại tích hợp vào `docs/` + changelog.

## Requirements
- Functional: test chạy được offline, không cần auth, không tốn quota.
- Non-functional: version upstream được pin và ghi lại; tài liệu đủ để người khác cài lại từ đầu.

## Architecture

Phân tầng test theo đúng cái test được:

| Tầng | Test bằng gì | Cần mạng? |
|---|---|---|
| Hash / phát hiện thay đổi / state I/O | `unittest` stdlib | không |
| Tính ngân sách quota | `unittest` stdlib | không |
| Lọc extension upload hợp lệ | `unittest` stdlib | không |
| `--dry-run` sinh đúng kế hoạch | `unittest` gọi subprocess | không |
| Gọi API thật | thủ công, một lần | có |

Dùng `unittest` stdlib vì repo đã dùng nó cho `tests/skill-sync/` — không thêm dependency test mới cho một pipeline phụ trợ (DRY với quy ước sẵn có).

Không mock `NotebookLMClient`. Logic đáng test đã tách khỏi tầng mạng ở Phase 4/5; phần chạm mạng thì test bằng tay một lần rồi thôi.

### Tên file kebab-case chặn `import` — dùng `importlib` theo đường dẫn

`build-notebooklm-course-assets.py` có dấu gạch ngang (đúng quy ước `.py` của repo, giống `sync-agent-skills.py`) nên **không import bằng tên module được**. Test nạp nó qua `importlib.util.spec_from_file_location`. Ngược lại `notebooklm_client.py` là snake_case đúng vì nó sinh ra để `import`.

### Ràng buộc interpreter — quyết định cách viết code, không chỉ cách chạy test

`notebooklm` chỉ tồn tại trong `scripts/.venv` (Phase 4). System Python 3.14.5 **không** có nó. Hai hệ quả bắt buộc:

1. Test phải chạy bằng interpreter của venv: `uv run --python scripts/.venv ...`, không bao giờ là `python` trần.
2. Quan trọng hơn: `scripts/build-notebooklm-course-assets.py` **không được `import notebooklm` ở top level** nếu test muốn import nó. Hàm thuần (hash, state I/O, ngân sách quota, lọc extension) phải nằm ở module không kéo theo tầng mạng — hoặc import `notebooklm` lười bên trong hàm cần nó.

Nếu bỏ qua điểm 2, `unittest discover` chết ngay ở khâu collect, trước cả khi chạy assert nào.

## Related Code Files
- Create: `tests/notebooklm/test_pipeline_state.py` — hash, state I/O, artifact spec
- Create: `tests/notebooklm/test_source_selection.py` — lọc extension, validate manifest
- Modify: `.gitattributes` — commit file đang untracked; mở rộng phạm vi LF nếu thêm fixture so byte
- Modify: `docs/project-changelog.md`
- Modify: `docs/codebase-summary.md`
- Create: `docs/notebooklm-integration.md`

## Implementation Steps

1. Viết test cho phần thuần logic:
   - hash ổn định; file đổi → phát hiện đổi; file không đổi → skip
   - state I/O round-trip; state hỏng/thiếu field → xử lý nhẹ nhàng, không crash
   - ngân sách quota: vượt ngưỡng thì dừng, không sinh tiếp
   - lọc extension: `.md` nhận, `.xlsx` từ chối
2. Test `--dry-run` bằng subprocess, khẳng định không có call mạng nào (không cần auth để chạy).
3. Chạy test. `-t` phải trỏ vào chính thư mục test (giống `tests/skill-sync/`) — `-t .` cho `ImportError: Start directory is not importable`:
   ```bash
   PYTHONUTF8=1 python -m unittest discover -s tests/notebooklm -t tests/notebooklm
   ```
   Chạy bằng **system Python** là phép thử chính: nó không có `notebooklm`, nên test pass nghĩa là điểm 2 ở mục Architecture đúng. Chạy lại bằng venv để chắc không có phụ thuộc ngược:
   ```bash
   PYTHONUTF8=1 scripts/.venv/Scripts/python.exe -m unittest discover -s tests/notebooklm -t tests/notebooklm
   ```
   Lưu ý `PYTHONUTF8=1`: console Windows mặc định cp1252, in ký tự tiếng Việt là `UnicodeEncodeError`. Pipeline tự `sys.stdout.reconfigure`, test thì không.
4. Verify end-to-end cả 3 surface, mỗi cái một lần:
   - **Skill**: invoke skill trong session mới, cho nó chạy một lệnh read-only
   - **MCP**: gọi một tool read-only qua MCP
   - **Library**: chạy lại pipeline, xác nhận skip 100%
5. Viết `docs/notebooklm-integration.md`: cài lại từ đầu, version đã pin (CLI + library), commit hash `SKILL.md` upstream, layout credential, ngân sách quota, cách chạy pipeline, và **ràng buộc không được chạy `notebooklm skill install` / `notebooklm mcp install`** vì chúng ghi user-global.
6. Cập nhật `docs/project-changelog.md` và `docs/codebase-summary.md` (thêm `scripts/` Python surface + `.mcp.json`).
7. Chốt lại hai câu hỏi mở trong `plan.md` — tier tài khoản và chính sách commit artifact — ghi câu trả lời vào `docs/notebooklm-integration.md`.

## Success Criteria
- [ ] Test pass qua `uv run --python scripts/.venv`
- [ ] Test pass khi **chưa** login (không phụ thuộc auth)
- [ ] Toàn bộ test pass bằng **system Python** — đây chính là bằng chứng không kéo `notebooklm` ở top level
- [ ] `--dry-run` không phát sinh call mạng nào
- [ ] Cả 3 surface verify tay xong: skill ✅ MCP ✅ library ✅
- [ ] `docs/notebooklm-integration.md` đủ để cài lại từ máy trắng
- [ ] Version CLI + library + commit hash `SKILL.md` upstream đã ghi lại
- [ ] Changelog + codebase-summary cập nhật
- [ ] `.gitattributes` đã được track (không còn `??`)
- [ ] `git status` không thấy file secret nào

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| Test CRLF-fail như `tests/skill-sync/` trên checkout Windows | `.gitattributes:8` **chỉ** phủ `tests/skill-sync/fixtures/**` — không phủ `tests/notebooklm/`. Và file đó hiện vẫn **untracked** (`git status` → `?? .gitattributes`), nên chưa bảo vệ ai trên bản clone mới. Hai việc: (1) so sánh nội dung đã normalize thay vì byte thô — rẻ và đủ; (2) commit `.gitattributes`, thêm dòng phủ fixture mới nếu về sau có test so byte |
| Test lỡ gọi mạng thật, đốt quota | Test chỉ chạm hàm thuần + `--dry-run`; không có test nào import đường dẫn tạo client |
| Docs hỏng ngay khi upstream đổi | Ghi version pin + commit hash, để lần drift sau biết đối chiếu từ mốc nào |
| `AGENTS.md` lệch khỏi `CLAUDE.md` | `AGENTS.md` là mirror sinh ra — sửa `CLAUDE.md` rồi re-sync, không sửa tay |
