# Agent-skill migration audit — Phase 0 baseline

## Scope and method

**Observation (fresh, read-only, 2026-08-23):** this audit walked `.claude/skills/**/SKILL.md` and `.agents/skills/**/SKILL.md`, parsed each Claude `name:` frontmatter field, and calculated SHA-256 for each matching relative path. It also enumerated repository-local agents, rules, hooks, and invalid portable references. No global skill directory was read and no corpus file was modified.

**Inference:** this document is the normalization baseline. A different hash does not by itself prove an error: it may be an intentional provider adapter. Phase 1 must preserve each difference before modifying a portable representation.

## Discovery baseline

**Observation:** both trees contain 60 recursively discovered `SKILL.md` files, with matching relative bundle locations. The following is the complete Claude declared-name/relative-path inventory:

| Declared name | Relative path |
| --- | --- |
| bs:agentize | `agentize/SKILL.md` |
| bs:ask | `ask/SKILL.md` |
| bs:bootstrap | `bootstrap/SKILL.md` |
| bs:brainstorm | `brainstorm/SKILL.md` |
| bs:autoresearch | `bs-autoresearch/SKILL.md` |
| bs:debug | `bs-debug/SKILL.md` |
| bs:graphify | `bs-graphify/SKILL.md` |
| bs:loop | `bs-loop/SKILL.md` |
| bs:plan | `bs-plan/SKILL.md` |
| bs:predict | `bs-predict/SKILL.md` |
| bs:scenario | `bs-scenario/SKILL.md` |
| bs:security | `bs-security/SKILL.md` |
| bs:code-review | `code-review/SKILL.md` |
| bs:coding-level | `coding-level/SKILL.md` |
| bs:context-engineering | `context-engineering/SKILL.md` |
| bs:cook | `cook/SKILL.md` |
| bs:copywriting | `copywriting/SKILL.md` |
| ckm:design | `design/SKILL.md` |
| bs:docs | `docs/SKILL.md` |
| bs:docs-seeker | `docs-seeker/SKILL.md` |
| bs:docx | `document-skills/docx/SKILL.md` |
| bs:pdf | `document-skills/pdf/SKILL.md` |
| bs:pptx | `document-skills/pptx/SKILL.md` |
| bs:xlsx | `document-skills/xlsx/SKILL.md` |
| excalidraw | `excalidraw/SKILL.md` |
| bs:find-skills | `find-skills/SKILL.md` |
| bs:fix | `fix/SKILL.md` |
| bs:git | `git/SKILL.md` |
| bs:gkg | `gkg/SKILL.md` |
| bs:journal | `journal/SKILL.md` |
| bs:llms | `llms/SKILL.md` |
| bs:markdown-novel-viewer | `markdown-novel-viewer/SKILL.md` |
| bs:mcp-builder | `mcp-builder/SKILL.md` |
| bs:mermaidjs-v11 | `mermaidjs-v11/SKILL.md` |
| bs:plans-kanban | `plans-kanban/SKILL.md` |
| bs:preview | `preview/SKILL.md` |
| bs:problem-solving | `problem-solving/SKILL.md` |
| bs:project-management | `project-management/SKILL.md` |
| bs:project-organization | `project-organization/SKILL.md` |
| bs:repomix | `repomix/SKILL.md` |
| bs:research | `research/SKILL.md` |
| bs:retro | `retro/SKILL.md` |
| revit-addin | `revit-addin/SKILL.md` |
| revit-debug | `revit-debug/SKILL.md` |
| revit-test | `revit-test/SKILL.md` |
| revit-wpf-mvvm | `revit-wpf-mvvm/SKILL.md` |
| revit-xaml-styles | `revit-xaml-styles/SKILL.md` |
| bs:scout | `scout/SKILL.md` |
| bs:security-scan | `security-scan/SKILL.md` |
| bs:sequential-thinking | `sequential-thinking/SKILL.md` |
| bs:ship | `ship/SKILL.md` |
| bs:show-off | `show-off/SKILL.md` |
| bs:skill-creator | `skill-creator/SKILL.md` |
| bs:team | `team/SKILL.md` |
| bs:tech-graph | `tech-graph/SKILL.md` |
| bs:test | `test/SKILL.md` |
| bs:ui-ux-pro-max | `ui-ux-pro-max/SKILL.md` |
| bs:use-mcp | `use-mcp/SKILL.md` |
| bs:watzup | `watzup/SKILL.md` |
| bs:worktree | `worktree/SKILL.md` |

