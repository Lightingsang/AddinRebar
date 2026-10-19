# Dispatch Instructions: challenger_m3_it2_2_gen2

## Identity
- Role: Concurrency Stress Challenger & Pipeline Verifier
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_2_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation Challenge)

## Scope & Tasks
1. Stress test concurrency synchronization:
   - Run `ConnectAsync_ConcurrentCalls_SynchronizesWithoutThrowingOrLeaking` in `RevitPipeClientAdversarialTests.cs`.
   - Verify that 5 concurrent connection requests cleanly synchronize without deadlocking, throwing, or leaking pipe streams.
2. Verify composite script payload:
   - Run `CompositeBimGenerator_IncludesAll8StagesInScriptAndArgs` in `StageGeneratorTests.cs`.
   - Verify that all 8 stages and payload args (`levels`, `grids`, `columns`, `beams`, `slabs`, `walls`, `openings`, `rooms`, `fixtures`, `pipes`, `rebars`) are present.
3. Test pipeline atomic dry-run execution:
   - Verify `BimPipelineEngine` with `DryRun = true` and `AtomicDryRun = true` successfully invokes the full 8-stage composite script.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:54:39Z
You are challenger_m3_it2_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Stress-test concurrency and pipeline dry-run:
1. Run ConnectAsync_ConcurrentCalls_SynchronizesWithoutThrowingOrLeaking.
2. Run CompositeBimGenerator_IncludesAll8StagesInScriptAndArgs.
3. Test atomic dry-run pipeline invocation.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
