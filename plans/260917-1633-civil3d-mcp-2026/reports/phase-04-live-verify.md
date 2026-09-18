# Phase 4 — live-verify end-to-end trên Civil 3D 2026 (2026-09-18)

Harness `HPCivil3d/tools/harness/run-live-verify.ps1` (Windows PowerShell 5.1) + `live-verify.py` (một phiên stdio qua `McpShared/tools/mcp-session.py`) trên **exe publish** `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe`, registry root cách ly `HPCivil3d/output/live-verify/registry` (xoá và cài seed lại mỗi run), scene = copy `Profile-5F.dwg` (Feet) + `Corridor-1a.dwg` (Meters) từ tutorial. Log: `HPCivil3d/output/live-verify/live-verify-run{1,2,3,4}.log` (run 3 = `-Runs 3 -IncludeIsolation`; run 4 = harness sau review, `-Runs 1 -IncludeIsolation`).

## Ma trận (run 3: 76 check tự động mỗi run = disabled 1 + main 72 + nodoc 1 + busy 2; run 4 sau review: 80 = mỗi phiên Python thêm 1 check "live registry roots untouched", main thêm check root AutoCAD cách ly)
| Nhóm | Số | Nội dung (mọi check đều PASS ở run 3 ×3) |
|---|---|---|
| A | 4 | context `civil3d` block trên `Profile-5F` (Feet, 8 alignments, 2 405 COGO), `units.length` = Civil unit, resource `civil3d://document/info` = cùng snapshot, `inspect_type Autodesk.Civil.DatabaseServices.Alignment` (filter Station, 28 member) |
| E | 20 | read · dryRun · commit + `U` sau `REGEN` · exception rollback · `none`+modify · `manual` ≡ auto · guard ×7 (`RebuildAll`, `DataShortcuts.SetWorkingFolder`, `ExportToDEM`, `ed.GetPoint`, `tr.Commit`, `StartTransaction`, `System.IO.File`) · compile · `cancel_execution` đua · timeout 5 s · logs/args/serializer (Point3d + Alignment `{handle,type,name}`) · `PointNotOnEntityException: Point Outside Surface.` · audit |
| E' | 4 | opt-in off `-32001` (text Civil 3D) · no-doc `-32003` · busy `-32002` sau 8.1 s · ESC (PostMessage) → retry |
| S | 24 | 12 seed: R1 counts; R2 8 alignments; R3 entities + samples theo tên, **paging `entityLimit 1/offset 1`**, unknown → `ArgumentException`; R4 14 profiles; R5 EG 2 405 điểm; R6 tâm/ngoài; R7 corridor; R8 **`partLimit 2 → partsTruncated`** + full parts (slope, mm); R10 limit/offset theo số, **range 10–12 tra trực tiếp**, group lạ → `ArgumentException`; W1 `test_tool` 2/2 → `run_tool` thật 2 405→2 406 → `U` → 2 405, tên trùng batch → `ArgumentException`; W2 polyline → dryRun 8→8 → `run_tool` 8→9 → `list_alignments` thấy → `U` → 8, layer lạ → `ArgumentException`; R9 trên `Corridor-1a` 10/52 lot m², `truncated`; units Feet vs Meters đúng từng drawing |
| R | 21 | MISS → ad-hoc (766 GRND) → `get_run` literal → `toolify_run` (Civil 3D) → `propose_tool mcp_verify_count_cogo_points` (`host civil3d`, category `Point`; mọi tool harness tạo đều mang tiền tố `mcp_verify_` để `--reset-verify-tools` không bao giờ xoá tool của người dùng) → `test_tool` 2/2 → `publish_tool` pending + review nêu `HPCivil3d.Mcp.Server.exe registry approve` → `run_tool` từ chối pending → CLI approve → hiện sau 0.5 s (`list_changed` 1) → search HIT → gọi = 766 → `get_tool`; fragile `mcp_verify_alignment_length` → 5 fail `InvalidOperationException` → quarantined → `run_tool` từ chối → `restore` + `newVersion` (ArgumentException) → re-approve → 5 lỗi caller vẫn published; `propose_tool` với `RebuildAll` → guard từ chối; tool `none` mà ghi → `test_tool` 0/2 fail |
| X | 3 | exe AutoCAD bên cạnh **trên root cách ly `output/live-verify/registry-autocad`** (50 seed cài vào đó): 62 tool tên AutoCAD, `get_autocad_context` báo bridge AutoCAD chưa nối (pipe `hpautocad-mcp-2026`, không chạm bridge Civil), seed nằm trong root cách ly; + 1 check cuối phiên: hash `%AppData%\HPAutoCad\McpServer` và `%AppData%\HPCivil3d\McpServer` trước = sau |

