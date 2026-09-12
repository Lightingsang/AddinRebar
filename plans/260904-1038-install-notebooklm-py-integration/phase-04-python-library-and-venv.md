---
phase: 4
title: "Python library + venv scripts/"
status: pending
priority: P2
effort: "1h"
dependencies: [1]
---

# Phase 4: Python library + venv `scripts/`

## Overview
Dựng venv riêng cho `scripts/`, cài `notebooklm-py` làm library, và viết một module client mỏng bọc `NotebookLMClient` để Phase 5 dùng lại.

## Requirements
- Functional: `import notebooklm` chạy được trong venv; module client kết nối được bằng session của Phase 1.
- Non-functional: system Python 3.14.5 không bị đụng; venv không vào git.

## Architecture

`CLAUDE.md` ghi rõ repo chưa có venv nào và system Python là thứ đang chạy skill script. Phase này lập venv đầu tiên — đặt ở `scripts/.venv` (cạnh code dùng nó) thay vì repo root, vì chỉ `scripts/` cần Python.

API là **async**. Entry point là context manager đọc lại session mà Phase 1 đã login:

```python
async with NotebookLMClient.from_storage() as client:
    nb = await client.notebooks.create("Research")
    await client.sources.add_url(nb.id, "https://example.com", wait=True)
    result = await client.chat.ask(nb.id, "Summarize this")
```

`from_storage()` đọc `~/.notebooklm/profiles/<profile>/storage_state.json` — nên library và CLI dùng chung một danh tính, không phải auth riêng.

Module bọc (`scripts/notebooklm_client.py`) tồn tại để tách hai thứ Phase 5 không nên tự lo: mở/đóng client đúng cách, và log `task_id` trước khi chờ (D9).

## Related Code Files
- Create: `scripts/.venv/` (không vào git)
- Create: `scripts/requirements-notebooklm.txt`
- Create: `scripts/notebooklm_client.py`
- Modify: `.gitignore` — thêm `.venv/`

## Implementation Steps

1. Tạo venv và cài library:
   ```bash
   uv venv scripts/.venv
   uv pip install --python scripts/.venv notebooklm-py
   ```
2. Pin version thực tế đã resolve vào `scripts/requirements-notebooklm.txt`:
   ```bash
   uv pip freeze --python scripts/.venv | grep -i notebooklm > scripts/requirements-notebooklm.txt
   ```
3. Thêm `.venv/` vào `.gitignore` (repo hiện chưa có dòng này — đã verify).
4. Viết `scripts/notebooklm_client.py` — kebab-case là quy ước cho script `.py` trong repo, nhưng file này là **module để import** nên phải snake_case (`import` không nhận dấu gạch ngang). Nội dung:
   - `open_client()` — async context manager bọc `NotebookLMClient.from_storage()`
   - `generate_and_wait(client, nb_id, kind, **opts)` — gọi generate, **log `task_id` ngay** rồi mới `wait_for_completion`, để artifact đã tốn quota không bị mất dấu nếu timeout
   - `SUPPORTED_UPLOAD_SUFFIXES` — frozenset `.pdf .txt .md .markdown .doc .docx .pptx .rtf .odt .csv .tsv .epub`
5. Smoke test không tốn quota — chỉ đọc:
   ```bash
   scripts/.venv/Scripts/python -c "import asyncio, notebooklm; print(notebooklm.__version__)"
   ```
   rồi một script nhỏ gọi `client.notebooks.list()` và in số notebook.
6. Verify venv không lọt vào git: `git status --porcelain` không hiện `scripts/.venv`.

## Success Criteria
- [ ] `scripts/.venv` tồn tại, `import notebooklm` chạy
- [ ] `scripts/requirements-notebooklm.txt` pin đúng version đã cài
- [ ] `notebooklm_client.py` list được notebook thật bằng session Phase 1
- [ ] `git status` không thấy `scripts/.venv`
- [ ] `scripts/notebooklm_client.py` < 200 dòng (ngưỡng modular hóa của `CLAUDE.md`)

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| Path venv khác nhau Windows/POSIX (`Scripts/` vs `bin/`) | Script gọi qua `uv run --python scripts/.venv` để không hardcode; chỉ hardcode trong lệnh smoke test thủ công |
| Async API dùng sai trong sync context | Module bọc chỉ expose async; caller Phase 5 chạy qua `asyncio.run()` một lần ở entry point |
| `from_storage()` không thấy session nếu profile bị đổi | Không set `NOTEBOOKLM_PROFILE` ở bất kỳ đâu; nếu lỗi thì `notebooklm status --paths` chỉ ra file đang được đọc |
| Cài library rồi lệch version với CLI của Phase 0 | Chấp nhận — hai env tách nhau có chủ đích. Phase 6 ghi cả hai version vào docs |