**Observation:** four skills are nested below the non-skill `document-skills/` container: `docx`, `pdf`, `pptx`, and `xlsx`. `_shared/` is not a skill: it contains `lib/` and `tests/`, but no `SKILL.md`. `common/` is a separate support directory and likewise has no `SKILL.md` (it contains two Python utilities and `README.md`).

**Inference:** both support directories must be represented as runtime/dependency infrastructure, not logical skill identities; discovery must not turn a container into a skill merely because a descendant has `SKILL.md`.

## Paired hash evidence

**Observation:** 31 of the 60 matching pairs are byte-identical. The identical paths are `bootstrap`, `brainstorm`, `bs-autoresearch`, `bs-loop`, `bs-predict`, `bs-security`, `code-review`, `docs`, `document-skills/docx`, `document-skills/pdf`, `document-skills/pptx`, `document-skills/xlsx`, `excalidraw`, `gkg`, `journal`, `mcp-builder`, `mermaidjs-v11`, `preview`, `problem-solving`, `project-organization`, `retro`, `revit-addin`, `revit-debug`, `revit-test`, `revit-wpf-mvvm`, `revit-xaml-styles`, `sequential-thinking`, `ship`, `test`, `ui-ux-pro-max`, and `watzup`.

**Observation:** the following 29 pairs differ. Each value is a full SHA-256 hash from the corresponding `SKILL.md`; C = `.claude/skills`, P = `.agents/skills`.

