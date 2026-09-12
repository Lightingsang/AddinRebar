---
title: "Cài đặt notebooklm-py vào repo (agent skill + Python lib + MCP)"
status: pending
created: 2026-09-04
scope: project
mode: hard
scope_decision: EXPANSION
blockedBy: []
blocks: [260823-agent-skill-portability-sync]
upstream: "https://github.com/teng-lin/notebooklm-py (MIT, unofficial)"
---

# Cài đặt notebooklm-py

Tích hợp `notebooklm-py` (client không chính thức cho Google Gemini Notebook / NotebookLM) vào repo qua **3 mặt**: agent skill project-scoped, Python library trong `scripts/`, và MCP server. Kèm 1 pipeline thật: nạp tài liệu Revit của repo → notebook → sinh asset học liệu cho `course-website/`.

**Scope user chốt:** 3 surface (skill + lib + MCP) · auth = interactive browser login · EXPANSION.
**Ngoài scope:** master-token / CI auth · Android backend · vendor source upstream vào repo · mọi thay đổi `.cs`/`.xaml` (Revit build gate không áp dụng).

Research đầy đủ: [reports/research-notebooklm-py.md](reports/research-notebooklm-py.md).

## Quyết định kiến trúc

| # | Quyết định | Lý do |
|---|---|---|
| D1 | CLI cài bằng `uv tool install "notebooklm-py[browser,markdown]"` — không `pip install --user`, và **không** kèm extra `mcp` | `uv` có sẵn (`~/.local/bin/uv`), `pipx` thiếu. Isolate khỏi system Python 3.14.5. Bỏ `mcp` vì `.mcp.json` gọi qua `uvx --from` — env ephemeral riêng, binary cài ở tool env không bao giờ được dùng tới |
| D2 | Skill cài **project-scoped**, tuyệt đối không chạy `notebooklm skill install` | Installer upstream ghi vào `~/.claude/skills/notebooklm/`; `CLAUDE.md` cấm sửa `~/.claude/skills` |
| D3 | Mirror skill sang `.agents/skills/notebooklm/` bằng tay, không dùng skill-sync engine | `.skill-sync/config.json` bị gitignore và không tồn tại trong checkout này; bootstrap nó là việc của plan `260823` |
| D4 | MCP khai báo trong `.mcp.json` ở project root, không dùng `notebooklm mcp install claude-code` | Lệnh đó ghi user-global `~/.claude.json`. File project vào được git, scope đúng repo |
| D5 | Credential giữ nguyên mặc định `~/.notebooklm/`; **không** trỏ `NOTEBOOKLM_HOME` vào worktree | `storage_state.json` = cookie Google sống. Để ngoài worktree thì commit nhầm là bất khả thi về mặt cấu trúc, không phụ thuộc `.gitignore` |
| D6 | Python lib dùng venv riêng `scripts/.venv` do `uv` quản, không đụng system Python | `CLAUDE.md` ghi nhận repo chưa có venv nào; giữ system Python sạch |
| D7 | Pipeline nạp thẳng file `.md`, không convert PDF | `.md`/`.markdown` nằm trong danh sách extension upload được hỗ trợ |
| D8 | Pipeline idempotent + cache artifact, key = SHA-256 nội dung file nguồn | Tài khoản là **tier Pro** (đo được: source 300/notebook, notebook 500, 500k từ/source). Rộng hơn free nhiều, nhưng quota generate/ngày vẫn hữu hạn và reset theo rolling window — chạy lại vẫn không được đốt. Idempotency giữ nguyên, chỉ ngân sách nới ra |
| D9 | Mọi lệnh sinh artifact chạy `--json` và log `task_id` trước khi `--wait` | `--wait` timeout tới 3600 s; mất task_id là mất luôn artifact đã tốn quota |
| D10 | Hàm thuần của pipeline **không** `import notebooklm` ở top level | `notebooklm` chỉ có trong `scripts/.venv`; nếu import ở top level thì test pure-logic chết ngay khâu collect |
| D11 | `--dry-run` được gọi API **read-only**, không phải offline | Muốn biết artifact first-add nào đã có thì phải hỏi server. Lệnh `list` không tính vào hạn mức nào — chỉ `chat`/`generate` mới tính |

## Phases

| # | Phase | File | Effort | Depends |
|---|---|---|---|---|
| 0 | Preflight + cài CLI | [phase-00](phase-00-preflight-and-cli-install.md) | 0.5h | — |
| 1 | Auth interactive + vệ sinh secret | [phase-01](phase-01-authentication-and-secret-hygiene.md) | 0.5h | 0 |
| 2 | Agent skill project-scoped + mirror | [phase-02](phase-02-project-scoped-agent-skill.md) | 1h | 1 |
| 3 | Đấu nối MCP server | [phase-03](phase-03-mcp-server-wiring.md) | 0.5h | 1 |
| 4 | Python library + venv `scripts/` | [phase-04](phase-04-python-library-and-venv.md) | 1h | 1 |
| 5 | Pipeline Revit docs → course assets | [phase-05](phase-05-revit-docs-to-course-assets-pipeline.md) | 4h | 4 |
| 6 | Test + cập nhật docs | [phase-06](phase-06-tests-and-documentation.md) | 1.5h | 2,3,5 |

