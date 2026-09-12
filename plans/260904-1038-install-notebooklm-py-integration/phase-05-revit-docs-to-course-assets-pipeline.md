---
phase: 5
title: "Pipeline Revit docs → course assets"
status: pending
priority: P2
effort: "4h"
dependencies: [4]
---

# Phase 5: Pipeline Revit docs → course assets

## Overview
Phần EXPANSION: một pipeline chạy được, nạp tài liệu Revit sẵn có của repo lên NotebookLM rồi sinh học liệu (audio overview, quiz, flashcards, mind map, briefing doc) cho `course-website/`. Idempotent và tiết kiệm quota.

## Requirements
- Functional: một lệnh nạp source → sinh artifact → tải về `output/notebooklm/`; chạy lại không sinh trùng.
- Non-functional: không vượt quota free tier trong một lần chạy; mọi `task_id` được ghi log; dừng giữa chừng thì chạy tiếp được.

## Architecture

### Nguồn có sẵn trong repo

| File | Dòng | Vai trò |
|---|---|---|
| `RevitTemplates-Huong-Dan-Tieng-Viet.md` | 519 | Hướng dẫn template Nice3point (tiếng Việt) |
| `RevitTemplates-Infographic-Prompt.md` | 268 | Prompt sinh infographic |
| `docs/revit-etabs-structural-automation-workflow.md` | 529 | Workflow tự động hóa kết cấu |
| `docs/system-architecture.md` | 353 | Kiến trúc add-in |
| `docs/duplicate-sheets-design.md` | 483 | Thiết kế tính năng |
| `docs/code-standards.md` | 153 | Chuẩn code |
| `docs/codebase-summary.md` | 135 | Tóm tắt codebase |

~2 440 dòng, 7 file — dư sức cho 1 notebook (đo được: 300 source/notebook, 500 000 từ/source). `.md` upload thẳng được, không cần convert PDF (D7).

### Luồng

```
scripts/build-notebooklm-course-assets.py
  │
  ├─ 1. đọc manifest → danh sách file nguồn
  ├─ 2. hash SHA-256 từng file → so với state.json
  │      └─ không đổi → bỏ qua upload
  ├─ 3. source add (--json, --wait) → lưu source_id
  ├─ 4. THU HOẠCH artifact first-add (KHÔNG tính quota)
  ├─ 5. generate phần còn lại, theo ngân sách quota
  │      └─ log task_id NGAY, rồi mới wait
  ├─ 6. download → output/notebooklm/
  └─ 7. ghi state.json (source_id, task_id, hash, timestamp)
```

### Ngân sách quota — đo thực tế, không đoán

Tài khoản `lightingsang@gmail.com` là **tier Pro**, không phải free. Đo bằng `client.settings.get_account_limits()`:

```
AccountLimits(notebook_limit=500, source_limit=300, raw_limits=(6, 500, 300, 500000, 2), tier=2)
```

| Hạn mức | Giá trị đo được |
|---|---|
| Source / notebook | **300** |
| Notebook / tài khoản | **500** (đang dùng 108) |
| Từ / source | **500 000** |
| `tier` | 2 |

7 file nguồn ≈ 2 440 dòng — không chạm giới hạn nào.

**Quota generate/ngày thì API này không trả về.** Bảng doc cho Pro là 20 audio/ngày, 500 chat/ngày. Không lấy con số đó làm cam kết: pipeline vẫn đọc lỗi quota từ server rồi dừng sạch, thay vì tin vào bảng tra. Reset là **rolling window tính từ lần dùng đầu**, không phải nửa đêm — retry mù đốt quota mà không hồi. State file vì thế vẫn bắt buộc, không phải tối ưu.

Khai thác được ghi trong doc: *"First-add auto-generated artifacts ... are generated once and do not count against limits."* Bước 4 thu hoạch trước khi gọi generate ở bước 5 — tiết kiệm được bao nhiêu thì bớt bấy nhiêu.

## Related Code Files
- Create: `scripts/build-notebooklm-course-assets.py`
- Create: `scripts/notebooklm-sources.json` (manifest nguồn + tuỳ chọn artifact)
- Create: `output/notebooklm/` (đã bị gitignore qua `output/`)
- Modify: `scripts/notebooklm_client.py` nếu cần thêm helper

## Implementation Steps

1. **Gate auth trước khi tiêu bất cứ gì.** Bước đầu tiên của script, trước mọi upload/generate:
   ```bash
   notebooklm auth check --test --json
   ```
   Fail thì thoát ngay với exit code khác 0. Auth interactive không tự hồi phục (Phase 1); cookie chết giữa chừng nghĩa là quota đã tiêu mà artifact không về.
2. Viết manifest `scripts/notebooklm-sources.json`: danh sách đường dẫn nguồn, tiêu đề notebook, và artifact muốn sinh kèm ngân sách mỗi loại.
3. Viết script chính. Tách theo mối quan tâm, mỗi phần một hàm; nếu vượt 200 dòng thì tách module theo `CLAUDE.md`:
   - `load_manifest()` / `hash_sources()` — phát hiện thay đổi
   - `sync_sources()` — upload cái nào đổi, dùng `source add` với `--title` là tên file cho dễ truy citation
   - `harvest_first_add()` — liệt kê artifact đã tự sinh, tải về, đánh dấu trong state
   - `generate_remaining()` — chỉ sinh cái còn thiếu, log `task_id` trước `wait`
   - `download_all()` → `output/notebooklm/`
   - `save_state()` — atomic write (ghi tmp rồi rename), tránh state hỏng khi Ctrl-C
