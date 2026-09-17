# Phase 0 — baseline (đo 2026-09-17 trước khi sửa `McpShared/`)

Repo sạch ở `McpShared/`, `HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPEtabs/` (`git status --short` chỉ 2 thư mục `.claude/` untracked không liên quan).

## `tools/list` 4 host (Release rebuild, registry cách ly `%TEMP%\hp-mcp-snapshot-civil3d-before\<host>`)
`pwsh plans/260917-1633-civil3d-mcp-2026/reports/snapshot-tools-list.ps1 -Tag before`

| Host | Tools | `HPRebar.Mcp.Server.Core.dll` SHA-256 (16) | File |
|---|---|---|---|
| revit | **33** | `3A3A774F3990A6CB` | `phase-00-tools-list-before-revit.json` |
| autocad | **62** | `3A3A774F3990A6CB` | `phase-00-tools-list-before-autocad.json` |
| navis | **24** | `3A3A774F3990A6CB` | `phase-00-tools-list-before-navis.json` |
| etabs | **24** | `3A3A774F3990A6CB` | `phase-00-tools-list-before-etabs.json` |

Cùng một Core.dll cho 4 exe (cùng build). SHA đầy đủ trong `phase-00-core-dll-before-<host>.sha256`.

## 7 suite test

| Suite | Total | Failed | Skipped |
|---|---|---|---|
| `McpShared/HPRebar.Mcp.Server.Core.Tests` | **164** | 0 | 0 |
| `McpShared/HPRebar.McpBridge.Core.Net48Tests` | **62** | 0 | 0 |
| `HPRebar/HPRebar.Mcp.Server.Tests` | **109** | 0 | 0 |
| `HPAutoCad/HPAutoCad.Mcp.Server.Tests` | **280** | 0 | 0 |
| `HPAutoCad/HPAutoCad.Aec.Tests` | **225** | 0 | 0 |
| `HPNavis/HPNavis.Mcp.Server.Tests` | **49** | 0 | 0 |
| `HPEtabs/HPEtabs.Mcp.Server.Tests` | **81** | 0 | 0 |

Không chạy (cần host cài, không nằm trong gate engine): `HPNavis.McpBridge.Tests` (net48, 135), `HPEtabs.McpBridge.Tests` (184).

Gate phase 0: 4 JSON `after` == `before` (diff rỗng), SHA Core.dll **khác** (rebuild thật), 7 suite = số trên (+ test mới ở Core.Tests).