Tổng ~9h. Phase 2/3/4 độc lập nhau — chạy song song được sau Phase 1.

## Rủi ro chính

- **Quota free tier** chặn Phase 5: 3 audio/ngày. Pipeline phải resumable, không phải chạy 1 phát. Ưu tiên khai thác artifact "first-add" (không tính quota).
- **`npx skills add` có thể ghi user-global** thay vì project — Phase 2 phải verify bằng thực nghiệm trước khi commit, fallback là vendor `SKILL.md` bằng tay.
- **Upstream không chính thức** — Google đổi API là gãy. Pin version trong Phase 0, ghi lại số version.
- **Chromium 170 MB tải im lặng** ở lần `login` đầu, không có progress bar — dễ tưởng treo.
- Thêm skill mới làm lệch baseline "60 SKILL.md" mà plan `260823` neo vào → đã đánh dấu `blocks`.

## Câu hỏi chưa chốt

1. ~~Tài khoản tier nào?~~ **Đã chốt 2026-09-04:** `lightingsang@gmail.com`, `settings.get_account_limits()` trả `AccountLimits(notebook_limit=500, source_limit=300, raw_limits=(6, 500, 300, 500000, 2), tier=2)` → **tier Pro**, không phải free. 108 notebook đang có. Xem D8 đã sửa.
2. Asset sinh ra (`.m4a`, `.png`, `.pdf`) có commit vào git không, hay chỉ để `output/` (đang bị gitignore)?

## Red Team Review

### Session — 2026-09-04
**Findings:** 9 (8 accepted, 1 rejected)
**Severity breakdown:** 1 Critical, 3 High, 5 Medium
**Lưu ý:** review chạy inline, không spawn reviewer subagent (session cấm delegate). Mọi finding đều kèm chứng cứ `file:line` đã verify bằng grep/find trên codebase thật.

| # | Finding | Severity | Disposition | Applied To |
|---|---|---|---|---|
| F1 | Lệnh test dùng `python` trần, nhưng lib chỉ có trong `scripts/.venv` → chết ở khâu collect | Critical | Accept | Phase 6 (+D10) |
| F2 | Phase 5 tự mâu thuẫn: `--dry-run` "không gọi API" vs `harvest_first_add()` phải list server | High | Accept | Phase 5 (+D11) |
| A1 | Success criterion đếm `ls` (63→64), lệch đơn vị với baseline `SKILL.md` (60) của plan `260823` | High | Accept | Phase 2 |
| S1 | `privacy-checker.cjs:29-33` chỉ khớp `.env*` + `/credentials/i`; cookie NotebookLM đọc thoải mái | High | Accept | Phase 1 |
| S2 | `settings.json:36` không khớp tool MCP → 38 tool chạy ngoài mọi hook của repo | Medium | Accept | Phase 3 |
| F3 | Không có gate auth trước khi tiêu quota; cookie chết giữa chừng = mất quota | Medium | Accept | Phase 5 |
| A2 | `.gitattributes:8` chỉ phủ `tests/skill-sync/fixtures/**`, và file vẫn untracked | Medium | Accept | Phase 6 |
| C2 | Extra `mcp` ở Phase 0 không bao giờ được dùng (Phase 3 đi `uvx` env riêng) | Medium | Accept | Phase 0 (+D1) |
| C1 | `notebooklm_client.py` là premature abstraction, chỉ 1 caller | Medium | **Reject** | — |

**Lý do reject C1:** Phase 6 cần import logic thuần mà không kéo tầng mạng (xem D10). Tách module chính là thứ làm việc đó khả thi — đây là tách vì testability, không phải vì abstraction.

### Whole-Plan Consistency Sweep

Delta từ 8 finding đã áp: extras `[browser,mcp,markdown]` → `[browser,markdown]`; đơn vị đo skill `ls` → `find -name SKILL.md`; `--dry-run` offline → read-only; thêm gate auth ở đầu Phase 5; thêm D10/D11.

Đã rà và đồng bộ:
- D1 trong `plan.md` khớp lệnh install ở `phase-00` (cả hai bỏ `mcp`); mục Architecture của `phase-00` giải thích lý do, success criterion bỏ dòng `notebooklm-mcp --help`.
- `phase-02` đổi cả bước 1, bước 3 và success criteria sang cùng một đơn vị (`SKILL.md` 60→61), risk row cập nhật baseline mới; `/tmp` thay bằng `$SCRATCH` có định nghĩa.
- `phase-05` đánh số lại bước 1–8 sau khi chèn gate auth; success criteria và risk table khớp mô tả `--dry-run` mới.
- `phase-01` thêm bước 6 (hook pattern) và dời "tier tài khoản" thành bước 7; Related Code Files thêm `privacy-checker.cjs`.
- `phase-06` Related Code Files thêm `.gitattributes`; risk row không còn nói quá phạm vi bảo vệ của nó.
- Effort không đổi (9h) — không finding nào thêm phase mới.

**Mâu thuẫn chưa giải quyết: không có.**
