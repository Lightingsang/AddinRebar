---
name: bash-heredoc-large-markdown-fails
description: In this repo's Bash tool (Git Bash on Windows), cat > file <<'EOF' with ~100+ line Vietnamese markdown bodies fails with "unexpected EOF while looking for matching quote" — use the Write tool for plan/phase files
metadata:
  type: project
---

Writing large plan markdown via Bash quoted heredoc (`cat > "path" <<'EOF' … EOF`) failed once the body exceeded roughly 100 lines (a 69-line `plan.md` worked; a ~110-line phase file errored with `unexpected EOF while looking for matching '''`). The Write tool succeeded for the same content.

**Why:** The Bash tool wrapper appears to re-quote the whole command; long bodies with mixed backticks/quotes/Unicode trip it. Not a content bug.

**How to apply:** For plan/phase markdown > ~80 lines in this repo, go straight to the Write tool. Bash heredocs are fine for short files. Keep `sed -i` for small in-place fixes.
