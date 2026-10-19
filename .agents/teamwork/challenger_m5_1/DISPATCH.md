# Dispatch: Challenger M5-1 (AutoCAD Interop, Bulge Geometry & Handle Linking Empirical Challenger)

**Agent**: `challenger_m5_1` (`teamwork_preview_challenger`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m5_1\`
**Date**: 2026-10-17

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 4, 5, 6)
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5\handoff.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
5. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Resource Directive (MANDATORY)
- Worker has already built and verified the solution.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- **MTP Concurrency Isolation**: You MUST pass `--results-directory "$env:TEMP\mtp-chal51-$([guid]::NewGuid())"` to prevent Microsoft.Testing.Platform log locking collisions!

## Mission
Empirically stress-test AutoCAD Interop, Bulge Geometry, and Handle Linking:
1. Run `dotnet test HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj --filter "FullyQualifiedName~FilletArc|FullyQualifiedName~XData|FullyQualifiedName~HandleLinking" --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal51-$([guid]::NewGuid())"`.
2. Empirically verify `FilletArcCalculator`:
   - Exact mathematical tests on bulges: 45° ($\tan(\pi/16) \approx 0.198912$), 90° ($\sqrt{2}-1 \approx 0.414214$), 135° ($\approx 0.668179$), 180° ($= 1.0$).
   - Clockwise vs Counter-Clockwise turns (sign verification).
   - Degenerate / collinear segments (180° straight turn, 0° turn).
   - Tangent clipping under acute angles and short segments ($t \le 0.45 \min(L_1, L_2)$).
3. Empirically verify `XDataService`:
   - Test conversion with null, empty, negative, and large integer values.
   - Verify all 12 items preserve DxfCodes and data integrity.
   - Verify handle links (DxfCode 1005) round-trip accurately.
4. Empirically verify Handle Linking:
   - Test clone re-binding remap logic: ensure old handles are completely remapped to new cloned handles with zero dangling references.
5. Record empirical evidence in `challenge_report.md` and deliver handoff report with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

## Mandatory Constraints
- `C:\kata_pro` is strictly read-only.
- Never inspect, decompile, or touch `Security.dll` or licensing code.


## 2026-10-17T04:04:22Z
You are challenger_m5_1, the AutoCAD Interop, Bulge Geometry & Handle Linking Empirical Challenger for Milestone M5 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m5_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).
