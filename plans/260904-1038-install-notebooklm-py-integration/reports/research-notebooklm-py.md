# Research: notebooklm-py (teng-lin/notebooklm-py)

Source: GitHub repo README + `docs/{installation,mcp-guide,configuration,cli-reference,security,quota-limits}.md`, fetched 2026-09-04. License MIT. Unofficial — wraps undocumented Google APIs, subject to breaking change.

## Surfaces

| Surface | Entry point | Install |
|---|---|---|
| CLI | `notebooklm` | `uv tool install "notebooklm-py[browser]"` |
| MCP server | `notebooklm-mcp` (38 tools) | `uvx --from "notebooklm-py[mcp]" notebooklm-mcp` |
| Python lib | `from notebooklm import NotebookLMClient` (async) | `uv add notebooklm-py` |
| Agent skill | `SKILL.md` at repo root, `name: notebooklm` | `notebooklm skill install` *(user-global — unusable here)* / `npx skills add` |

## Extras matrix

| Extra | Purpose |
|---|---|
| `browser` | Interactive Playwright login (downloads Chromium ~170 MB on first `login`, 30–90 s, **no progress bar**) |
| `cookies` | Import cookies from installed Chrome/Edge |
| `headless` | Master-token auth |
| `markdown` | Markdown artifact exports |
| `mcp` | MCP server console script |

Python 3.10–3.14 supported (local: 3.14.5 ✅). On Windows the library auto-sets `WindowsSelectorEventLoopPolicy` and `PYTHONUTF8=1`; docs say plain `pip` is fine on Windows since python.org builds are not externally-managed (PEP 668 does not apply).

## Credential storage — verbatim layout

```
~/.notebooklm/
├── config.json                    # global: default profile, language
└── profiles/
    └── default/
        ├── storage_state.json     # LIVE GOOGLE COOKIES
        ├── master_token.json      # durable credential (only via --master-token)
        ├── context.json           # active notebook / conversation
        └── browser_profile/       # persistent Chromium user-data dir
```

Docs, verbatim: *"Treat both as secrets: keep them out of source control, logs, bug reports, shared artifact stores, and untrusted process environments."* Perms `0700` dirs / `0600` files; on Windows inherited ACLs substitute for POSIX mode bits.

Relevant env vars: `NOTEBOOKLM_HOME` (relocates everything), `NOTEBOOKLM_PROFILE`, `NOTEBOOKLM_AUTH_JSON` (inline auth for CI, no file writes), `NOTEBOOKLM_NOTEBOOK`, `NOTEBOOKLM_HL`, `NOTEBOOKLM_LOG_LEVEL`, `NOTEBOOKLM_BACKEND`.

`notebooklm status --paths` prints resolved paths + their source.

## MCP config block (verbatim from docs/mcp-guide.md)

```jsonc
{
  "mcpServers": {
    "notebooklm": {
      "command": "uvx",
      "args": ["--from", "notebooklm-py[mcp]", "notebooklm-mcp"]
    }
  }
}
```

`notebooklm mcp install claude-code` writes this to **`~/.claude.json`** (user-global). Transports: `stdio` (default) or `--transport http` (loopback 127.0.0.1:9420). HTTP bearer token is env-only via `NOTEBOOKLM_MCP_TOKEN` — *"There is no `--token` flag ... so it cannot leak via `ps aux`."* Server binds the **active profile at startup**.

38 tools: notebooks (5), sources (8), chat (6), notes (1), studio (6), research (3), sharing (3), server (1).

## `source add` — classification and supported types

Classification order when `--type` omitted: (1) argument contains `://` → `url`/`youtube`; (2) path exists → `file` upload; (3) otherwise → inline `text`.

Supported upload extensions: `.pdf .txt .md .markdown .doc .docx .pptx .rtf .odt .csv .tsv .epub`

**`.md` is natively supported — no PDF conversion needed for this repo's docs.**

Flags: `--title`, `--type`, `--mime-type`, `--follow-symlinks` (symlinks rejected by default), `--allow-internal` (SSRF guard for URL sources), `--timeout`, `--json`.

## Generate / download surface

Generate: `audio` (`--format deep-dive|brief|critique|debate`, `--length short|default|long`), `video` (`--style auto|whiteboard|kawaii|…`), `cinematic-video`, `report` (`--format briefing-doc|study-guide|blog-post|custom`), `slide-deck`, `quiz`, `flashcards`, `infographic`, `data-table`, `mind-map`, `revise-slide`.

Universal polling flags: `--wait/--no-wait` (default async), `--timeout` (300–3600 s per type), `--interval` (2 s), `--language`, `-s/--source ID` (repeatable), `--json` → `{"task_id": ..., "status": ...}`.

Download targets: audio→`.m4a`, video→`.mp4`, slide-deck→`.pdf`/`--format pptx`, infographic→`.png`, report→`.md`, quiz/flashcards→`--format json|markdown|html`. Flags `--all`, `--latest/--earliest`, `--force/--no-clobber`.

`--json` payloads are emitted even under `--quiet`. Errors: `{"error": true, "code": ..., "message": ...}`.

## Quotas — Standard (free) tier, the binding constraint

| Resource | Standard | Plus | Pro |
|---|---|---|---|
| Sources / notebook | 50 | 100 | 300 |
| Chats / day | 50 | 200 | 500 |
| **Audio overviews / day** | **3** | 6 | 20 |
| Video overviews / day | 3 | 6 | 20 |
| Reports · flashcards · quizzes · mind maps / day | 10 | — | — |
| Deep research | **10 / month** | 3/day | — |
| Cinematic video | unavailable | unavailable | 2/day |

Per-source cap 500 MB or 500 000 words. Reset windows are **rolling from first use**, not calendar midnight.

Exploitable: *"First-add auto-generated artifacts (the report/flashcards/infographic/slide-deck/audio/video overview generated when sources are first added) are generated once and do not count against limits."*

## Unresolved

- Whether `npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code` writes to `./.claude/skills/` (project) or `~/.claude/skills/` (user) on Windows — **must be verified empirically in Phase 2**; manual vendoring is the documented fallback.
- Whether the account in use is Standard or a paid tier — decides whether the Phase 5 pipeline runs in one pass or must be spread across days.
