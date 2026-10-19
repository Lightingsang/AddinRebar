# DISPATCH — reviewer_c_iter2_2

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C — Iteration 2 (Remediation Review)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_iter2_2
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Remediation Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2\handoff.md

## Mission
Review việc khắc phục trong `run-benchmark.ps1` và tính đồng bộ thư mục:
1. `Remove-BenchmarkSandbox`: đã dùng tiền tố nghiêm ngặt `$requiredPrefix = $sandboxRootCanonical + [System.IO.Path]::DirectorySeparatorChar` chưa? Đã chặn hoàn toàn xóa nhầm thư mục anh em `%TEMP%\NotHPDynamoBenchmark` và thư mục gốc chưa?
2. Kiểm tra tính đồng bộ song song: `HPDynamo/tools/harness/` và `HPDynamo/tools/benchmark/` có giống hệt nhau 100% không?
3. Kiểm tra Clean Code Governance: file <= 300 dòng, method <= 50 dòng, 0 dead code, 0 unannotated empty catch.

Đưa ra phán quyết rõ ràng: **APPROVE** hoặc **REQUEST_CHANGES** trong `handoff.md` và thông báo qua `send_message`.

## 2026-10-09T19:13:37Z
Sender: 0a4397e4-543d-4476-a901-af2f39506668
Content: Bạn là reviewer_c_iter2_2 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) — Iteration 2 (Remediation).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_iter2_2

Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_iter2_2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2\handoff.md

Nhiệm vụ: Review việc khắc phục trong run-benchmark.ps1 (Remove-BenchmarkSandbox strict prefix check), đồng bộ 100% giữa tools/harness và tools/benchmark, và kiểm tra Clean Code Governance (files <= 300 dòng).
Đưa ra phán quyết rõ ràng: APPROVE hoặc REQUEST_CHANGES trong handoff.md và thông báo qua send_message.
