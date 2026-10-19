# vn-autocad runtime (vendored)

Source: https://github.com/andyluu98/vn-autocad-skill @ 98d2b7536d20832f116245cd053be420f935a622 (Apache-2.0, see LICENSE).

Copied unchanged: `vncad/`, `tools/`, `examples/`, `pyproject.toml`, `requirements.txt`, `README.md`, `CHANGELOG.md`.
Not copied: `.claude-plugin/`, `agents/`, `commands/`, `docs/`, `tests/`, `.github/`.

Modified: the eight `vn-*` skills under `.claude/skills/` had `${CLAUDE_PLUGIN_ROOT}` replaced by
`.claude/skills/_shared/vn-autocad` (repo-relative; run commands from the repository root),
because they are installed as project skills, not as a plugin.

The five agents (`.claude/agents/kien-truc-su.md`, `ky-su-ket-cau.md`, `ky-su-co-dien.md`,
`kiem-soat-ban-ve.md`, `ky-su-du-toan.md`) and three commands (`.claude/commands/ho-so-moi.md`,
`soat-ban-ve.md`, `xuat-dwg.md`) come from the same commit, with the same path replacement.
