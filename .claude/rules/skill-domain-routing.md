# Skill Domain Routing — Revit / WPF / C# Stack

Project này tập trung phát triển **Revit Add-In** dùng Nice3point templates + WPF + CommunityToolkit.Mvvm. Routing chỉ giữ skills phù hợp stack.

## Revit Add-In Development

```
User wants to...
├── Tạo Revit Add-In mới (scaffold)         → /bs:revit-addin
├── Setup multi-version (Revit 2022–2027)   → /bs:revit-addin
├── Viết ViewModel/View (MVVM + Toolkit)    → /bs:revit-wpf-mvvm
├── Style XAML (Theme.xaml, dark/light)     → /bs:revit-xaml-styles
├── Debug add-in trong Revit (F5)           → /bs:revit-debug
├── Troubleshoot FileLoadException, deploy  → /bs:revit-debug
├── Setup unit test (TUnit/xUnit/RevitTest) → /bs:revit-test
└── External Command / Application logic    → /bs:revit-addin
```

## Codebase Understanding

```
User wants to...
├── Tìm file/symbol nhanh                    → /bs:scout
├── Onboard repo mới / dump cho LLM context  → /bs:repomix
├── Semantic go-to-definition (C#)           → /bs:gkg
└── Build knowledge graph queryable          → /bs:graphify
```

## Documentation

```
User wants to...
├── Update project docs (./docs/)            → /bs:docs
├── Tra cứu Revit API / Nice3point docs      → /bs:docs-seeker
├── Inline diagram trong markdown            → /bs:mermaidjs-v11
├── Publish-grade SVG diagram                → /bs:tech-graph
├── Editable canvas (Excalidraw)             → /bs:excalidraw
├── Hand-off / session summary               → /bs:watzup
└── Sprint retro từ git history              → /bs:retro
```

## Documents / Office Files (xuất report từ add-in)

```
User wants to...
├── Xuất BOM/Schedule → Excel                → /bs:xlsx (qua document-skills)
├── Xuất shop drawing report → PDF           → /bs:pdf
├── Xuất specs Word                          → /bs:docx
└── Tạo slide thuyết trình                   → /bs:pptx
```

## Testing

```
User wants to...
├── Test framework decision (TUnit/xUnit)    → /bs:revit-test (Revit-specific)
├── Generic test runner / coverage           → /bs:test
└── Generate edge case scenarios             → /bs:scenario
```

## Security

```
User wants to...
├── STRIDE/OWASP audit + auto-fix            → /bs:security
└── Scan secrets, dependencies, OWASP        → /bs:security-scan
```

## MCP (Model Context Protocol) — Optional

Giữ cho trường hợp tương lai cần build MCP server cho Revit (vd. expose Revit API qua MCP cho AI agents).

```
User wants to...
├── Build MCP server                         → /bs:mcp-builder
├── Convert code thành CLI/MCP               → /bs:agentize
└── Discover/execute MCP tools               → /bs:use-mcp
```

## Content / Copy

```
User wants to...
├── Write landing page, email cho course      → /bs:copywriting
└── Brand identity (logo, banner)             → /bs:design
```

## Visual Aids

```
User wants to...
├── Code walkthrough HTML                     → /bs:preview --explain
├── Architecture diagram (PNG/SVG)            → /bs:tech-graph
├── Inline Mermaid v11 diagram                → /bs:mermaidjs-v11
├── Self-contained HTML showcase              → /bs:show-off
└── Read long markdown trong browser          → /bs:markdown-novel-viewer
```

## Domains KHÔNG dùng trong project này

Đã xóa khỏi `.claude/skills/`:
- Web FE (React/Vue/Next.js/Tailwind/shadcn)
- Web BE (Node/Python/FastAPI/Django, MongoDB/Postgres)
- Mobile (iOS/Android/Flutter)
- Web testing (Playwright)
- AI media gen (Imagen/Veo/MiniMax)
- DevOps web (K8s/Docker/Vercel)
- Payment (Stripe/Polar/SePay)
- Shopify, Better Auth, TanStack, Remotion, Three.js, Shader

Nếu user cần stack ngoài Revit/WPF → suggest tách project riêng, không pollute `.claude/` này.

## Usage Notes

- Pick ONE skill per distinct user intent
- Skill Revit thường combine với core workflow: `/bs:plan` → skill Revit cụ thể → `/bs:cook`
- Skill không listed ở đây = core utility (ask, brainstorm, sequential-thinking) — invoke on demand
- Tất cả skill Revit (`revit-*`) đều có frontmatter `category: revit` để filter dễ
