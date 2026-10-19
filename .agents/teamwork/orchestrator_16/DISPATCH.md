# ORCHESTRATOR DISPATCH SPECIFICATION — HPKATA

## Mission
Clean-room reverse-engineer the KataPro structural rebar detailing suite at `C:\kata_pro` into a comprehensive technical blueprint (`docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`) and build **HPKata** (`HPKata/HPKata.slnx`), a production-ready dual-host **AutoCAD 2026 (.NET 8)** and **Revit 2026 (.NET 8)** rebar detailing, dynamic tag-linking, scheduling, and 1D cutting-stock optimization system with two-way data compatibility with existing KataPro drawings and Excel templates.

## Context & Authoritative Request
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-16T23:55:10Z`)
- **Working directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_16\`
- **Project Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`

## Reference Material & Target Artifacts (STRICTLY READ-ONLY)
- `C:\kata_pro\Kata_pro64_Cad2025.dll` (.NET 8 AutoCAD 2025+ plugin, 4,276 TypeDefs, 33,792 MethodDefs)
- `C:\kata_pro\Kata_Class_Lib.dll` (.NET Framework 4.8 shared library, 491 TypeDefs, 3,356 MethodDefs)
- `C:\kata_pro\Kata_Class_Lib_Revit.dll` (.NET Framework 4.8 Revit integration library, 692 TypeDefs, 4,357 MethodDefs)
- `C:\kata_pro\Goi lenh.lsp` (AutoCAD Lisp loader, >110 command aliases, and 86-line default configuration block at lines 152–237)
- `C:\kata_pro\Iso_kata.dwg` (Block definitions, attributes, layers, dimension styles, text styles)
- `C:\kata_pro\dangthep\dang00.wmf` .. `dang67.wmf` (68 standard rebar shape definitions)
- `C:\kata_pro\Kata.xlsm`, `C:\kata_pro\Kata ENG.xlsm`, `C:\kata_pro\Iso_QHPlus.xls` (Excel scheduling, coupler, and QS templates)
- `C:\kata_pro\Icon\Ribbon\Ribbon_Command_Clicks.txt` (Full Ribbon command inventory)
- REA CLI: `C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd` and `.NET` reflection / IL inspection tools.

