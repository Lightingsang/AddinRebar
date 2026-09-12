# NotebookLM integration

Tích hợp [`teng-lin/notebooklm-py`](https://github.com/teng-lin/notebooklm-py) (MIT) — client không chính thức cho Google Gemini Notebook — vào repo trên ba mặt: agent skill, Python library, MCP server. Kèm một pipeline nạp tài liệu Revit của repo lên notebook rồi kéo học liệu về.

Plan gốc: [`plans/260904-1038-install-notebooklm-py-integration/`](../plans/260904-1038-install-notebooklm-py-integration/plan.md).

> **Upstream là unofficial.** Nó gọi API Google không có tài liệu công khai. Gãy là chuyện bình thường, không phải sự cố bất thường. Version đã pin bên dưới để biết mốc mà đối chiếu.

## Version đang dùng

| Thành phần | Version |
|---|---|
| CLI (`uv tool`) | `notebooklm-py==0.8.2`, build `c1008a44` |
| Library (`scripts/.venv`) | `notebooklm-py==0.8.2` — pin trong [`scripts/requirements-notebooklm.txt`](../scripts/requirements-notebooklm.txt) |
| MCP server | resolve qua `uvx --from "notebooklm-py[mcp]"` (env ephemeral riêng) |
| Skill | `SKILL.md` vendor từ nhánh `main`, khớp upstream sau khi normalize line-ending |

Xuất xứ skill nằm ở [`skills-lock.json`](../skills-lock.json) do `npx skills add` sinh — nguồn `teng-lin/notebooklm-py` kèm hash nội dung `c8710704…`. Dùng nó để phát hiện upstream đã đổi.

## Cài lại từ máy trắng

```bash
# 1. CLI. KHÔNG thêm extra `mcp` — xem mục Bẫy bên dưới.
uv tool install "notebooklm-py[browser,markdown]"

# 2. Đăng nhập. Mở browser, hạn 5 phút.
notebooklm login --browser chrome     # dùng Chrome thật, nhận nhanh hơn Chromium bundled
notebooklm auth check --test --json   # phải thấy token_fetch: true

# 3. Agent skill, project-scoped.
npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code
cd .claude/skills/notebooklm
for item in .[!.]* *; do case "$item" in SKILL.md|LICENSE) ;; *) rm -rf -- "$item" ;; esac; done
cd - && cp -r .claude/skills/notebooklm .agents/skills/

# 4. Python library.
uv venv scripts/.venv
uv pip install --python scripts/.venv notebooklm-py
```

MCP đã khai báo sẵn trong [`.mcp.json`](../.mcp.json), không cần cài gì thêm — `uvx` tự resolve lúc chạy.

## Bẫy — cả ba đều đã dính thật, không phải giả định

**`notebooklm skill install` ghi vào `~/.claude/skills/`.** `CLAUDE.md` cấm sửa thư mục đó. Dùng `npx skills add` như trên.

**`npx skills add` clone TOÀN BỘ repo upstream — 111 MB.** `tests/` 77M, `docs/` 24M, `src/` 7.3M, PNG 657K. Nguy hiểm hơn dung lượng: nó thả `CLAUDE.md`, `AGENTS.md`, `.gitignore`, `.gitattributes` **lồng bên trong** `.claude/skills/` — `CLAUDE.md` lồng nhau có thể bị đọc thành project instruction. Luôn chạy bước cắt. Sau khi cắt: 28K, đúng 2 file.

**`notebooklm mcp install claude-code` ghi user-global `~/.claude.json`.** Config của repo là `.mcp.json`.

**Binary `notebooklm-mcp` cài kèm CLI là shim gãy.** Không có extra `mcp` thì nó chết với `ModuleNotFoundError: No module named 'fastmcp'`. Đừng "sửa" bằng cách thêm extra vào tool install — `.mcp.json` đi qua `uvx`, vốn resolve env riêng, nên extra ở tool env không bao giờ được dùng tới.

**`generate_quiz` và `generate_flashcards` KHÔNG nhận `language`.** Mọi kind khác thì có. Truyền vào là `TypeError` giữa chừng, sau khi quota cho các artifact trước đã tiêu. `notebooklm_client.generate_and_wait` lọc kwarg theo chữ ký thật và log cảnh báo khi bỏ.

**`artifacts.list` liệt kê cả artifact CHƯA xong.** Có tên trong danh sách không đồng nghĩa tải được — `download_*` ném `ArtifactNotReadyError`. Pipeline bắt lỗi này theo từng kind rồi bỏ qua, vì một artifact chậm không được phép vứt bỏ những cái đã xong.

**Audio vượt timeout mặc định 300 s.** Audio overview của 7 tài liệu mất hơn 5 phút; `ARTIFACT_SPEC` để audio 1800 s. Timeout chỉ quyết định lần chạy này có chờ hay không — server vẫn sinh tiếp, lần chạy sau nhặt về.

**`generate_mind_map` trả `task_id=None`.** Poll trên `None` quét danh sách không thấy gì rồi bỏ sau ~28 s. `generate_and_wait` trả `None` thay vì giả vờ xong; pipeline hỏi lại `artifacts.list` và ghi `unverified` nếu không thấy.

**Source file không "refresh" tại chỗ được.** Server không có đường quay lại đường dẫn local, nên khi hash đổi thì phải thêm bản mới rồi xoá bản cũ — nếu không notebook tích luỹ bản lỗi thời và asset được dựng từ cả hai.

**`language` không đủ để ra tiếng Việt.** `report` nhận `language` và ra đúng tiếng Việt. `quiz`/`flashcards` từ chối tham số đó; `mind_map` nhận nhưng vẫn ra tiếng Anh. Manifest vì thế có thêm `instructions` (free-text) nói rõ ngôn ngữ — `generate_and_wait` tự bỏ tham số nào kind đó không nhận. Artifact đã sinh thì **không** được sinh lại tự động; muốn đổi ngôn ngữ phải xoá trên server trước.

**Console Windows mặc định cp1252.** In tiếng Việt là `UnicodeEncodeError`. Pipeline tự `sys.stdout.reconfigure(encoding="utf-8")`; chạy test thì đặt `PYTHONUTF8=1`.

## Credential

Nằm ở `~/.notebooklm/profiles/<profile>/`, **ngoài worktree** — giữ nguyên như vậy, đừng trỏ `NOTEBOOKLM_HOME` vào repo.

```
~/.notebooklm/
├── config.json
└── profiles/default/
    ├── storage_state.json     ← cookie Google sống
    ├── context.json
    └── browser_profile/
```

Hai lớp bảo vệ, khác nhau:

- **Chặn commit** — `.gitignore` có `.notebooklm/`, `storage_state.json`, `master_token.json`.
- **Chặn đọc** — [`.claude/hooks/lib/privacy-checker.cjs`](../.claude/hooks/lib/privacy-checker.cjs) khớp hai tên file đó. Trước đây hook chỉ khớp `.env*` và `/credentials/i`, nghĩa là agent `cat` cookie ra transcript được. `.gitignore` không chặn việc đọc — cần cả hai lớp.

Auth là interactive và **không tự hồi phục**. Cookie chết thì `notebooklm login` lại. Pipeline luôn `auth check --test` trước khi tiêu quota.

## Quota — đo, không tra bảng

Tài khoản `lightingsang@gmail.com`, đo bằng `client.settings.get_account_limits()`:

```
AccountLimits(notebook_limit=500, source_limit=300, raw_limits=(6, 500, 300, 500000, 2), tier=2)
```

→ **tier Pro**: 300 source/notebook, 500 notebook/tài khoản, 500 000 từ/source. 7 file nguồn của pipeline không chạm giới hạn nào.

Quota generate/ngày thì API không trả về. Pipeline vì thế đọc lỗi quota từ server rồi dừng, thay vì tự suy ra hạn mức từ bảng tra. Reset là **rolling window tính từ lần dùng đầu**, không phải nửa đêm — retry mù đốt quota mà không hồi.

## Pipeline học liệu

```bash
# Xem trước. Chỉ gọi read-only (list notebook/source/artifact) — không tốn quota.
PYTHONUTF8=1 scripts/.venv/Scripts/python.exe scripts/build-notebooklm-course-assets.py --dry-run

# Chạy thật.
PYTHONUTF8=1 scripts/.venv/Scripts/python.exe scripts/build-notebooklm-course-assets.py
```

| File | Vai trò |
|---|---|
| [`scripts/notebooklm-sources.json`](../scripts/notebooklm-sources.json) | Manifest: allow-list tường minh file nguồn + kind artifact |
| [`scripts/build-notebooklm-course-assets.py`](../scripts/build-notebooklm-course-assets.py) | Pipeline |
| [`scripts/notebooklm_client.py`](../scripts/notebooklm_client.py) | Wrapper mỏng; snake_case vì nó để `import` |
| `output/notebooklm/` | Artifact tải về + `state.json` (đã gitignore) |

**Idempotent.** Source chỉ upload lại khi hash SHA-256 đổi; artifact chỉ sinh khi notebook chưa có kind đó. Chạy lại lần hai báo `0 to upload, 7 unchanged` và không tiêu quota.

**Manifest là allow-list, không phải glob.** Cố ý: pipeline đẩy nội dung repo ra dịch vụ bên thứ ba, nên không file nào được lên đó do quét thư mục tự động. Có test bảo vệ điều này.

`task_id` luôn được ghi vào `state.json` **trước** khi chờ. Generate tiêu quota ngay lúc server nhận; mất id vì timeout hay Ctrl-C là mất luôn artifact đã trả tiền.

## Test

```bash
PYTHONUTF8=1 python -m unittest discover -s tests/notebooklm -t tests/notebooklm
```

Chạy bằng **system Python** là phép thử chính — nó không có `notebooklm`, nên test pass chứng minh code không kéo thư viện ở top level. Không test nào chạm mạng.

`build-notebooklm-course-assets.py` là kebab-case (đúng quy ước `.py` của repo) nên không `import` bằng tên module được; test nạp nó qua `importlib.util.spec_from_file_location`.

## Còn treo

- `AGENTS.md` lệch với `CLAUDE.md` sau khi thêm mục NotebookLM. Quy ước repo là re-sync chứ không sửa tay, nhưng `.skill-sync/config.json` không có trong checkout này — bootstrap là việc của plan `260823`.
- Chưa chốt: artifact (`.m4a`, `.png`) có commit vào git không, hay chỉ để `output/`.
- Lúc cài, Socket quét skill ra 7 alert (`https://skills.sh/teng-lin/notebooklm-py`). Sau khi cắt thì skill dir chỉ còn văn bản, không còn code thực thi.