## Kết quả
| Run | Kết quả |
|---|---|
| run 1 (harness bản đầu) | 36/40 main — 4 FAIL harness: `inspect_type` cần `memberFilter`; check "context < 1.5 s khi script chạy" **sai với host main-thread** (context xếp sau script, 15 s — bỏ, ghi README); `list_pipe_networks` → lỗi seed **`args.Int("partLimit", 200)`** vs schema default 60 (test mới `Schema_defaults_equal_the_code_fallbacks` bắt); X needle |
| run 2 (`-IncludeIsolation`) | 64/72 main (lý do trên + fragile tool example "East-West Road" không tồn tại → 1/2 test → chuỗi R fail; sửa lấy 2 tên alignment thật); isolation 5/6 — I-2 FAIL vì **`Test-Path \\.\pipe\x` kết nối vào pipe** → bridge nhận/nhả client → tên biến mất thoáng qua → `Test-PipeUp` = liệt kê thư mục pipe |
| run 3 (`-Runs 3 -IncludeIsolation`, 13:2x–14:1x) | **3 × 76/76** (disabled 1 + main 72 + nodoc 1 + busy 2, 0 skip) + isolation **8/8** → wrapper 20 steps, 0 failed, exit 0; Civil quit graceful mỗi run, không process mồ côi |
| run 4 (harness sau review `code-review-phase-04.md`, `-Runs 1 -IncludeIsolation`, 14:17–14:2x) | **80/80** (disabled 2 + main 73 + nodoc 2 + busy 3) + isolation **9/9** (thêm baseline "no AutoCAD pipe") → wrapper 13 steps, 0 failed, 0 skipped, exit 0; `%AppData%\HPAutoCad\McpServeregistry.db` mtime 13:36:18 và `%AppData%\HPCivil3d\McpServeregistry.db` 10:06 **không đổi** (hash trước = sau, in trong log); 50 seed AutoCAD cài vào `output/live-verify/registry-autocad`; audit 807 → 834 dòng trong E; root run vẫn còn `Point/mcp_verify_count_cogo_points` + `Alignment/mcp_verify_alignment_length` sau isolation; không acad.exe mồ côi |

## Isolation hai chiều (run 2/3)
| # | Kết quả |
|---|---|
| I-1 Civil 3D thứ hai | **PASS** — cửa sổ bridge "Pipe hpcivil3d-mcp-2026 is already in use - another Civil 3D 2026 instance is serving MCP." sau 40 s; log dùng chung có `could not create pipe`; bridge đầu vẫn trả context |
| I-2 AutoCAD 2026 thuần (+ `bridge.scr` AutoCAD) | **PASS** run 3 — pipe `hpautocad-mcp-2026` lên sau 30 s, Civil loader.log 542 → 542; **coexist**: `get_autocad_context` Inches + `get_civil3d_context` Meters cùng lúc (run 4: server AutoCAD của bước này chạy trên root cách ly `registry-autocad`; thêm baseline "chưa có pipe AutoCAD" trước khi start) |
| I-3 Advance Steel 2026 | **PASS** — cửa sổ lên, Civil loader.log không tăng, không pipe AutoCAD |
| I-4 harness AutoCAD `-OnlyIsolation` | **PASS 4/4** — AutoCAD thứ hai "in use", Civil 3D không nạp bundle AutoCAD (log `output/live-verify/autocad-only-isolation.log`) |

## X — hồi quy
- `reports/regression-tools-list-phase-04.py` (4 host, Release build mới): **Revit 33, AutoCAD 62, Navis 24, ETABS 24 byte-identical** với phase-0 after, `KNOWN_CHANGED` rỗng.
- Suite: Core.Tests 206 · Net48 71 · HPRebar.Mcp.Server.Tests 109 · HPAutoCad.Mcp.Server.Tests 280 · HPAutoCad.Aec.Tests 225 · HPNavis.Mcp.Server.Tests 49 · HPEtabs.Mcp.Server.Tests 81 · HPCivil3d.McpBridge.Tests 55 · HPCivil3d.Mcp.Server.Tests **106** — 0 fail.

## M — manual (chưa làm, ghi cho phase 5 known gaps)
- Dialog modal Civil (Panorama/Toolspace) khi script chờ → `-32002` — chưa kích bằng harness (không SendKeys vào Civil ngoài ESC).
- Corridor dự án thật: `list_corridors` + `Rebuild` bị chặn — chỉ trên corridor tutorial.
- Drawing từ data shortcut project → `list_*` đọc reference object — chưa có project DREF.

## Kết luận
Sau run 3 ×3 sạch và run 4 (harness sau review) sạch: ADR-02/03/04 → **Accepted (verified live)**; `Verified` cho bridge + server + 12 seed + registry loop trên Civil 3D 2026 máy dev.
