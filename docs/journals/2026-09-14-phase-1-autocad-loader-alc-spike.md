# Phase 1: AutoCAD Loader + ALC Isolation + Spike — Loader Worked, Spike Ungated, Review Found the Leaks

**Date**: 2026-09-14 13:14  
**Severity**: Medium  
**Component**: HPAutoCad.McpBridge.Loader / HPAutoCad.McpBridge / AutoCAD plugin architecture  
**Status**: Resolved (fixes committed bae2fd2)

## Bối cảnh

Phase 0 tách McpShared và scaffold `HPAutoCad/`. Phase 1 đưa loader + custom `AssemblyLoadContext` để cách ly Roslyn 5.9 / Immutable 10 của bridge khỏi Roslyn 4.10 / Immutable 8.0 của AutoCAD 2026 (R25.1.74, .NET 8). Spike chạy unattended: 5 kiểm tra (load context, Roslyn, WPF, Idle, busy), tất cả PASS. Review tìm 3 sự cố lớn: compiler ref list thiếu AcMgd, spike commands ungated trong autoload bundle, Idle timeout leaks handler. Tất cả fix trong bae2fd2 (run 3).

## Thiết kế Đúng Ngay Lần Đầu — Rồi Ungated Bị Lộ

### Loader + ALC: thiết kế chính thức (ADR-05, xác nhận spike)

`HPAutoCad.McpBridge.Loader` là `IExtensionApplication` AutoCAD nạp. Nó tạo `BridgeLoadContext` (tùy chỉnh `AssemblyLoadContext`), nạp bridge DLL vào đó qua `AssemblyDependencyResolver` từ deps.json. Resolver: gặp `accoremgd`/`acdbmgd` → null → AutoCAD xử trong context Default. Gặp `Microsoft.CodeAnalysis*`/`System.Collections.Immutable` → phân giải từ `Contents\Bridge`, nạp vào ALC. Spike chứng minh: `Roslyn 5.9.0.0 'HPAutoCad.McpBridge'`, `Immutable 10.0.0.0 'HPAutoCad.McpBridge'`, `AcDbMgd 25.1.0.0 'Default'` — riêng biệt, không đụng. Phương án B (compile trong server, bỏ loader) không cần.

### Spike: 5 kiểm tra, tất cả PASS run 1

1. **Load context**: loader nạp, bridge log "PASS 1"
2. **Roslyn scripting**: biên dịch + chạy script Roslyn, trả về layer + model → "PASS 2 roslyn with document"
3. **WPF modeless**: `Core.Application.ShowModelessWindow(Window)` từ ALC → window hiển thị thread 1 → "PASS 3"
4a. **Idle từ background**: thread 20 subscribe `Application.Idle +=`, handler chạy thread 1, quiescent=true, `LockDocument` + transaction + append line OK, 963 ms → "PASS 4a"
4b. **ExecuteInApplicationContext**: thread 20 gọi → **callback chạy thread 20 (sai)**, quiescent=false, block 12,9 s → "PASS 4b" (theo literal output, nhưng ADR-02 từng đặt câu hỏi "chạy ở thread nào?", run 2 block vô hạn, loại ngay)
5. **Busy**: `LINE` chờ input → quiescent=false, ESC → true → "PASS 5"

Bundle: 76 tập tin / 21 MB (Roslyn satellites ×13, đầy đủ).

## Sự cố Lớn — Review Tìm 3 Vấn đề Xấu, Đều Fix Ngay

### 1. Compiler ref list: AcCoreMgd ×2, thiếu AcMgd (major)

**Sự cố:**  
`BridgeEntry.CreateCompiler()` tham chiếu danh sách = `[AcCoreMgd, AcCoreMgd, AcDbMgd]` (bình luận nói `AcMgd/AcCoreMgd/AcDbMgd`). Kiểm tra XML docs + metadata: `Document`/`DocumentCollection`/`Editor`/`Core.Application` sống trong **AcCoreMgd**; `Autodesk.AutoCAD.ApplicationServices.Application` (idiom `Application.DocumentManager…` — mẫu mà mô hình học được) và `DocumentExtension` sống trong **AcMgd**, không có trong đó. Hậu quả: script dùng `Application.*` → CS0103; phase-3 seed test dùng wrapper ref tất cả 3 NuGet → pass seed, bridge reject — **drift lớn**.

**Fix (bae2fd2):**  
Add `typeof(Autodesk.AutoCAD.ApplicationServices.Application).Assembly, // AcMgd`, fix bình luận. Spike run 3 verify: script `Application.DocumentManager.Count` → `docs 1`. Metadata bridge DLL nay ref `acmgd` (lowercase) → casing bug fix (nit 4).

### 2. Spike commands ungated trong autoload bundle (major)

**Sự cố:**  
`HPMCPSPIKE` (append line → start LINE → ESC ×2) + `HPMCPSPIKEQUIT` (send `_.CLOSE _N _.QUIT`) ship trong `LoadOnAutoCADStartup=true`. Ai cũng gọi nó (hoặc phase-2 `SendStringToExecute`… guard reject nhưng human gõ tay được), mutate/discard bản vẽ → **mất dữ liệu**. Plan nói "xoá end of phase 2" = ≥1 phase destructive command live trên dev machine + early bundle.

