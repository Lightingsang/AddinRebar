---
phase: 2
title: "Agent skill project-scoped + mirror"
status: pending
priority: P1
effort: "1h"
dependencies: [1]
---

# Phase 2: Agent skill project-scoped + mirror

## Overview
Đưa skill `notebooklm` vào `.claude/skills/` của **repo này** (không phải `~/.claude/skills/`), mirror sang `.agents/skills/`, và khai báo nó trong bảng routing để agent biết khi nào dùng.

## Requirements
- Functional: skill xuất hiện trong catalog project, invoke được, và `SKILL.md` khớp bản upstream.
- Non-functional: không ghi bất cứ thứ gì vào `~/.claude/skills/` — `CLAUDE.md` cấm tuyệt đối.

## Architecture

Upstream cấp hai đường cài, **một trong hai vi phạm rule repo**:

| Đường | Ghi vào | Dùng được? |
|---|---|---|
| `notebooklm skill install` | `~/.claude/skills/notebooklm/` + `~/.agents/skills/notebooklm/` | ❌ `CLAUDE.md` cấm sửa `~/.claude/skills` |
| `npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code` | *cho là* `./.claude/skills/` | ⚠️ **phải verify** |
| Vendor tay `SKILL.md` từ repo upstream | `./.claude/skills/notebooklm/` | ✅ fallback chắc chắn |

Đích cuối giống nhau bất kể đường nào:

```
.claude/skills/notebooklm/SKILL.md     ← nguồn
.agents/skills/notebooklm/SKILL.md     ← mirror (D3: copy tay)
```

Mirror bằng tay vì `.skill-sync/config.json` bị gitignore và không tồn tại trong checkout này — bootstrap engine sync là scope của plan `260823`, không phải plan này.

Skill upstream (`name: notebooklm`) hướng dẫn agent: setup/auth, invariant vận hành (giữ ID, `--json`, gate source-ready, chờ artifact), ranh giới xin phép trước thao tác phá hủy, và workflow source→artifact.

## Related Code Files
- Create: `.claude/skills/notebooklm/SKILL.md`
- Create: `.agents/skills/notebooklm/SKILL.md`
- Modify: `.claude/rules/skill-domain-routing.md` — thêm dòng routing
- Modify: `CLAUDE.md` — ghi nhận skill mới + ràng buộc "không dùng `notebooklm skill install`"

## Implementation Steps

1. Chụp trạng thái trước, để chứng minh không rò ra user-global:
   ```bash
   SCRATCH="${TMPDIR:-/tmp}/notebooklm-plan"; mkdir -p "$SCRATCH"
   ls ~/.claude/skills/ > "$SCRATCH/skills-before.txt"
   find .claude/skills -name SKILL.md | wc -l    # kỳ vọng 60
   find .agents/skills -name SKILL.md | wc -l    # kỳ vọng 60
   ```
   Đếm bằng **số file `SKILL.md` đệ quy**, không phải `ls | wc -l`. `ls` đếm cả `README.md`, `INSTALLATION.md`, `_shared/`, `common/` — không phải skill. Plan `260823` neo baseline vào đúng con số 60 này, nên phải đo cùng đơn vị thì mới so được.
2. Thử đường `npx` **trước**, vì nó giữ được liên kết với upstream:
   ```bash
   npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code
   ```
3. **Verify ngay** nó ghi vào đâu — đây là bước quyết định, không được bỏ:
   ```bash
   ls -la .claude/skills/notebooklm/ 2>/dev/null && echo "PROJECT ✅"
   diff <(ls ~/.claude/skills/) "$SCRATCH/skills-before.txt" || echo "RÒ RA USER-GLOBAL ❌"
   ```
4. Nếu bước 3 báo rò user-global: xóa thứ vừa ghi ở `~/.claude/skills/notebooklm/`, rồi vendor tay — tải `SKILL.md` từ `https://raw.githubusercontent.com/teng-lin/notebooklm-py/main/SKILL.md` vào `.claude/skills/notebooklm/SKILL.md`.
5. **Cắt bloat — `npx skills add` clone TOÀN BỘ repo upstream, không chỉ `SKILL.md`.** Đã đo thực tế: **111 MB** (`tests/` 77M, `docs/` 24M, `src/` 7.3M, PNG 657K, `uv.lock` 556K). Nguy hiểm hơn dung lượng: nó thả `CLAUDE.md`, `AGENTS.md`, `.gitignore`, `.gitattributes`, `.env.example` lồng bên trong `.claude/skills/` — `CLAUDE.md` lồng nhau có thể bị đọc thành project instruction. Cắt còn `SKILL.md` + `LICENSE` (giữ LICENSE vì vendor code MIT):
   ```bash
   cd .claude/skills/notebooklm
   pwd | grep -q "skills/notebooklm$" || { echo "SAI THƯ MỤC - DỪNG"; exit 1; }
   for item in .[!.]* *; do
     case "$item" in SKILL.md|LICENSE) ;; *) rm -rf -- "$item" ;; esac
   done
   ```
   Kết quả mong đợi: 111M → 28K, đúng 2 file.