| Relative skill | C SHA-256 | P SHA-256 |
| --- | --- | --- |
| agentize | `5065d297957406ad85fa7d6555dd514490413736f593c5203d905deef3a8638f` | `46e88b9660dce101577db15fdd0367d642a289c591c89022e93fb0092688fe61` |
| ask | `82a01dae424fbe3fb12f6738ee977fea96f268216840f5c6be07a1d25af8e63f` | `52ac908fefc07b33966178cc32594009e5f20487182b1133e50647b0ae1e6807` |
| bs-debug | `0aae641778582804ca9cd4e3c9dcfdabd80eb2c36572759d21a0976495e4ea1f` | `6a2bd96df7f4254e4d42bfd5017ebe788e2cf338ca0d5034bced601294acb258` |
| bs-graphify | `ab24bc884c291a67d54740026111e216e9c8a6d98534595e0c0b5433776844b0` | `dbef26b5591f4a51d48341b9c945833d1fda0a6ea49c34eccb1acef6695f7f63` |
| bs-plan | `59e47f2941db7932945693db2892cfc44c906dfcf2dd62ba98ca04fc955bb262` | `3f977534826f65e989f712af76ece0239d807e008654ec1a2e3b5d263f4bf238` |
| bs-scenario | `03252d8b1bc0b06724da82acc58a9a1b7e05e06dcaf8a53c1444e7feb169305b` | `dfc5b969ced59ffa021ace38431397e1081c08183e9a3260b6babafb0b39f04c` |
| coding-level | `28fb8931edc919513d58239596bf6852a1979e351b4c272022407374addfe893` | `cd36ed6877a7b4916ebd107ab447706cf745bcda7f2aa92fb45b381a6b990e8f` |
| context-engineering | `95b12f1c7b8d2a5eec6f407a341ed68eb932d9c6b74834ec00d9c5b7c9b18b66` | `1666c44f374a722b3312203cfffb561ed311e5cdf7cbfd3e5b2863b387b0b9cd` |
| cook | `f917bb9e95b59df6740d358e134583f6ee300c03e057f16fe24f2bd7fb519f5e` | `ecd09ee79c34d2fd58d76c8011dfa173c75acc8e1d410cf03408bb4e6bfd7d37` |
| copywriting | `8035bf86bc71eb7648437c786a408df963b41cec431a02804420890d9ccc7f0c` | `c76f6ca62db4ed58effdc36360754e6f5942dd7de1c1ac9dc787393ff3910291` |
| design | `3bc917676ef68b3f89d3be23fdb0432595b58e092cb245c115407ce5d5d2124e` | `b4e0311107f4d63ac19e6fab2932bfb01c4524e48062cbb78556782337ec02f2` |
| docs-seeker | `3905475de58e086537be0878137c4f69471ca8a7fe78d397a953e4a921625c9e` | `f3f34d9c29a7778be1f2d9aeb6e7a15192667aa4717aa90aa18e1cea422a111b` |
| find-skills | `837c07a9554fb35aa0d70abae534bd88330c5883ce55c59282d3db374d11fad3` | `7cd51cfa8804817334df619617040eea1ced103867583a608a5723d45199b519` |
| fix | `934648b2877f3b3093d24fac8987cb8c939cc019e43925daab1fdc9e3b5e9c84` | `184226f9b7b6e89f2f756dafeed08e8c93476ceb297f1b8f7a40b1041330020a` |
| git | `3f067702aeb5423323e095b27ec4c641d131541ea7d3f426ba7185e42aec3982` | `e5b4e6e63a0949e94c00ed61a65df9e6a917d13ffac29cae759d39b46a19f298` |
| llms | `d2dae4d7e8d1b1d6665657e53b790fda4631f5822848eb339a7d7e9a4e68e413` | `9916891fd445ffadd7a5d7798c67bd173a002bf51be5c50af2216be947e32545` |
| markdown-novel-viewer | `e7ed0694b2e1f5aff195e0098210db928836a17fca46bede95eb80dbba88562a` | `0ef01dfe312536f4a0c24caae22c1b4decf371341909f76ce19b5d9be29dc289` |
| plans-kanban | `2636a0adf54e5fe721c6e42ad27f7c93dec6877ef463d9ee8e07bcbbf1c6b55a` | `ec9712887a01b07be49a16e57f9b4f35950ee75fd43398cc4ed3e08dbcf9efa2` |
| project-management | `3ab0ce96d44e1e8bb7967e07527886d964f14800b0ee92a27326b37967163e8c` | `b116e05e8ad8e0b9e0039a9ef998c089450b8123722d5870c74eeec9d432b978` |
| repomix | `ab4907cfda926346141d0e9e3db1d964a5b0bc8bed6c6378569234ad37141327` | `6022b931f8beeba9f9d5d2b51e259e6ecd17846941455bb4bc690e361abb184c` |
| research | `9ab82b324a399c35e47d0b9d62c5bafecb4d14c2e30462ecc72888ce38b4c5c9` | `a12a7a52af367c24e7c6b6d32098e622ebdf7acdaf660dafe98c7ed1c4a1172e` |
| scout | `1409a40e5239495fa98086fe49b1b1df9ebbb7689fa8d09a339e07ece35c6df1` | `d57217080f9b2441cc2cd7d48fb33c7923192a7d56d6da3596bdfa47f04190c0` |
| security-scan | `2ab45fccd8522a62de4e17cc8b6a62ef922da0b49bd02480dcd4ea3836156a01` | `f765f52908c2257a52e4b2430e5a063ae27ea6962e750f84eda36f3bd6b8524d` |
| show-off | `98e8defce4976091bf02a9f26b0e971b4d61c82664be4a1c2c9bc2aadad9f33d` | `21afbca6a9af07976036ba862b87e7dee6fc4e390301239241fe12741b986596` |
| skill-creator | `bfb081365d6d132d6c1b5f1f4f4ef5768611410ebdc0c470a29624350e489d4a` | `a998f0ebdfe1c94541d921e0e7c0238f292a46e31eb6920ece2633d3bf9e87d6` |
| team | `f66fd6058fab66dca6f84bd0ff7769537206f5ddb89c90e578c013283da474f4` | `e09cce842dd0c59bc2a132e4644fa595ab1a032ae2fa9724a760545dfb8e67e8` |
| tech-graph | `ffa5adb3c6a3abe6e8053b5dc3c6edbff7ffbc60890eaebd5f88529a57cb100e` | `0a29ca2e56ae6968af36cff899927e65b1be2b509fdb875c4f7a4635b9ae34f7` |
| use-mcp | `1e81440855fee912fb54b08eeada7cc39159e55a475b1529d0b77bbb879a5012` | `5987dc6d12bbb8fe0c6860a0d72a8c75ca95c9e126fc67039983a321acdb5570` |
| worktree | `44a7c39cafe0143148bda75e7f91c74c21af0535676f51e69e41514556df1f3e` | `5c5c162a293a333248b72b40427997cf9f77bd45aa540c5a62885d107cc244f3` |