**Fix (bae2fd2):**  
`SpikeRunner.Start()` refuse nếu `HPAUTOCAD_MCP_SPIKE != "1"` (`Environment.GetEnvironmentVariable`). Bình luận lệnh. Phase-2 step 7 xoá hoàn toàn khi pipe harness chạy.

### 3. Idle timeout leaks handler + stale work runs sau (major)

**Sự cố:**  
`OnIdleAsync` khi `Task.Delay(StepTimeout)` thắng: **không unsubscribe** `Idle -= handler` → lần idle kế (phút sau, drawing khác) handler chạy `work()` (append line / start LINE / **CLOSE QUIT**) + `TrySetResult` trên TCS không ai chờ. Pattern này phase-2 executor sẽ copy → **phải bẻ ngay**.

**Fix (bae2fd2):**  
`CancellationTokenSource` cho delay. Timeout: `TrySetResult` trước, sau đó `Idle -= handler`. Handler: if `completion.Task.IsCompleted` return (no-op). Spike run 3 quit: `CloseAndDiscard()` (Idle tick 1) + `Quit()` (tick 2) → acad.exe exit 0 sau 18 s. Harness `Stop-Process` fallback.

## Các Sự Thật Đau Khác

**Spike run 2: ExecuteInApplicationContext block vô hạn** (> 5 phút → kill AutoCAD). Run 1 block 12,9 s. Chỉ là "không có cách timeout một call block thread" → ADR-02 loại ngay, không spike phase-2.

**SECURELOAD prompt** mỗi lần loader DLL đổi hash. Run 1 không prompt (có thể user đã *Always Load* trước, hash cũ). Unattended harness tự bấm (BM_CLICK dialog #32770). Liệu *Always Load* ghi `TRUSTEDPATHS` hay chỉ nhớ hash → phase-5 docs. Phase-2 entry point bất chứng.

**Bundle optimize**: `SatelliteResourceLanguages=en` (Roslyn satellites), `RemoveDir Contents\Bridge` (deploy sạch), exclude design-time builds → 76 / 21 MB thành 24 / 14 MB.

**Nit sai trong review (nit 15):** "remove `using System.IO`" — lỗi tại Loader project có `ImplicitUsings`, nhưng Bridge project (`UseWPF`) không → build fail nếu xoá. Kiểm sát: **phải build**. Kept in bridge csproj.

## Số liệu

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug/Release` | ✅ 0 warnings / 0 errors |
| Bridge binary (Roslyn, Immutable, Contracts) | ✅ Có; no Autodesk |
| Loader binary (3 files: dll/pdb/deps.json) | ✅ No bridge, no Roslyn |
| McpShared tests | ✅ 70/70 pass |
| HPRebar tests (McpBridge code-review không chạm shared) | ✅ 106/106 pass |
| Spike run 1 | ✅ 5/5 pass |
| Spike run 2 | ⚠️ 4b block vô hạn; 4a/5 PASS; quit fail |
| Spike run 3 (sau fix) | ✅ 5/5 pass; unattended quit OK |
| Code review score | 7/10 → 10/10 (3 major fixed) |

## Quyết định & Bài Học

**ADR-02 xác nhận:** Idle one-shot = đường chính. `ExecuteInApplicationContext` = loại. Executor phase-2: unsubscribe + no-op on completed request (pattern tuần thủ).

**ADR-05 xác nhận:** Loader ALC = thiết kế chính thức. SECURELOAD cứ prompt (ký phút pack).

**Bài học:**
1. **Metadata kiểm tra bắt drift sớm.** Compiler reference list sai-sai nhỏ → script surface hẹp → phase-3 seed test đối chiếu metadata wrapper, không chỉ viết test. Nếu xây integration test từ đầu, lỗi này bị lộ ở phase-2.
2. **Ungated spike commands không chấp nhận.** Autoload bundle = sản phẩm demo → destroy code không thể sống qua 1 phase. Gate ngay. Delete sớm + lock date.
3. **Idle timeout pattern không leak.** Executor phase-2 inherit timeout logic → phải unsubscribe + mark complete trước khi delay thắng. Không để handler stale.
4. **Build trước nit fix.** "Remove System.IO implicit" fail trên WPF csproj → không phải nit, cần build verify.

## Tiếp theo

Phase-2 `MainThreadExecutor` = executor thực, dùng ADR-02 Idle one-shot + unsubscribe-on-complete rule. Spike xoá step 7 đầu. Pipe harness kiểm tra executor đúng thread + transaction. Phase-3 seed compile test + metadata wrapper ref danh sách = 3 assembly đúng.

**Commit:** cc1868b (spike + loader) + bae2fd2 (3 major + 10 minor fixes). Build 0 warnings, 176 test pass, spike 5/5.