6. Đối chiếu nội dung với upstream bất kể đi đường nào:
   ```bash
   # npx cài trên Windows ghi CRLF, upstream raw là LF -> so sau khi normalize,
   # không thì diff báo lệch toàn file trong khi nội dung giống hệt.
   curl -sL https://raw.githubusercontent.com/teng-lin/notebooklm-py/main/SKILL.md -o /tmp/upstream-SKILL.md
   diff <(tr -d '' < /tmp/upstream-SKILL.md) <(tr -d '' < .claude/skills/notebooklm/SKILL.md)
   ```
7. Mirror sang `.agents/`:
   ```bash
   mkdir -p .agents/skills/notebooklm
   cp .claude/skills/notebooklm/SKILL.md .agents/skills/notebooklm/SKILL.md
   ```
8. Thêm routing vào `.claude/rules/skill-domain-routing.md`, mục **Documentation** — đây là nơi đúng vì skill này phục vụ nghiên cứu/soạn tài liệu:
   ```
   ├── Nạp tài liệu → NotebookLM, sinh học liệu   → /notebooklm
   ```
9. Cập nhật `CLAUDE.md`: ghi skill mới và ràng buộc — chạy `notebooklm skill install` sẽ ghi user-global, cấm dùng; cài lại thì theo phase này.
10. Xác nhận `~/.claude/skills/` không đổi so với ảnh chụp bước 1.

## Success Criteria
- [ ] `.claude/skills/notebooklm/` chỉ còn `SKILL.md` + `LICENSE` (không còn `CLAUDE.md`/`AGENTS.md`/`src/`/`tests/` lồng bên trong)
- [ ] `du -sh .claude/skills/notebooklm/` ≈ 28K, không phải 111M
- [ ] `SKILL.md` khớp upstream sau khi normalize line-ending
- [ ] `.agents/skills/notebooklm/SKILL.md` byte-identical với bản `.claude/`
- [ ] `~/.claude/skills/` không thay đổi (diff với ảnh chụp bước 1 rỗng)
- [ ] `find .claude/skills -name SKILL.md | wc -l` → **61** (60 → 61)
- [ ] `find .agents/skills -name SKILL.md | wc -l` → **61** (60 → 61)
- [ ] `skill-domain-routing.md` và `CLAUDE.md` đã có mục mới
- [ ] Skill invoke được trong session mới

## Risk Assessment
| Rủi ro | Xử lý |
|---|---|
| `npx skills add` ghi user-global bất chấp `--agent claude-code` | Bước 3 bắt được; bước 4 rollback + vendor tay. Đây là lý do bước 1 chụp ảnh trước |
| `npx skills add` đổi flag/CLI upstream | Fallback vendor tay không phụ thuộc CLI đó chút nào |
| Installer thả `CLAUDE.md`/`AGENTS.md` lồng trong `.claude/skills/` | Bước 5 xoá. Đây là rủi ro đã xảy ra thật, không phải giả định |
| Skill chạy với full agent permission; Socket quét ra 7 alert lúc cài | Đã cắt còn `SKILL.md` (văn bản thuần) + `LICENSE` — không còn code thực thi nào trong skill dir |
| SKILL.md upstream drift khỏi bản vendor | Phase 6 ghi commit hash upstream vào docs để đối chiếu sau |
| Thêm skill làm lệch baseline 60 `SKILL.md` của plan `260823` | Đã đánh dấu `blocks` trong frontmatter cả hai plan. Baseline mới là **61/61**, đã verify con số 60 hiện tại bằng `find`; plan `260823` phải rebase sang 61 |
| `AGENTS.md` là bản mirror sinh ra từ `CLAUDE.md` | Sửa `CLAUDE.md` rồi re-sync; **không** sửa tay `AGENTS.md` |