**Inference:** the 29 differing pairs require independent Claude and portable base hashes plus a semantic base hash in the future manifest. They cannot safely be normalized with a blind byte-copy rule.

## Configuration corpus

**Observation:** 12 Claude agent inputs exist in `.claude/agents/`: `brainstormer`, `code-reviewer`, `code-simplifier`, `debugger`, `docs-manager`, `git-manager`, `journal-writer`, `planner`, `project-manager`, `researcher`, `tester`, and `ui-ux-designer` (all Markdown). The same 12 role names presently exist as `.codex/agents/*.toml`.

**Observation:** eight Claude rule inputs exist: `development-rules.md`, `documentation-management.md`, `orchestration-protocol.md`, `primary-workflow.md`, `review-audit-self-decision.md`, `skill-domain-routing.md`, `skill-workflow-routing.md`, and `team-coordination-rules.md`.

**Observation:** `.claude/hooks/` has 88 files: 76 `.cjs`, five `.md`, four `.txt`, one `.sh`, one `.example`, and one `.jsonl`. Its root runners are `descriptive-name.cjs`, `node-hook-runner.sh`, `privacy-block.cjs`, `scout-block.cjs`, `simplify-gate.cjs`, `skill-dedup.cjs`, `task-completed-handler.cjs`, and `teammate-idle-handler.cjs`. `.codex/hooks/` also has 88 files.

**Inference:** equal hook file counts are evidence of a copied implementation, not evidence that event payloads, matchers, or tool contracts are portable. Hooks need semantic platform adapters.

## Invalid portable references and environment limitations

**Observation:** 18 portable `SKILL.md` files contain `.Codex`-rooted paths or machine-global `~/.Codex` references: `ask`, `bs-graphify`, `bs-plan`, `coding-level`, `copywriting`, `design`, `docs-seeker`, `git`, `llms`, `markdown-novel-viewer`, `plans-kanban`, `research`, `scout`, `show-off`, `skill-creator`, `team`, `use-mcp`, and `worktree`. `skill-creator` additionally contains three `docs.Codex.com`/`code.Codex.com` URLs.

**Observation:** a root `README.md` is absent. `HPRebar/README.md` is present and is the available product README. `git rev-parse --is-inside-work-tree` returns `fatal: not a git repository (or any of the parent directories): .git`; therefore no Git history or status evidence is available from this working directory.

**Inference:** invalid paths and documentation URLs are migration defects, but they must be corrected through provider-aware conversion and validation rather than broad text replacement. The missing root README and unavailable Git data are environment limitations, not open design questions.

## Phase 0 preservation decision

**Observation:** `scripts/sync-agent-skills.py` and `.skill-sync/` are absent at this baseline.

**Inference:** implementation may begin only after the divergent portable contents are snapshotted and the fixture suite supplies a reliable behavior contract. The Phase 0 test suite intentionally remains RED until the engine exists.

## Unresolved questions

None.

## Baseline correction (2026-08-24)

**Observation:** the original `plans-kanban` Claude hash above omitted the hex character `7` after `dec687`, yielding an invalid 63-character value. A fresh read-only SHA-256 of `.claude/skills/plans-kanban/SKILL.md` is `2636a0adf54e5fe721c6e42ad27f7c93dec6877ef463d9ee8e07bcbbf1c6b55a`; the portable hash was already correct.

**Inference:** this is a Phase 0 audit transcription correction, not corpus drift. It preserves the baseline inventory of 60 paired skills and 29 divergent pairs.
