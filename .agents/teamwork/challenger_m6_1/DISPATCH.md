# DISPATCH — Gate M6: Unit Conversions, Parameter Mapping & Shape Adapters Challenger

**Assigned Agent**: `challenger_m6_1` (`teamwork_preview_challenger`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_1\`  
**Target Projects**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission
Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) has been implemented by `worker_m6`.
Your task is to empirically stress-test unit conversions, parameter bridges, and shape adapters:
1. Run target unit tests under concurrency isolation:
   ```powershell
   dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --filter "FullyQualifiedName~RevitUnitConverter|FullyQualifiedName~RevitParameterBridge|FullyQualifiedName~RevitShapeAdapter" --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal61-$([guid]::NewGuid())"
   ```
2. Empirically verify Unit Quarantine (`RevitUnitConverter`):
   - Precision: $1000\text{ mm} \rightarrow 3.280839895\text{ ft}$, $20\text{ mm} \rightarrow 0.0656167979\text{ ft}$.
   - Roundtrip: $x \equiv \text{FeetToMm}(\text{MmToFeet}(x))$ within $10^{-6}\text{ mm}$.
   - Vector and point conversions.
   - Tolerance comparisons (`AreCloseMm`, `AreCloseFeet`).
3. Empirically verify Parameter Bridge (`RevitParameterBridge`):
   - Mapped properties: Partition, Mark, Quantity, Diameter, CuttingLength.
   - Strict check: verify that single-bar length `REBAR_ELEM_LENGTH` is mapped to `CuttingLengthMm`, and verify that `TotalLength` is NEVER used as single-bar length.
4. Empirically verify Shape Adapters (`RevitShapeAdapter`, `RevitShapeMappingTable`, `RevitCurveAnalyzer`):
   - Standard shapes (`M_00`, `M_02`, `M_11`, `M_T1`, `M_77`, `M_51`).
   - U-bar symmetry: $|A - C| < 1.0\text{ mm} \rightarrow \text{dang02}$; $|A - C| \ge 1.0\text{ mm} \rightarrow \text{dang04}$.
   - Freeform fallback via `RevitCurveAnalyzer`: evaluate 3D curve vertices and bend angles.
5. Record empirical evidence in `challenge_report.md` and deliver explicit verdict in `handoff.md`: `APPROVE` or `REQUEST_CHANGES`.


## 2026-10-17T05:04:39Z
You are challenger_m6_1, the Unit Conversions, Parameter Mapping & Shape Adapters Empirical Challenger for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_1\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 4, 9, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution in Release mode.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal61-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Empirically stress-test Unit Conversions, Parameter Mapping, and Shape Adapters:
1. Run: dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --filter "FullyQualifiedName~RevitUnitConverter|FullyQualifiedName~RevitParameterBridge|FullyQualifiedName~RevitShapeAdapter" --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal61-$([guid]::NewGuid())".
2. Empirically verify RevitUnitConverter:
   - Precision: 1000 mm -> 3.280839895 ft, 20 mm -> 0.0656167979 ft.
   - Roundtrip: FeetToMm(MmToFeet(x)) == x within 1e-6 mm.
   - Vector and point transformations, tolerance helpers (AreCloseMm, AreCloseFeet).
3. Empirically verify RevitParameterBridge:
   - Verify single-bar length REBAR_ELEM_LENGTH is mapped to CuttingLengthMm, and confirm TotalLength is NEVER used as single-bar length.
   - Test mapping with null comments, missing parameters, and edge-case values.
4. Empirically verify RevitShapeAdapter:
   - Test standard shape mappings: M_00 -> dang01, M_02 -> dang03, M_11 -> dang22, M_T1 -> dang02, M_77 -> dang40.
   - Test U-bar symmetry: |A - C| < 1.0 mm -> dang02; |A - C| >= 1.0 mm -> dang04.
   - Test geometric fallback via RevitCurveAnalyzer.
5. Record empirical evidence in challenge_report.md and deliver handoff.md with explicit verdict: APPROVE or REQUEST_CHANGES.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