4. State file `output/notebooklm/state.json`: `{notebook_id, sources: {path: {hash, source_id}}, artifacts: {kind: {task_id, status, downloaded_path}}}`.
5. Thêm `--dry-run` in ra đúng những lệnh sẽ chạy và quota sẽ tiêu. **`--dry-run` được phép gọi API read-only** (`notebooks list`, `sources list`, `studio list`) — bắt buộc phải vậy: muốn biết artifact nào đã tồn tại từ first-add thì phải hỏi server, và lần chạy đầu chưa có `notebook_id` trong state để suy ra. Ranh giới thật là:

   | `--dry-run` được làm | `--dry-run` KHÔNG được làm |
   |---|---|
   | list notebook / source / artifact | `source add` |
   | đọc `state.json` | mọi lệnh `generate` |
   | in kế hoạch + ước tính quota | `chat ask` (tốn quota chat) |

   Lệnh list không tính vào bất kỳ hạn mức nào trong bảng quota — chỉ `chat` và `generate` mới tính. Vì thế dry-run vẫn an toàn về quota, chỉ không phải "offline".
6. Chạy thật lần đầu:
   ```bash
   uv run --python scripts/.venv scripts/build-notebooklm-course-assets.py --dry-run
   uv run --python scripts/.venv scripts/build-notebooklm-course-assets.py
   ```
7. Verify tính idempotent — chạy lại ngay, phải báo skip toàn bộ, không gọi generate:
   ```bash
   uv run --python scripts/.venv scripts/build-notebooklm-course-assets.py
   ```
8. Kiểm tra artifact tải về mở được: `.m4a` phát được, `.png` xem được, quiz/flashcards `.md` đọc được.

## Success Criteria
- [ ] Script thoát ngay khi `auth check --test` fail, trước khi tiêu quota
- [ ] `--dry-run` in ra kế hoạch + ước tính quota; chỉ gọi lệnh list, không `generate`/`source add`/`chat`
- [ ] Lần chạy đầu tạo notebook, upload đủ source trong manifest
- [ ] Artifact first-add được thu hoạch trước khi generate
- [ ] Lần chạy thứ hai skip 100%, không tiêu quota
- [ ] `output/notebooklm/` có artifact mở được + `state.json` hợp lệ
- [ ] Mọi `task_id` xuất hiện trong log trước khi wait
- [ ] Ctrl-C giữa chừng rồi chạy lại → tiếp tục, không sinh trùng

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| **Hết quota giữa chừng** | Tier Pro nên khó chạm, nhưng không loại trừ. State file + skip logic; script bắt lỗi quota từ server, dừng sạch, báo còn thiếu gì — không retry mù, không tự suy ra hạn mức từ bảng tra |
| Rolling window làm "đợi tới mai" không đúng | Script chỉ báo hết quota, không tự tính thời điểm reset — để người quyết định |
| **`generate_*` không cùng chữ ký** — `quiz`/`flashcards` không nhận `language`, kind khác thì có | Đã dính thật: lần chạy đầu chết `TypeError` sau khi 7 source đã upload và `report` đã sinh (quota đã tiêu). `generate_and_wait` lọc kwarg theo `inspect.signature` và **log cảnh báo** khi bỏ — nuốt im nghĩa là ra asset tiếng Anh cho nguồn tiếng Việt mà không ai biết |
| **`generate_mind_map` trả `task_id=None`** | Đã dính thật. `wait_for_completion(nb, None)` quét danh sách tìm không thấy gì rồi bỏ sau ~28 s. Wrapper trả `None` thay vì giả vờ xong; pipeline hỏi lại `artifacts.list` và ghi `status: unverified` nếu không thấy — ghi `ready` cho thứ không tồn tại sẽ khiến mọi lần chạy sau skip vĩnh viễn |
| **Mất `notebook_id` khi crash giữa chừng** | Đã dính thật ở lần chạy đầu: id chỉ lưu ở cuối `run()`. Giờ lưu ngay sau `resolve_notebook`. Fallback khớp theo tiêu đề vẫn còn nhưng chỉ cứu được tới khi ai đó đổi tên notebook |
| Cookie hết hạn giữa lần chạy dài | Gate `auth check --test` ở bước 1; nếu chết giữa chừng thì `task_id` đã lưu trong state, login lại rồi chạy tiếp sẽ poll task cũ |
| Generate timeout (tới 3600 s) mà artifact vẫn đang chạy server-side | `task_id` đã log ở bước trước; chạy lại sẽ poll đúng task cũ thay vì sinh mới |
| Tài liệu tiếng Việt bị sinh artifact tiếng Anh | Dùng `--language vi` cho generate; verify trên artifact đầu tiên |
| Artifact nặng (`.m4a`, `.mp4`) làm phình repo | Đổ vào `output/` — đã gitignore. Câu hỏi mở #2 trong `plan.md` cần chốt trước khi ai đó commit tay |
| Nội dung repo bị đẩy lên dịch vụ Google bên thứ ba | Nguồn chọn là tài liệu kỹ thuật/giáo trình đã có, không phải code sản phẩm hay thông tin khách hàng. Manifest là danh sách **allow-list tường minh** — không quét thư mục tự động, để không có file nào bị upload ngoài ý muốn |
