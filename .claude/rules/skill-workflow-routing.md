# Skill Workflow Routing — Revit Add-In Development

Workflow sequences cho Revit Add-In stack. Skills listed in typical execution order.

## Core Development Workflow (Revit Add-In)

```
/bs:plan (Stack-Aware 6 phase) → /bs:cook (build verify gate) → /bs:revit-debug (F5 smoke test) → /bs:code-review → /bs:ship → /bs:journal
```

| User Intent | Suggested Start |
|-------------|----------------|
| "tạo add-in revit mới" | `/bs:plan` → `/bs:revit-addin` → `/bs:cook` |
| "thêm command/button vào ribbon" | `/bs:plan --fast` → `/bs:cook` |
| "thêm view/dialog WPF" | `/bs:plan` → `/bs:revit-wpf-mvvm` → `/bs:cook` |
| "execute this plan" | `/bs:cook <plan-path>` |
| "quick implementation" | `/bs:cook --fast` (Revit project vẫn enforce build gate) |

## Scaffold-Only Workflow

```
/bs:revit-addin → dotnet new revit-addin → /bs:revit-xaml-styles (Theme.xaml) → /bs:revit-debug (verify F5)
```

| User Intent | Suggested Start |
|-------------|----------------|
| "scaffold project mới" | `/bs:revit-addin` |
| "setup Theme.xaml" | `/bs:revit-xaml-styles` |
| "config multi-version" | `/bs:revit-addin` → reference `multi-version-strategy.md` |

## Bugfix Workflow

```
/bs:scout → /bs:revit-debug → /bs:fix → /bs:test → /bs:code-review
```

| User Intent | Suggested Start |
|-------------|----------------|
| "add-in không load trong Revit" | `/bs:revit-debug` (troubleshoot runbook) |
| "FileLoadException khi load DLL" | `/bs:revit-debug` |
| "build fail RevitAPI.dll not found" | `/bs:revit-debug` |
| "ViewModel binding không update" | `/bs:revit-wpf-mvvm` → check pattern |
| "test fail trong Rider" | `/bs:revit-test` → setup guide |
| "CI/CD failing" | `/bs:fix --auto` |
| "tổng quan tại sao X xảy ra" | `/bs:scout` then `/bs:debug` |

## Investigation Workflow

```
/bs:scout → /bs:debug → /bs:brainstorm → /bs:plan
```

| User Intent | Suggested Start |
|-------------|----------------|
| "hiểu module X hoạt động sao" | `/bs:scout` |
| "tìm hiểu Revit API X có gì" | `/bs:docs-seeker` (Nice3point/RevitAPI docs) |
| "explore approach cho feature" | `/bs:brainstorm` then `/bs:plan` |

## Post-Implementation Checklist

After completing Revit add-in implementation:
- ✅ `dotnet build -c Debug.R<XX>` pass (HARD-GATE in `/bs:cook`)
- ✅ Test pass (TUnit in-process + xUnit pure logic)
- `/bs:code-review` — review code trước merge
- `/bs:ship` — full pipeline (tests → review → version → PR)
- `/bs:journal` — document decisions + lessons learned
- **(Optional but recommended)** F5 smoke test trong Revit thực — verify UI/UX không sai theme/owner/layout

## Setup Skills

Trước khi start implementation:
- `/bs:worktree` — isolated worktree cho feature/fix (tránh đụng main branch)
- `/bs:scout` — discover files + patterns
- `/bs:revit-addin` — nếu project chưa có Nice3point scaffold

## Revit-specific Skill Combos

| Tình huống | Skill combo |
|---|---|
| Tạo project mới từ đầu | `/bs:revit-addin` + `/bs:revit-xaml-styles` (Theme.xaml) + `/bs:revit-test` (setup test project) |
| Thêm WPF dialog | `/bs:revit-wpf-mvvm` + `/bs:revit-xaml-styles` (reuse styles) |
| Migrate version (vd R26 → R27) | `/bs:revit-addin` (preprocessor matrix) + `/bs:revit-debug` (verify build) |
| Setup CI/CD | `/bs:ship` + `/bs:devops` không có — fallback dùng GitHub Actions trực tiếp |
