# PCC catalogue × NotebookLM cross-check

Date 2026-10-03. Catalogue: [PRAGMATIC_CLEAN_CODE_RULES.md](../../../docs/clean-code/PRAGMATIC_CLEAN_CODE_RULES.md) (PCC-001…293). Notebook "Pragmatic Clean Code" `2d479edc-…`, source `8d272cd3-…`, CLI `notebooklm ask … --json`, one question per chapter ("list every principle, guideline, code smell, exception and 'do not apply mechanically' caveat … terse bullet list").

**Status: BLOCKED after ch12.** ch13 ask stalled (NETWORK_ERROR, no streamed bytes for 180 s); ch14 ask → "Authentication expired or invalid". Per instructions stopped, no `notebooklm login`. ch13, ch14, ch15 NOT cross-checked.

Method: each top-level NotebookLM bullet mapped to the chapter's rules + smells table + "NOT mechanically" list (and cross-chapter rules when the book places the idea elsewhere). Classes: COVERED / PARTIAL / MISSING / DOUBTFUL (claim not backed by NotebookLM's own cited text; counted separately, not as a gap).

## Summary

| Ch | Title | Bullets | Covered | Partial | Missing | Doubtful | Note |
|---|---|---|---|---|---|---|---|
| 1 | Understanding clean code | 24 | 24 | 0 | 0 | 0 | brevity caveat lives in PCC-187 (ch11), not ch1 list — fine |
| 2 | Meaningful names | 31 | 31 | 0 | 0 | 0 | "incomplete names" cite is ch11 text → PCC-189 |
| 3 | Writing better methods | 28 | 27 | 0 | 0 | 1 | answer returned **0 references** (ungrounded); every item checked against catalogue instead |
| 4 | Formatting code | 21 | 21 | 0 | 0 | 0 | |
| 5 | SRP | 18 | 17 | 0 | 0 | 1 | |
| 6 | OCP | 18 | 18 | 0 | 0 | 0 | |
| 7 | LSP | 16 | 16 | 0 | 0 | 0 | |
| 8 | ISP | 17 | 17 | 0 | 0 | 0 | |
| 9 | DIP | 19 | 17 | 1 | 0 | 1 | |
| 10 | Static methods and dependencies | 14 | 14 | 0 | 0 | 0 | |
| 11 | Designing smaller classes | 21 | 20 | 0 | 0 | 1 | |
| 12 | Organizing classes and projects | 21 | 21 | 0 | 0 | 0 | |
| 13 | Coupling, cohesion, reuse | — | — | — | — | — | BLOCKED (network stall) |
| 14 | Using comments effectively | — | — | — | — | — | BLOCKED (auth expired) |
| 15 | Testable code and clean tests | — | — | — | — | — | not asked |
| **Σ ch1–12** | | **248** | **243** | **1** | **0** | **4** | |

Conclusion so far: catalogue is a superset of NotebookLM's chapter digests for ch1–12; NotebookLM surfaced no principle, smell or caveat without a PCC rule. Catalogue is richer (detection signals, review questions, tables) everywhere.

## PARTIAL items

### P1 — ch9: Dependency Injection of a concrete type is not Dependency Inversion

- **NotebookLM (paraphrase):** injecting a concrete class through the constructor uses DI but still breaks DIP; the consumer stays coupled to the concrete type. Listed as a smell and as a caveat ("DI without DIP is incomplete").
- **Citation given (short):** refs 6–8 — DIP/DI comparison table; "inject dependencies instead of creating them inside the class"; ref 9/20 — bookstore first injects `FastWheels`, then "a better approach would be to introduce an abstraction and inject it".
- **Catalogue today:** substance covered by PCC-154 (signal "constructor parameter typed as a concrete service") and PCC-180 ("a concrete constructor dependency still violates DIP"). But PCC-158, the rule titled "Don't confuse DIP with DI; check both", only detects the opposite half (interface-typed field filled by `new Concrete()`), and does not link PCC-180.
- **Proposed action — amend PCC-158 (no new rule):**
  - *Detection Signals* — add: "Constructor parameter or injected field typed as a concrete low-level class: injection without inversion (book's intermediate bookstore step before the delivery abstraction)."
  - *Related Rules* — add PCC-154, PCC-180.
  - *Review Question* — extend: "…and is what it receives typed as an abstraction, not a concrete class?"

## MISSING items

None for ch1–12.

## DOUBTFUL items (no catalogue change)

| # | Ch | NotebookLM claim (paraphrase) | Why doubtful | Action |
|---|---|---|---|---|
| D1 | 3 | Step-down ordering "reads like a newspaper article"; labels the abstraction rule "SLAP" | Whole ch3 answer carried **no citations**; both labels absent from catalogue's ch3 extraction (PCC-070, PCC-073) — likely imported from general Clean Code lore | None; PCC-070/073 already state the rules without the labels |
| D2 | 5 | Combining responsibilities makes classes multiply "exponentially (M×N)" | Cited jacket/backpack text describes multiplication; M×N is multiplicative, not exponential | None; PCC-116 wording ("add up instead of multiplying") is correct |
| D3 | 9 | Stable value types (`Person`, `Package`, collections) need no interface | Cited refs 3/20/23 only give the qualifier "when behaviour may vary"; the example list is NotebookLM's | None; PCC-154 already records "book gives no list of dependencies fine to keep concrete" |
| D4 | 11 | "Readability by team members and peer review should guide formatting decisions" (under brevity caveat) | Cited ref 14 is the clever-one-liner passage; nothing about review guiding formatting | None; PCC-187 covers the actual point (clarity over brevity) |

## Chats used

14 `ask` calls issued (budget 16): ch1–ch12 answered; ch13 stalled after 180 s (may or may not have consumed quota); ch14 refused (auth expired). No retry, no `--new`, no login.

## To finish (needs user)

1. `notebooklm login` (interactive, user only), then `notebooklm auth check --test --json`.
2. Re-ask ch13, ch14, ch15 with the same question template (`--request-timeout 300` for ch13), then map against catalogue lines 4211–5532 (PCC-219…293).
