---
name: tracked-mcp-json-carries-dev-path
description: .mcp.json is git-tracked yet holds machine-specific absolute exe paths for hprebar-revit/autocad/navis; every phase review should check it is not swept into the commit
metadata:
  type: project
---

`.mcp.json` at the repo root is **tracked** but its live content has absolute `F:\1-CONG VIEC\…\output\…\*.Mcp.Server.exe` command paths (one entry per host, `hprebar-navis` added 2026-09-15). Phase reports say "must not be committed" yet it shows as `M` in `git status`, so a `git commit -a` / `git add -A` leaks the dev path.

**Why:** three consecutive MCP phases (Revit, AutoCAD, Navis) each edited it locally; nobody has moved it to a `.mcp.json.example` + gitignore pattern — that is a user decision (the file is tracked today).

**How to apply:** in every phase review of an MCP host, run `git diff --stat -- .mcp.json`; if non-empty, raise a Medium finding recommending explicit staging or `git update-index --skip-worktree .mcp.json`, and leave the example/gitignore migration to the lead.