## Key Requirements & Acceptance Criteria
- **R1 / AC1**: Technical Blueprint `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` covering all 6 subsystems, >110 commands, exact XData RegApp/DxfCode schemas, Block/Attribute schemas from `Iso_kata.dwg`, all 68 rebar shapes (`dang00`..`dang67`), and the 86 configuration lines of `Goi lenh.lsp`.
- **R2 / AC2**: Shared Domain Core & UI Library in `HPKata/HPKata.slnx`:
  - `HPKata.Core`: Zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`. 68 rebar shapes, bend/hook/lap calculations, 86-parameter config parser/serializer, KataPro XData & Block Attribute 2-way serialization, schedule aggregation, coupler schedule, 1D cutting-stock optimization (`to_hop_thep`), QR tag generator, headless Excel export (`ClosedXML`) + COM interop.
  - `HPKata.UI`: `net8.0-windows` WPF + `CommunityToolkit.Mvvm` + `MahApps.Metro` Dark/Light host-theme aware dialogs for Setup, Drawing & Editing, Rebar Schedule / Cutting Optimization.
- **R3 / AC3**: Automated Tests in `HPKata/HPKata.Core.Tests`:
  - Minimum 100 unit tests passing 100% filter-free.
  - Full coverage of 68 shapes, bend/hook/lap, 86 config parameters, XData round-trip, cutting-stock optimization on 11.7m stock bars, Excel reports.
- **R4 / AC4**: AutoCAD 2026 & Revit 2026 Host Add-Ins (Phase 1 Vertical Slice):
  - `HPKata.AutoCad`: .NET 8 AutoCAD 2026 plugin + `GoiLenh_HPKata.lsp`. All Phase 1 commands (`vt`, `sua`, `link`, `ud`, `tkt`, `tohopthep`, `toexcel`, etc.).
  - `HPKata.Revit`: .NET 8 Revit 2026 add-in. ExtensibleStorage/Parameter bridge to Revit Rebar, 68 shape codes, schedule/cutting-stock/export commands.
  - Verified with zero runtime binding or guard errors.
- **Governance & Safety**:
  - NEVER decompile, copy, or circumvent `Security.dll` or licensing DRM.
  - Read-only `C:\kata_pro`.
  - Comply with `docs/clean-code/HP_CLEAN_CODE_CORE.md` (Class <= 300 lines, Method <= 50 lines, 0 dead code, 0 unannotated catch).


## 2026-10-16T23:57:03Z

You are orchestrator_16, the Project Orchestrator for HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_16\

Please read your dispatch specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_16\DISPATCH.md
and the authoritative original user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-16T23:55:10Z)

Your mission:
Clean-room reverse-engineer the KataPro structural rebar detailing suite at C:\kata_pro into a comprehensive technical blueprint (docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md) and build HPKata (HPKata/HPKata.slnx), a production-ready dual-host AutoCAD 2026 (.NET 8) and Revit 2026 (.NET 8) rebar detailing, dynamic tag-linking, scheduling, and 1D cutting-stock optimization system with two-way data compatibility with existing KataPro drawings and Excel templates.

Target Artifacts to Inspect (Read-Only):
- C:\kata_pro\Kata_pro64_Cad2025.dll (.NET 8 AutoCAD 2025+ plugin, 4,276 TypeDefs, 33,792 MethodDefs)
- C:\kata_pro\Kata_Class_Lib.dll (.NET Framework 4.8 shared library, 491 TypeDefs, 3,356 MethodDefs)
- C:\kata_pro\Kata_Class_Lib_Revit.dll (.NET Framework 4.8 Revit integration library, 692 TypeDefs, 4,357 MethodDefs)
- C:\kata_pro\Goi lenh.lsp (AutoCAD Lisp loader, >110 command aliases, and 86-line default configuration block at lines 152–237)
- C:\kata_pro\Iso_kata.dwg (Block definitions, attributes, layers, dimension styles, text styles)
- C:\kata_pro\dangthep\dang00.wmf .. dang67.wmf (68 standard rebar shape definitions)
- C:\kata_pro\Kata.xlsm, C:\kata_pro\Kata ENG.xlsm, C:\kata_pro\Iso_QHPlus.xls (Excel scheduling, coupler, and QS templates)
- C:\kata_pro\Icon\Ribbon\Ribbon_Command_Clicks.txt (Full Ribbon command inventory)
- REA CLI: C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd and .NET reflection / IL inspection tools.

Strict Constraints:
1. Never decompile, copy, or circumvent Security.dll, hardware locks, or license checks in C:\kata_pro.
2. Treat C:\kata_pro as strictly read-only.
3. Clean Code Governance: docs/clean-code/HP_CLEAN_CODE_CORE.md, class <= 300 lines, method <= 50 lines, 0 dead code, 0 unannotated catch.
4. HPKata.Core: host-agnostic C# library with zero references to Autodesk.AutoCAD.* or Autodesk.Revit.*.
5. Create and maintain plan.md and progress.md in your working directory (.agents/teamwork/orchestrator_16/).


## 2026-10-17T02:41:11Z

URGENT SYSTEM RESOURCE DIRECTIVE FROM USER:
During the Gate M3 5-agent parallel swarm, the user's Windows desktop popped up two modal OS error dialogs:
1. `dotnet.exe - System Warning: Unknown Hard Error`
2. `powershell.exe - System Error: A new guard page for the stack cannot be created` (NTSTATUS 0xC00000FD STATUS_STACK_OVERFLOW / Windows desktop heap & commit exhaustion from 5 subagents spawning concurrent `dotnet` / `VBCSCompiler` / `MSBuild` / `powershell` / `fsi` processes at the same time).

MANDATORY RULES FOR ALL REMAINING MILESTONES (M4, M5, M6, AND GATES):
1. `HPKata/Directory.Build.props` now has `<UseSharedCompilation>false</UseSharedCompilation>` and `<BuildInParallel>false</BuildInParallel>`. Do NOT remove these properties.
2. Every `dotnet build` or `dotnet test` invocation by any worker, reviewer, challenger, or auditor MUST include `--disable-build-servers -m:1` (and `--no-build` when running `dotnet test` in Gate subagents after the worker has already built the solution).
3. Gate subagents (Reviewers, Challengers, Auditors) running in parallel MUST NOT simultaneously compile or spawn heavy `dotnet build` / `dotnet fsi` / recursive PowerShell loops. Have the Worker run `dotnet build --disable-build-servers -m:1` and `dotnet test --disable-build-servers -m:1` once, record the log, and have parallel Reviewers/Auditors perform static inspection or run lightweight `--no-build` checks without concurrent compilation storms or unbounded stack recursion.
4. Never write a test or script that triggers deep/infinite stack recursion (`StackOverflowException`).
