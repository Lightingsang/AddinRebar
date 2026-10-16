---
phase: 2
title: "Vendor VNConvert + golden tables"
status: completed
priority: P1
effort: "3h"
dependencies: []
type: AFK
---

# Phase 2: Vendor VNConvert

## Overview
Copy the managed library into the repo as its own project, wire it into `HPAutoCad.Aec`, and prove the Gemini port against the
original UniKey tables.

## Requirements
- Functional: `HPAutoCad/ThirdParty/VNConvert/VNConvert.csproj` (net8.0, `AllowUnsafeBlocks` as upstream) built from upstream
  `src/VNConvert/**` @ `205e569` **minus `NativeExports.cs`**; `HPAutoCad.Aec` → `ProjectReference`; added to `HPAutoCad.slnx`.
- `NOTICE.md` beside it: source URL, commit, authors (Phạm Kim Long / UniKey vnconv, AJS lisp.vn), "no LICENSE upstream, GPL origin,
  internal use only — never distribute outside the company", file list, every local edit.
- Golden data: `HPAutoCad.Aec.Tests/TestData/vn-charset-golden.json` = 134 Vietnamese letters (Unicode) ↔ TCVN3 bytes ↔ VNI-Win bytes,
  generated once by a scratchpad script that **parses upstream `vnconv/data.cpp`** (original human-written tables; the C++ file itself is
  not committed). Script kept in `HPAutoCad/tools/vnconvert/generate-golden-from-unikey-data.py` with the data.cpp sha256 pinned.
- Non-functional: vendored files byte-identical to upstream (a test hashes them against `NOTICE.md`); Clean Code checklist applies to the
  csproj/NOTICE/tests, not to vendored lines (third-party, Boy Scout not applied).

## Related Code Files
- Create: `HPAutoCad/ThirdParty/VNConvert/{VNConvert.csproj, NOTICE.md, *.cs, Core/*.cs}`, `HPAutoCad.Aec.Tests/VnConvert/VnCharsetGoldenTests.cs`,
  `HPAutoCad.Aec.Tests/TestData/vn-charset-golden.json`, `HPAutoCad/tools/vnconvert/generate-golden-from-unikey-data.py`
- Modify: `HPAutoCad/HPAutoCad.slnx`, `HPAutoCad.Aec/HPAutoCad.Aec.csproj`, `HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj` (test data copy)

## Implementation Steps
1. Copy files from the scratchpad clone (no build inside the clone); drop `NativeExports.cs`; record sha256 of each in NOTICE.
2. csproj: keep `IsAotCompatible`/`IsTrimmable` off-path (no AOT publish here), `TreatWarningsAsErrors` false for this project only.
3. Golden generator → JSON; tests: every letter TCVN3→Unicode, Unicode→TCVN3, same for VNI-Win (2-byte), round trip, upper/lower.
4. Port the 1-2 upstream test cases that add value (VIQR not needed) to xUnit v3.
5. `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false`; confirm `VNConvert.dll` lands in
   `HPAutoCad.McpBridge/bin/.../` (transitive copy) — bridge csproj stays unchanged.
6. `dotnet test HPAutoCad.Aec.Tests` + `HPAutoCad.Mcp.Server.Tests` + `HPCivil3d.McpBridge.Tests` (mirror untouched).

## Success Criteria
- [ ] Build green; golden tests 134 × 2 charsets × 2 directions pass; vendored-hash test passes.
- [ ] `VNConvert.dll` in the bridge output without touching the bridge csproj; Civil mirror tests green.
- [ ] Any golden mismatch = upstream bug → logged in NOTICE, fixed minimally, not hidden.

## Risk Assessment
- Gemini port deviates from UniKey tables → golden test catches it; fix is a logged vendored edit.
- Transitive copy does not reach `Contents\Bridge` in the deployed bundle → check `DeployBundle` item list; if needed, adjust Aec (not bridge) packaging.
