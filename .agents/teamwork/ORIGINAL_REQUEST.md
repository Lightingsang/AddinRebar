# Original User Request

## 2026-09-27T15:57:37Z

Implement the **Kata Rebar** feature in the `HPRebar` ecosystem, enabling automated generation of 3D concrete beam reinforcement in Revit 2026 based on structural calculation and detailing data read from sheet `Dam` of `Kata.xlsm` (via active COM or file fallback).

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Integrity mode: development

## Requirements

### R1. Kata Dam Sheet Data Parser (`HPRebar.Core`)
Parse structural and reinforcement data from sheet `Dam` of `Kata.xlsm` into strongly-typed DTO models (`KataBeamRebarSpec`):
- Beam header parameters: Name (`B3`), count (`B4`), dimensions $b \times h$ (`B5:B6`), slab thickness (`B7`), level (`B10`), anchorage length multipliers (`G2:G3`, `H3`, `H5`).
- Continuous top and bottom main bars (`B11`, `B12`, e.g., `2f18`, `3f20`).
- Additional span and support bars (top extra layers in rows 13–16 across support columns; bottom extra layers in rows 17–18 across spans).
- Side bars / torsion bars / web skin reinforcement (`E5`, `G4`, row 20).
- Stirrups: Bar diameter (`G6`), spacing near supports (`G7`, e.g., `a150`), spacing at mid-span (`G8`, e.g., `a200`), stirrup types from rows 25–27 (closed stirrup □, cap stirrup U, cross tie C).
- Provide dual data source capability: primary reading from active Excel via COM (`oleaut32`), with graceful fallback to ClosedXML reading `.xlsm` from disk.

### R2. Rebar Geometry & Distribution Calculator (`HPRebar.Core`)
Convert `KataBeamRebarSpec` into explicit 3D rebar curves, dimensions, and distribution arrays within the beam coordinate system:
- Calculate top and bottom continuous bar lengths with anchorage into end supports (hook 90° or lap length based on $d$).
- Calculate support top additional bar cutoffs based on span ratios ($L/4$, $L/3$, or sheet `Dam` settings in `H3:H5`).
- Calculate mid-span bottom additional bar start/end cutoffs relative to clear span faces.
- Segment stirrup distribution along each span (support dense zones $L/4$, mid-span sparse zones $L/2$).
- Calculate concrete cover offsets and side-bar positions.
- All core calculations must reside in `HPRebar.Core` (`netstandard2.0`) with 0 references to `Autodesk.Revit.*`.

### R3. Revit 3D Rebar Generation & Idempotent Update (`HPRebar`)
In Revit 2026 (.NET 8):
- Match beam selection by user on Revit plan with beam data in sheet `Dam` by name (`B3`) and span count.
- Smart RebarBarType resolution: Resolve bar designations (e.g., `f18`, `d8`) to project `RebarBarType` by numerical diameter ($\approx 18\text{ mm} \pm 0.5\text{ mm}$) and standard `RebarHookType`.
- Create native `Rebar` elements inside the host beam using Revit API (`Rebar.CreateFromCurves` / `Rebar.CreateFromCurvesAndShape`).
- Idempotency & Cleanup: Tag all generated rebars with an identifier parameter (e.g., `Comments` = `"HPRebar_Kata_{BeamName}"`). On re-run, automatically locate and delete previous Kata rebars for that beam run before regenerating to prevent duplication.

### R4. User Interface & Ribbon Integration (`HPRebar`)
- Add a "Kata Rebar" button to the existing HPRebar ribbon panel (adjacent to "Kata Export").
- Modeless/Modal WPF Dialog matching the repo's ThemeDark/ThemeLight dynamic styling and MaterialDesign 5.3.2 tokens.
- Dialog previews the parsed beam specification (spans, top/bottom bars, stirrups), verifies matching host beam, displays mapped `RebarBarType`s, and provides an action button to execute rebar creation inside a Revit `Transaction`.

## Acceptance Criteria

### Unit Testing & Core Verification (No Revit Required)
- [ ] Comprehensive xUnit test suite in `HPRebar.Core.Tests` verifies `KataDamSheetReader`: parses sample sheet `Dam` rows (continuous bars, additional top/bottom bars, stirrups, skin bars).
- [ ] xUnit test suite verifies `KataRebarCalculator`: calculates bar cutoffs, anchorage hooks, and stirrup distributions for single-span, multi-span continuous, and cantilever beams.
- [ ] `dotnet test HPRebar.Core.Tests` passes with 100% green tests.

### Build & Integration Verification
- [ ] `dotnet build HPRebar.slnx -c Debug.R26` compiles cleanly with 0 errors.
- [ ] No circular dependencies or forbidden references (`HPRebar.Core` must remain pure `netstandard2.0` with no Revit references).

### End-to-End Revit Workflow
- [ ] Reading active Excel `Kata.xlsm` sheet `Dam` populates the preview UI with beam dimensions and rebar layout.
- [ ] Executing "Tạo thép / Generate Rebar" creates actual 3D `Rebar` instances in the Revit host beam with correct diameter, layout rules, and covers.
- [ ] Running the command a second time on the same beam cleanly replaces prior Kata rebar without duplicates.

## 2026-10-05T23:21:03Z

Xây dựng Deliverable độc lập cấp cao HPBim/ viết 100% bằng C# .NET với solution HPBim.slnx, tuân thủ nghiêm ngặt quy định HP Clean Code Governance của kho mã nguồn 01_AddinRebar (chỉ tham chiếu McpShared/, không tham chiếu chéo). Hệ thống kế thừa và chuyển đổi logic hình học, chuẩn TCVN và quy trình soát lỗi từ vn-autocad-skill (Python -> C#) để tự động sinh mô hình BIM 3D bản địa (Kiến trúc, Kết cấu, MEP) trực tiếp trong Revit 2026 thông qua hạ tầng Revit MCP Named Pipe hiện có, điều khiển bởi CLI Tool và bộ Agent AI chuyên môn.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

Tham chiếu:
- Quy chuẩn Clean Code bắt buộc: docs/clean-code/HP_CLEAN_CODE_CORE.md, docs/architecture/ARCHITECTURE.md, docs/clean-code/host-appendix/revit.md
- Logic thiết kế & hình học TCVN tham khảo: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\vn-autocad-skill-main (Python vncad -> port sang C#)
- Nền tảng chia sẻ: McpShared/ (Contracts, Core, Roslyn compiler, Named Pipe)
- Hạ tầng Revit MCP hiện hữu: Pipe \\.\pipe\hprebar-mcp-r2026 của HPRebar.McpBridge
- Benchmark Ground-Truth: Bộ dữ liệu nha_pho_5x18m (port sang C# test fixture NhaPho5x18mFixture.cs)

## Requirements

### R1. Cấu Trúc Solution & Domain C# Thuần (HPBim.Core)
- Khởi tạo solution HPBim/HPBim.slnx, global.json, Directory.Build.props tuân thủ tiêu chuẩn repo.
- Xây dựng thư viện C# thuần HPBim.Core (target netstandard2.0, tuyệt đối không tham chiếu Autodesk.Revit.*):
  - Chuyển đổi toàn bộ cấu trúc dữ liệu hình học từ vncad sang C#: AxisGrid, LevelDef, WallDef, ColumnDef, BeamDef, SlabDef, OpeningDef, RoomDef, MepFixtureDef, PipeRunDef, RebarDef.
  - Triển khai logic tính toán không gian, bù trừ offset, định vị cửa, tính toán diện tích phòng khép kín, và quy tắc cấu tạo cốt thép TCVN thuần C#.
  - Xây dựng QC Engine thuần C# kiểm tra va chạm (clash), quy chuẩn khoảng cách và quy chuẩn TCVN trên dữ liệu trước khi gửi sang Revit.

### R2. Bộ Điều Phối & Client Giao Tiếp Revit (HPBim.Client)
- Xây dựng thư viện HPBim.Client (target net8.0 / net10.0, tham chiếu HPBim.Core và McpShared):
  - Kết nối trực tiếp tới Named Pipe \\.\pipe\hprebar-mcp-r2026 của HPRebar.McpBridge đang chạy trong Revit 2026.
  - Quản lý quy trình dựng hình theo trật tự chuẩn BIM: Levels -> Grids -> Columns/Beams/Floors -> Walls -> Doors/Windows -> Rooms -> MEP.
  - Tự động kiểm tra Family/Type trong Document; nếu thiếu Type (như Cột 220x220, Tường 110), Client phát lệnh sinh/nhân bản Type và gán đúng kích thước thông qua script Revit API.
  - Hỗ trợ chế độ chạy thử nghiệm an toàn (dryRun: true rollback TransactionGroup).

### R3. Ứng Dụng Dòng Lệnh HPBim CLI & Bộ Tác Tử AI (HPBim.Cli & Agents)
- Xây dựng console executable HPBim.Cli (net10.0) cung cấp các lệnh:
  - validate --input <model.json>: Soát lỗi hình học, clash và kiểm tra hợp chuẩn TCVN offline.
  - generate --input <model.json> [--dry-run]: Kết nối tới Revit pipe và dispatch tạo dựng mô hình.
  - verify --input <model.json>: Truy vấn mô hình Revit và đối soát số lượng, tọa độ với schema.
  - estimate --input <model.json>: Xuất bảng bóc tách khối lượng (BOQ).
- Đóng gói bộ hướng dẫn/vai trò Agent (Kiến trúc, Kết cấu, MEP, Soát lỗi, Điều phối) tương thích với Antigravity và Claude Code để AI tự chủ thiết kế và thực thi qua CLI.

### R4. Kịch Bản Kiểm Thử & Xác Thực Khách Quan (xUnit / MTP Tests)
- HPBim.Core.Tests: Bộ Unit Test xUnit v3 / MTP độc lập (chạy trong vài giây không cần Revit):
  - Chuyển đổi trọn bộ dữ liệu nha_pho_5x18m thành NhaPho5x18mFixture.cs.
  - Test kiểm tra tính toán hình học, giải thuật phân chia không gian, hàm lượng thép TCVN và bộ soát lỗi QC.
- HPBim.Client.Tests: Bộ test tích hợp kiểm tra kết nối Named Pipe và round-trip đối soát với Revit 2026.

## Acceptance Criteria

### Tuân Thủ Clean Code & Kiến Trúc
- [ ] 100% mã nguồn mới được viết bằng C# (không sử dụng runtime Python).
- [ ] Tuân thủ triệt để HP_CLEAN_CODE_CORE.md: Phương thức <= 50 dòng, lớp <= 300 dòng, không catch exception rỗng không có lý do giải thích, không code thừa.
- [ ] Không có tham chiếu chéo giữa các MCP folders (HPBim chỉ tham chiếu McpShared/).
- [ ] HPBim.Core hoàn toàn không tham chiếu Autodesk.Revit.* và chạy được trên mọi môi trường .NET.

### Độ Chính Xác Benchmark NhaPho5x18m
- [ ] Toàn bộ 100% Unit test trong HPBim.Core.Tests chạy PASS qua lệnh dotnet test.
- [ ] Lệnh hpbim generate --input NhaPho_5x18m.json dựng thành công trong Revit 2026:
  - 5 Levels đúng cao độ lý thuyết (+0.150m, +3.800m, +7.200m, +10.600m, +13.400m).
  - Hệ trục Grids X1-X5, YA-YB.
  - 10 Cột BTCT 220x220 tại các giao điểm trục.
  - Toàn bộ tường bao 220, tường ngăn 110, tự động host chính xác cửa đi và cửa sổ.
  - Toàn bộ các phòng (Rooms) khép kín và tính đúng diện tích.
  - Hệ dầm và bản sàn các tầng.
  - Thiết bị vệ sinh và đường ống cấp thoát nước cơ bản.
- [ ] Lệnh hpbim verify trả về kết quả khớp 100% số lượng element thực tế trong Revit so với thiết kế.
- [ ] Chế độ --dry-run rollback sạch sẽ 100% không làm thay đổi trạng thái Document.

## Follow-up — 2026-10-06T00:48:56Z

The server was restarted. The user instructed to continue ('tiep tuc'). Resume orchestration for the HPBim deliverable. Generation 1 completed M1 Iteration 2 with a soft handoff documented in `.agents/orchestrator_10/handoff.md`. Spawn or resume the Orchestrator (Generation 2) to fix the M1 audit finding in QcEngine, complete M1 verification, and execute Milestones M2, M3, M4, and M5 to completion.


## 2026-10-07T02:05:11Z

Tái cấu trúc và thiết lập lại toàn diện Đội ngũ Multi-Agent Kết Cấu & BIM (Kỹ sư Chủ trì Điều phối, Kỹ sư Kết cấu CAD, Kỹ sư BIM Revit, Kỹ sư QA/QC Độc lập) cùng hạ tầng Sổ cái Ledger và bộ công cụ CLI C# Fail-Fast, hiện thực hóa triệt để Nguyên Tắc Vàng: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

Tài liệu quy chuẩn gốc bắt buộc tuân thủ:
- Quy chế phối hợp chuẩn Kỹ sư Kết cấu: C:\Users\STR-HP03\.gemini\antigravity\brain\087c0df3-fff1-40c1-83ce-c0d4c6cb6acd\quy_che_phoi_hop_cad_revit_chuan_ky_su_ket_cau.md
- Quy chuẩn Clean Code kho mã nguồn: docs/clean-code/HP_CLEAN_CODE_CORE.md
- Hạ tầng kết nối trực tiếp: Named Pipe AutoCAD (\\.\pipe\hpautocad-mcp-2026) và Named Pipe Revit (\\.\pipe\hprebar-mcp-r2026).

## Requirements

### R1. Tái Lập Hệ Thống Hồ Sơ & Quyền Hạn Bộ Tứ Agent
- Kỹ sư Chủ trì Điều phối (coordinator_assistant): Quản trị vòng đời 7 bước của cấu kiện, điều phối tuần tự theo phân đoạn (Phần Móng -> Tầng thân), quản lý Sổ cái state/bim_coordination_ledger.json, cơ chế Đóng băng phân đoạn (Freeze Zone) và phát hành Phiếu yêu cầu làm rõ (RFI) khi có mâu thuẫn.
- Kỹ sư Kết cấu CAD (cad_assistant): Rà soát bản vẽ theo 2 giai đoạn (Khung trục/cao độ -> Cấu kiện), đối soát chéo 6 nguồn (Mặt bằng, Mặt cắt, Chi tiết, Thống kê, Ghi chú, Tầng liên đới), phân loại 3 nhóm (Nhóm 1: Đủ thông tin; Nhóm 2: Mâu thuẫn; Nhóm 3: Thiếu thông tin). Chỉ chuyển giao Nhóm 1 sang Revit.
- Kỹ sư BIM Revit (revit_assistant): Chỉ dựng cấu kiện Nhóm 1 có dữ liệu duy nhất; cơ chế tự kiểm tra 2 pha tự động (Pha 1: Tạo đối tượng -> Pha 2: Đo lại ngay b, h, z, vị trí; sai số > 1.0mm rollback); không tự đoán theo kinh nghiệm; chỉ báo cáo "Đã dựng xong theo dữ liệu được giao và đã kiểm tra lại các thông số cơ bản".
- Kỹ sư QA/QC Độc lập (qaqc_assistant): Kiểm toán độc lập 8 bước, không mặc định CAD hay Revit đúng; 3 trạng thái kết luận bắt buộc (Đúng / Sai, cần sửa / Chưa đủ cơ sở để kết luận); định tuyến lỗi tự động (Lỗi CAD đọc sai -> CAD sửa; Lỗi Revit dựng sai -> Revit sửa).
- Cả 4 Agent đều khắc cốt Nguyên Tắc Vàng tại ĐIỀU 0 của System Prompt.

### R2. Thiết Lập Sổ Cái Điều Phối & Cơ Chế Fail-Fast Bằng Code C#
- Khởi tạo Sổ cái điều phối trung tâm state/bim_coordination_ledger.json ghi nhận chính xác 7 bước vòng đời từng cấu kiện: Đọc bản vẽ -> Kiểm tra thông tin -> Dựng Revit -> Kiểm tra độc lập -> Sửa lỗi -> Kiểm tra lại -> Xác nhận đạt.
- Cài đặt cơ chế kiểm soát kỹ thuật Fail-Fast Gate trong HPBim.Cli: Triệt tiêu hoàn toàn các logic tự ghép nét (line-pairing) hoặc đoán mò; nếu thiếu bất kỳ trường bắt buộc nào (cao độ, tiết diện, điểm tựa), ném ngoại lệ dừng ngay đối tượng đó và đưa vào danh sách "Chưa đủ cơ sở để kết luận".

### R3. Thực Thi Dứt Điểm Phân Đoạn Phần Móng (Cọc, Đài Móng, Giằng Móng)
- Thực thi theo thứ tự tuần tự nghiêm ngặt:
  1. Làm sạch mô hình: Purge sạch 100% 211 dầm ma hiện có trong Revit, bảo tồn nguyên vẹn 27 Grids và 10 Levels.
  2. Hạ cao độ cọc: Điều chỉnh 196 cọc D400 ngàm 100mm vào đáy đài tương ứng (COTE -2.500m cho ĐC-1..3', -2.700m cho ĐC-4..5, -2.800m cho ĐC-6).
  3. Trích xuất & Bàn giao Nhóm 1: Agent CAD trích xuất đường bao 72 đài móng từ block mm (đỉnh -1.600m) và 22 loại giằng móng GM-1..22 (đỉnh -1.600m).
  4. Dựng mô hình Revit: Agent Revit dựng đài móng và giằng móng với cơ chế tự kiểm tra 2 pha.
  5. Kiểm toán độc lập: Agent QA/QC chạy quy trình 8 bước đối soát trực tiếp DWG vs Revit, xuất Biên bản nghiệm thu chính thức Markdown và file kết quả JSON.

## Acceptance Criteria

### Tuân Thủ Nguyên Tắc Vàng & Tiêu Chuẩn Kỹ Thuật
- [ ] 100% hồ sơ 4 Agent và tài liệu quy chế khắc cốt Nguyên Tắc Vàng tại ĐIỀU 0: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."
- [ ] 211 dầm ma trong Revit bị xóa sạch 100%, số lượng dầm Structural Framing về đúng 0 trước khi dựng dầm mới.
- [ ] 27 trục và 10 cao trình trong Revit được bảo toàn nguyên vẹn 100%.
- [ ] 196 cọc D400 có cao độ đầu cọc khớp chính xác đáy đài móng, sai số tim cọc <= 2.0mm.
- [ ] 72 đài móng và hệ giằng móng GM-1 đến GM-22 được dựng đúng COTE đỉnh -1.600m, sai số tiết diện b x h = 0.0mm, sai số cao độ/hình học <= 1.0mm.
- [ ] Mọi cấu kiện thiếu thông tin phải được đưa vào danh sách "Chưa đủ cơ sở để kết luận" kèm tọa độ và lý do, công khai minh bạch, cấm tự dựng mò.
- [ ] Agent QA/QC hoàn tất kiểm toán 8 bước và xác nhận nghiệm thu ĐẠT 100% cho phân đoạn Phần Móng.

## Follow-up — 2026-10-07T06:10:58Z

Hệ thống vừa restart, vui lòng kiểm tra trạng thái của Orchestrator 12 và worker_m3_foundation, tiếp tục hoàn tất Milestone M3 (dựng 72 đài móng, 22 loại giằng móng GM-1..22 và chạy kiểm toán QA/QC độc lập 8 bước).


## 2026-10-07T13:49:28Z

Triển khai hoàn thiện quy trình BIM Pipeline và Coordination Ledger cho HPBim (P0 và P1) tại G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim: bảo đảm kết quả trung thực, preview và apply đồng nhất 100%, có khả năng tự phục hồi và tiếp tục an toàn sau lỗi hoặc đứt kết nối mà không tạo trùng lặp hay báo thành công sai.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

## Requirements

### R1. P0 — Kết quả trung thực & Preview/Apply tương đương
- **Phân định Artifact & Schema Validation**: Kiểm tra hợp chuẩn schema, version, required fields và dependencies; từ chối dứt khoát raw CAD JSON hoặc file sai định dạng, không deserialize thành BimModel rỗng rồi báo PASS.
- **Single Source of Truth Execution Plan**: Toàn bộ logic giải quyết tài nguyên (level, family symbol, tọa độ, transform) và ranh giới transaction được thống nhất thành một Execution Plan (IR) duy nhất dùng chung cho cả Preview (Dry-Run với rollback) và Apply (Commit). Loại bỏ các script phân tán trong CLI commands.
- **Báo cáo chi tiết từng cấu kiện (Per-Element Granularity)**: Mọi cấu kiện trong plan phải trả về trạng thái rõ ràng: Created, Updated, NoOp, Skipped hoặc Failed kèm ID và lý do chi tiết. Cấm fallback ngầm (chọn type đầu tiên, bỏ qua lỗi).
- **Rollback Verification & Status Honesty**: Dry-run chỉ được báo thành công khi mã thực thi không có lỗi và Revit bridge xác nhận transaction đã được rollback 100%. Phản ánh trung thực partial commit và outcome unknown; thu thập warning từ Revit.
- **Loại bỏ Metric/PASS Literal chưa đo**: Mọi đánh giá QA/QC phải dựa trên dữ liệu đo đạc thực nghiệm. Nếu thiếu nguồn dữ liệu đối chiếu hoặc thiếu thông số đo, phải kết luận Inconclusive, cấm tự động nghiệm thu.

### R2. P1 — Phục hồi & Idempotency qua các phiên
- **Document & Plan Identity**: Quản lý định danh tài liệu Revit (Document Title, Path, ActiveView), snapshot revision/hash và plan hash. Kiểm tra tính hợp lệ và độ mới của tài liệu trên Revit thread trước khi chạy mỗi batch.
- **Composite Source Key & Provenance Mapping**: Định danh cấu kiện bằng Composite Source Key `[DocIdentifier]#[Category]:[PrimaryHandle]` kèm danh sách ProvenanceHandles thành phần. Thiết lập bảng ánh xạ SourceKey → Revit UniqueId. Thực thi Idempotency: chạy lại cùng plan không sinh cấu kiện trùng lặp.
- **Intent Journal & Revit Extensible Storage Receipt**: Ghi Operation Intent vào Journal cục bộ trước khi gửi qua pipe. Trong Revit, lưu Operation ID và Element Mapping nguyên tử cùng transaction dựng hình vào Revit Extensible Storage (DataStorage element ẩn).
- **Phân tách Timeout & Tự động Reconcile**: Tách biệt transport timeout, queue deadline và execution budget. Khi gặp timeout sau khi gửi hoặc đứt kết nối, đánh dấu `OutcomeUnknown`. Khi kết nối lại, tự động đọc Extensible Storage trong Revit để xác nhận commit status và hoàn tất mapping mà không gửi lệnh mù.
- **Kiểm toán hình học độc lập (Measured Geometric Verification)**: Kiểm toán sau commit bằng cách đo trực tiếp từ Revit API (tâm, cao độ đỉnh/đáy, kích thước tiết diện từ Solid/Curve/Parameters) so sánh với BimExecutionPlan trong phạm vi CoordinationTolerances; không dùng số lượng cấu kiện (counts) để nghiệm thu.

### R3. P2 — Lập đề xuất kiến trúc (Design Proposal Only)
- Soạn thảo tài liệu đề xuất kiến trúc (Design Proposal) cho các tính năng nâng cao trong tương lai: change tracking hai chiều, hàng đợi xử lý nền (background queue), và batching quy mô lớn.

## Acceptance Criteria

### Verification & Testing
- [ ] Toàn bộ 370 unit/integration tests hiện hữu (268 Core + 102 Client) tiếp tục PASS 100% (zero regressions).
- [ ] Bộ unit test mới kiểm thử Schema Validation: Chặn đứng raw CAD JSON hoặc schema không hợp lệ, không để lọt model rỗng báo PASS.
- [ ] Bộ test mới kiểm thử Single Execution Plan: Chứng minh Preview và Apply thực thi cùng một plan logic, chỉ khác cờ rollback/commit.
- [ ] Bộ test mới kiểm thử Dry-Run: Chứng minh Dry-Run báo FAIL nếu có lỗi execution hoặc không có xác nhận rollback từ bridge.
- [ ] Bộ test mới kiểm thử Idempotency: Chạy plan lần đầu sinh cấu kiện; chạy lại lần 2 nhận diện trạng thái NoOp/Update, không tạo trùng element trong RVT.
- [ ] Bộ test mới kiểm thử Reconcile & Recovery: Mô phỏng đứt kết nối/timeout sau commit; cơ chế Reconcile tự động phát hiện receipt trong Extensible Storage và cập nhật ledger chính xác.
- [ ] Bộ test mới kiểm thử Geometric Verification: Cấu kiện sai lệch kích thước hoặc cao độ vượt quá `CoordinationTolerances` bị đánh dấu Defect/Failed ngay cả khi số lượng cấu kiện (counts) khớp 100%.

### Repository & Governance Rules
- [ ] Không tạo dependency hai chiều giữa `HPAutoCad` và `HPRebar`.
- [ ] Giữ nguyên `FailFastGate` và phương châm: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng".
- [ ] Tuân thủ triệt để Clean Code standards của repository (`HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`).
- [ ] Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.

## Follow-up — 2026-10-07T14:39:43Z

Server vừa khởi động lại. Người dùng yêu cầu tiếp tục ("tiep tuc"). Vui lòng kiểm tra trạng thái hiện tại của orchestrator, các track và worker, hồi sinh/khởi động lại các task nếu cần, và tiếp tục đẩy mạnh tiến độ triển khai các Milestone M1-M5 cho HPBim theo kế hoạch.

## Follow-up — 2026-10-07T16:24:11Z

Server vừa khởi động lại. Người dùng yêu cầu tiếp tục ("tiep tuc"). Vui lòng kiểm tra lại trạng thái trên đĩa, tái kích hoạt crons giám sát, đánh thức Project Orchestrator và các worker đang chạy dở để tiếp tục triển khai các Milestone cho HPBim theo đúng kế hoạch.


## 2026-10-07T22:29:51Z

Xây dựng và tích hợp Trình mô phỏng 3D Cốt thép Dầm trực quan bằng Helix Toolkit (WPF) cùng Mô-đun tính toán & bố trí cốt thép dầm theo TCVN 5574:2018 vào Add-in Revit 2026 (HPRebar), cho phép tương tác 3D thời gian thực, đọc nội lực từ ETABS 22 / Excel và tự động sinh Rebar 3D trong Revit.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Integrity mode: development

Tài liệu quy chuẩn & Nền tảng tham chiếu:
- Quy chuẩn Clean Code bắt buộc: docs/clean-code/HP_CLEAN_CODE_CORE.md, docs/clean-code/host-appendix/revit.md
- Nền tảng hiện hữu: HPRebar/HPRebar/BeamRebar/ (View, ViewModel, Services, Canvases 2D hiện có)
- Hạ tầng Revit MCP & ETABS MCP trong repo: McpShared/ và HPEtabs/

## Requirements

### R1. Tích hợp Thư viện Đồ họa 3D Helix Toolkit vào HPRebar
- Bổ sung package HelixToolkit.Wpf vào HPRebar/HPRebar/HPRebar.csproj tương thích với cấu hình đa phiên bản (đặc biệt là .NET 8 / net8.0-windows7.0 cho Revit 2026).
- Cấu hình ILRepack để merge an toàn HelixToolkit.Wpf vào HPRebar.dll, đảm bảo không xung đột dynamic loading trong tiến trình Revit.exe.

### R2. Xây dựng Khung nhìn Viewport 3D Helix (Beam3DViewport)
- Xây dựng UserControl Beam3DViewport.xaml và code-behind đóng gói HelixViewport3D với:
  - Hệ thống ánh sáng mặc định DefaultLights, lưới mặt bằng trục tọa độ GridLinesVisual3D, ViewCube điều hướng góc nhìn.
  - Khối bê tông dầm bán trong suốt (Semi-transparent Material, Alpha ~ 0.35) thể hiện rõ bao hình học dầm (b x h x L).
  - Hệ cốt thép dọc 3D dựng bằng TubeVisual3D hoặc MeshBuilder (chuẩn đường kính thực tế): Thép chủ lớp trên (màu Đỏ), thép chủ lớp dưới (màu Xanh lam), thép cấu tạo bên hông (màu Xanh lá).
  - Hệ cốt đai 3D khép kín theo đúng bước đai gia cường vùng gối (a100) và bước đai khoảng giữa nhịp (a200).
  - Bộ điều khiển Camera 3D mượt mà: Xoay Orbit (Chuột trái/phải), Pan, Zoom to cursor.

### R3. Nâng cấp Giao diện Điều khiển Tương tác (BeamRebarView & ViewModel)
- Tích hợp Beam3DViewport vào BeamRebarView.xaml song song với 2 Canvas mặt cắt 2D (Elevation & Section) hiện tại (hỗ trợ chuyển đổi Tab 3D / 2D hoặc bố cục chia màn hình).
- Ràng buộc dữ liệu MVVM phản ứng thời gian thực (Reactive): Khi kỹ sư thay đổi kích thước tiết diện dầm, số lượng thanh thép hoặc bước đai trên giao diện, mô hình 3D trong Helix Viewport tự động tái tạo và cập nhật ngay lập tức.

### R4. Mô-đun Tính toán Cốt thép Dầm theo TCVN 5574:2018 (HPRebar.Core)
- Triển khai thuật toán tính toán diện tích cốt thép yêu cầu As (uốn) và Asw (cắt) theo TCVN 5574:2018 cho tiết diện chữ rectangular thuần C# trong HPRebar.Core.
- Tự động gợi ý phương án đường kính và số thanh thép tối ưu thỏa mãn điều kiện bền và khoảng hở thông thủy cấu tạo (t >= 25 - 30 mm).
- Đọc bảng nội lực dầm từ 2 nguồn: Import bảng Excel (.xlsx qua ClosedXML) hoặc đọc trực tiếp từ ETABS 22.

### R5. Tự động Sinh Rebar 3D trong Revit 2026 & Xuất Báo cáo Excel
- Kích hoạt lệnh sinh Rebar bản địa (Structural Rebar) trong Revit 2026 vào dầm được chọn thông qua RebarCreationService, gán đúng RebarBarType, hook và spacing.
- Xuất file thuyết minh tính toán kiểm tra kết cấu chi tiết ra file Excel (.xlsx).

## Acceptance Criteria

### Tính toàn vẹn Mã nguồn & Clean Code
- [ ] 100% C# .NET Native (ĐIỀU 0: Tuyệt đối không dùng Python).
- [ ] Tuân thủ HP Clean Code Governance: Lớp <= 300 dòng, phương thức <= 50 dòng, 0 catch vô cớ, 0 warnings, 0 errors.
- [ ] Lệnh dotnet build HPRebar/HPRebar.slnx -c Debug.R26 biên dịch thành công 100%.

### Hiệu năng & Trải nghiệm 3D
- [ ] Khung nhìn 3D HelixViewport3D hiển thị mượt mà 60 FPS, xoay/zoom không giật lag.
- [ ] Cốt thép 3D hiển thị dạng thanh ống tròn đúng tỷ lệ đường kính và màu sắc quy ước.
- [ ] Cập nhật 3D tức thì (< 50ms) khi thay đổi dữ liệu cấu hình thép.

### Độ chính xác Tính toán TCVN & Revit
- [ ] 100% Unit tests trong HPRebar.Core.Tests kiểm tra công thức TCVN 5574:2018 đạt PASS.
- [ ] Sinh Rebar 3D vào Revit Document chính xác vị trí, lớp bảo vệ và không gây lỗi InvalidOperationException.


## 2026-10-08T08:01:51Z

Build HPDynamo MCP (Model Context Protocol) subsystem for Dynamo in Revit 2026, enabling AI coding agents to inspect, author, and execute Dynamo graphs (.dyn) and DesignScript/C# code both headlessly and live on the active Revit canvas.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. McpShared Dynamo Profile & Pipe Contracts
Extend `McpShared` to define Dynamo host contracts without breaking existing hosts:
- Add `PipeNaming.DynamoHost = "hpdynamo-mcp-2026"`.
- Add `HostProfile.Dynamo` and Dynamo-specific `GuardProfile` / `AnalyzerProfile` with standard namespaces and safety rules.
- Maintain strict host-neutrality and wire contract compatibility in `McpShared/`.

### R2. HPDynamo Solution & MCP Server Executable (`HPDynamo.Mcp.Server`)
Create `HPDynamo/HPDynamo.slnx` and `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport):
- Implement `Program.cs` bootstrapping `McpServerHost.CreateBuilder(args, HostProfile.Dynamo)`.
- Implement Core Tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
- Wire 8 Registry Meta Tools (`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`).
- Embed and auto-install the 12 embedded seed tools library.

### R3. Dual-Host In-Process Bridge (`HPDynamo.McpBridge`)
Create `HPDynamo.McpBridge` (.NET 8.0-windows7.0) running inside `Revit.exe`:
- **Dual-Host Lifecycle:**
  1. Revit Add-in (`IExternalApplication` + `.addin` manifest) that starts the Named Pipe listener `hpdynamo-mcp-2026` immediately upon Revit startup (allowing headless execution via `RevitDynamoModel` without Dynamo UI open).
  2. Dynamo ViewExtension (`IViewExtension` + XML manifest) that binds to the active `DynamoViewModel` / `WorkspaceModel` when Dynamo UI is opened, enabling live canvas node/wire authoring and inspection.
- **Dual-Level Safety & Rollback:**
  - Wrap graph executions modifying Revit documents in a Revit `TransactionGroup` ("MCP Dynamo: <label>"); rollback completely when `dryRun=true`.
  - Use Dynamo workspace snapshot / `UndoRecorder` for canvas-level changes.
- **ScriptGuard & Compiler:**
  - Roslyn compiler with `ScriptArgs`, denying forbidden namespaces (`System.IO`, `System.Net`, `Process`, unauthorized `Reflection`).
  - Direct DesignScript execution engine interacting with Dynamo geometry and Revit nodes.
- **Bridge UI:**
  - Modeless status window with MaterialDesignThemes 5.3.2, dark/light theme tracking, and mandatory "Allow AI Dynamo execution" opt-in toggle (default OFF).

### R4. Embedded Seed Library (12 Seeds)
Implement 12 verified seed tools under `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
1. `run_dyn_file`: Execute a `.dyn` file with input parameter dictionary overrides.
2. `get_active_graph_info`: Inspect active canvas workspace (nodes count, wires, run status, errors).
3. `search_nodes_library`: Search available node definitions in loaded Dynamo packages & core.
4. `inspect_node_type`: Get input/output ports, descriptions, and type signatures of a node.
5. `add_node_to_workspace`: Place a new node on canvas at coordinates (X, Y).
6. `connect_node_ports`: Wire an output port of Node A to an input port of Node B.
7. `disconnect_node_ports`: Disconnect wire between specified ports.
8. `set_node_properties`: Configure node attributes (title, slider range, boolean, text).
9. `get_node_output_values`: Retrieve evaluated output data from nodes after execution.
10. `export_graph_to_image`: Render canvas or graph to PNG image artifact.
11. `validate_dyn_file`: Inspect a `.dyn` JSON file for missing packages, cyclic wires, or errors.
12. `create_code_block_node`: Generate a DesignScript CodeBlock node with script content.

### R5. Test Suites & Quality Assurance
- `HPDynamo.Mcp.Server.Tests` (.NET 10.0, xUnit v3):
  - Registry lifecycle tests (draft -> tested -> pending_approval -> published).
  - Seed tool compilation tests against Dynamo and Revit reference assemblies.
  - ScriptGuard deny-list and AST analyzer tests.
- `HPDynamo.McpBridge.Tests` (.NET 8.0-windows7.0):
  - Dispatcher and named pipe protocol tests.
  - Headless graph parser and DesignScript runner tests.
- Compliance with `HP_CLEAN_CODE_CORE.md` (no forbidden namespaces, no commented-out code, structured exception logging).

## Acceptance Criteria

### Build & Packaging
- [ ] `dotnet build HPDynamo/HPDynamo.slnx` succeeds with 0 errors and 0 warnings.
- [ ] `HPDynamo.Mcp.Server` publishes cleanly as a self-contained / win-x64 console exe.
- [ ] Deployment manifests (`HPDynamo.McpBridge.addin` and `HPDynamo_ViewExtensionDefinition.xml`) are correctly generated and positioned.

### Server & Tool Surface
- [ ] Starting `HPDynamo.Mcp.Server.exe` and sending `tools/list` returns exactly 24 tools (4 Core + 8 Registry + 12 Seeds) with valid JSON Schema.
- [ ] Script proposals via `propose_tool` undergo AST validation and reject scripts exceeding 300 lines or containing forbidden namespaces.

### Execution & Safety
- [ ] Named Pipe listener `hpdynamo-mcp-2026` handles requests with JSON-RPC 2.0 NDJSON.
- [ ] `dryRun=true` in `execute_dynamo_code` or `run_dyn_file` rolls back all created Revit elements via Revit `TransactionGroup`.
- [ ] Script execution is rejected when the bridge UI opt-in checkbox is OFF.

### Test Verification
- [ ] All unit and integration tests in `HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests` pass.
- [ ] `McpShared` regression tests continue to pass without breaks (`HPRebar.Mcp.Server.Core.Tests`).

## 2026-10-08T11:29:50Z

Triển khai hoàn thiện quy trình BIM Pipeline và Coordination Ledger cho HPBim (P0 và P1) giải quyết triệt để 10 lỗi chặn từ review ngày 08/10/2026, bảo đảm kết quả trung thực tuyệt đối, preview/apply tương đương, phục hồi an toàn sau sự cố và loại bỏ mọi hình thức shadow testing.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

## Requirements

### R1. Phase 1 — P0 Core Wire, Cardinality & Strict Batch Transaction
- **Canonical String SourceKey**: Chuẩn hóa `SourceKey` thành chuỗi canonical `[DocIdentifier]#[Category]:[PrimaryHandle]` trên toàn bộ `ElementPlanItem`, wire DTO, `ScriptArgs`, Roslyn runner và `HPBimCoordinationReceipt`. Bảo đảm 100% roundtrip equality qua JSON serialization/deserialization.
- **Cardinality 1:1 & Không Synthesize Created**: Bắt buộc đối soát chính xác 1:1 giữa danh sách items trong plan và execution records trả về từ Revit. Cấm tuyệt đối việc tự tạo kết quả `Created` khi response thiếu records hoặc khi response bị cắt cụt do vượt ngưỡng 64KB (`execRes.Truncated == true`). Bất kỳ sự thiếu hụt nào phải đánh dấu ngay `OutcomeUnknown`.
- **Strict All-or-Nothing Batch Transaction**: Khi bất kỳ cấu kiện nào trong batch gặp lỗi (`Failed`), rollback toàn bộ transaction của batch đó trên Revit, không ghi receipt vào Extensible Storage, và báo lỗi toàn diện, bảo đảm không để lại mô hình dở dang trong RVT.
- **Loại bỏ Heuristic Fallbacks trong Script Runner**: Áp dụng đầy đủ `rotationDeg` và `topLevel` constraint cho Cột. Loại bỏ hoàn toàn các lệnh `FirstOrDefault()` khi chọn Pipe/PipingSystem type; nếu thiếu symbol/type phải báo lỗi tường minh `ERR_SYMBOL_NOT_FOUND`.

### R2. Phase 2 — P1 Document Identity, Complete Hash & Journal Hydration
- **Ràng buộc Định danh Tài liệu Đích**: CLI `generate` tự động truy vấn thông tin tài liệu Revit đang hoạt động (Title, Path, VersionGUID) trên Revit API thread trước khi biên dịch plan; gắn chặt vào `ExecutionPlan` và `IntentJournal`. Pre-flight trên Revit runner sẽ từ chối thực thi nếu phát hiện sai tài liệu hoặc snapshot đã lỗi thời.
- **Canonical Plan Hash Toàn diện**: `BimExecutionPlan.ComputePlanHash` phải bao phủ toàn bộ các thuộc tính ảnh hưởng đến hình học và mô hình: tọa độ tất cả đỉnh polygon của profile, độ dày, cao độ, thông số tiết diện, family, host, top level và document identity.
- **Hydration Index Sổ cái & Bảo toàn Metadata Journal**:
  - `BimCoordinationLedgerService.LoadFromFile` phải tự động khôi phục và hydrate toàn bộ chỉ mục `_mappings` (SourceKey → Revit UniqueId) từ dữ liệu lưu trên đĩa.
  - `IntentJournal.UpdateStatusAsync` khi cache miss phải nạp lại record gốc từ đĩa trước khi cập nhật trạng thái, bảo toàn 100% metadata (`PlanHash`, `ItemCount`, `SourceDoc`, `TargetDoc`) qua các phiên process restart.
  - `BimCoordinationLedgerSerializer` thực hiện Atomic File Replace (.tmp -> atomic replace) kèm file-lock để chống hư hỏng dữ liệu khi tiến trình bị tắt đột ngột.
- **Đối soát Reconcile 3 Yếu tố Chặt chẽ**: `ReconcileService` chỉ công nhận `Committed` khi receipt trong Extensible Storage khớp đồng thời cả 3 yếu tố: `OperationId`, `PlanHash`, và `TargetDocId`. Nếu không tìm thấy receipt hoặc thiếu bằng chứng, giữ nguyên trạng thái `OutcomeUnknown` để ngăn chặn retry ghi đè mù quáng.

### R3. Phase 3 — Đo đạc Hình học QA/QC Thực tế & Chuyển đổi E2E Tests
- **Kiểm toán Hình học Thực tế (Measured Geometric Verification)**: Nối `MeasuredGeometricVerifier` vào lệnh `VerifyCommand` và quy trình nghiệm thu CLI. Thực hiện truy vấn trực tiếp hình học (Solid, Curve, Elevation) từ Revit API qua named pipe để so sánh với dung sai `CoordinationTolerances`. Loại bỏ toàn bộ các số liệu gán sẵn (`AvgCadEdgeDevMm=0.5`...) và cấm nghiệm thu nếu thiếu dữ liệu đối chiếu (phải trả về `Inconclusive`).
- **Xóa bỏ Shadow Mocks trong E2E Tests**: Refactor toàn bộ `HPBim.E2E.Tests`, loại bỏ hoàn toàn các lớp shadow contracts tự tạo (`E2ESchemaValidator`, `E2EExecutionPlan`, `E2EIntentJournal`...), chuyển 100% sang kiểm thử trực tiếp các production classes của `HPBim.Core` và `HPBim.Client`. Chỉ sử dụng fake test ở biên truyền tải I/O Named Pipe.

## Acceptance Criteria

### Verification & Testing
- [ ] **Test Production Coverage**: 100% tests trong `HPBim.Core.Tests`, `HPBim.Client.Tests`, và `HPBim.E2E.Tests` sử dụng trực tiếp production classes và PASS 100% (zero regressions trên toàn bộ 3,489 tests của repo).
- [ ] **SourceKey Wire Roundtrip**: Test chứng minh `CompositeSourceKey` string canonical giữ nguyên equality sau chu trình `ExecutionPlan` -> `BridgeJson` -> `ScriptArgs` -> `BimPlanRunner` -> `ExecuteResult` -> `Ledger/Receipt`.
- [ ] **Strict Batch Rollback**: Test chứng minh khi 1 cấu kiện trong batch 10 phần tử bị lỗi, toàn bộ batch bị rollback, không có receipt nào được tạo và CLI trả về exit code 1.
- [ ] **No Synthesized Created**: Test chứng minh khi response bị rỗng hoặc truncated >64KB, pipeline trả về `OutcomeUnknown` và kích hoạt Reconcile, không tự tạo kết quả `Created`.
- [ ] **Ledger & Journal Persistence Across Restarts**: Test khởi tạo instance `IntentJournal` và `BimCoordinationLedgerService` mới từ file trên đĩa chứng minh `_mappings` được hydrate đầy đủ và metadata của intent không bị mất khi cập nhật trạng thái.
- [ ] **Measured Geometric Verification**: Cấu kiện sai lệch hình học hoặc thiếu bằng chứng đo đạc từ Revit API bị đánh dấu `Inconclusive` hoặc `Defect`, cấm tự động chuyển sang `ConfirmedPass`.
- [ ] **Revit API Binding Probe**: Biên dịch thành công mã runner C# Roslyn đối chiếu với các reference assemblies của Revit 2026.

### Repository & Governance Rules
- [ ] Không tạo dependency hai chiều giữa `HPAutoCad` và `HPRebar`.
- [ ] Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file <= 300 dòng (Rule C2), method <= 50 dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2).
- [ ] Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.

## 2026-10-08T14:31:16Z

Quota has reset. Please resume: complete Milestone M5 Gate 2 verification, run the final victory audit, and conclude the project.
## 2026-10-08T15:03:54Z

Triển khai hoàn thiện đợt 2 (Wave 2 Remediation) cho HPBim tại G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim giải quyết triệt để 5 lỗi chặn P0/P1 từ báo cáo re-review ngày 08/10/2026: (1) Strict Record Matching & Validation; (2) Journal Torn-tail Auto-Recovery; (3) Recovery Receipt Coverage & Integrity; (4) CLI Ledger Wiring & Ledger-First Durable Commit; (5) Measured Geometric Engine Strict Inconclusive.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

## Requirements

### R1. Strict Record Matching & 1:1 Bijection Validation (P0)
- Nâng cấp `ReconcileRecordsStrict1To1` trong `BimPipelineEngine.ElementRecords.cs` để đối soát song ánh (bijection) chính xác 1:1 giữa planned items và returned records.
- Từ chối ngay lập tức (`OutcomeUnknown` / `Failed`) nếu phát hiện:
  - Record ngoại lai (`Foreign record` — SourceKey không nằm trong plan).
  - Record trùng lặp (`Duplicate record` — nhiều hơn 1 record cho cùng 1 SourceKey).
  - Giá trị `Status` không thuộc dải enum `ElementExecutionStatus` hợp lệ.
  - Record có status `Created` hoặc `Updated` nhưng thiếu `TargetUniqueId`.
  - Mâu thuẫn Category hoặc Mark giữa plan item và execution record.
- Cấu kiện bắt buộc (mặc định toàn bộ plan item) nếu bị Revit trả về `Skipped` mà không có lý do được định nghĩa trước sẽ bị coi là lỗi vi phạm tính toàn vẹn (Integrity Defect) và kích hoạt rollback batch.

### R2. Journal Torn-Tail Resilience & Auto-Recovery (P1)
- Xử lý triệt để lỗi torn-tail trong `IntentJournal.cs`: khi mở tệp hoặc trước khi thực hiện `AppendLineWithFlushAsync`, kiểm tra dưới `_ioLock` xem ký tự cuối cùng của file có phải ký tự xuống dòng (`\n`) hay không.
- Nếu phát hiện dòng cuối bị cắt cụt dở dang (thiếu newline hoặc JSON không hoàn chỉnh), tiến hành đệm xuống dòng hoặc cắt tỉa/quarantine phần dở dang về ranh giới dòng hợp lệ trước khi ghi bản ghi mới.
- Bảo đảm 100% các intent mới sau sự cố crash/torn-tail đều được ghi trọn vẹn và đọc lại chính xác khi cold restart (khắc phục test `TORN_APPEND_INTENT_LOST`).

### R3. Recovery Receipt Completeness & Coverage Verification (P1)
- Nâng cấp `ExtensibleStorageReceiptReader.cs` và `ReconcileService.cs`:
  - `ParseElementMap` không được âm thầm chuyển JSON hỏng thành map rỗng; JSON hỏng phải trả về `null` hoặc báo lỗi rõ ràng.
  - `ApplyCommittedReceiptAsync` bắt buộc đối soát số lượng phần tử (`ItemCount`) và danh sách expected keys trong receipt so với intent gốc. Nếu số lượng không khớp hoặc mapping bị thiếu/hỏng, giữ nguyên trạng thái `OutcomeUnknown`, cấm đánh dấu journal là `Committed`.
  - Đối soát định danh tài liệu đích bằng cả `Title`, `PathName` và `VersionGuid` (hoặc `CentralGuid` nếu có), ngăn chặn việc nhận nhầm receipt từ bản sao RVT cùng tên.

### R4. CLI Ledger Wiring & Durable Ledger-First Commit Order (P1)
- Nối `BimCoordinationLedgerService` vào `GenerateCommand.cs`:
  - Khởi tạo/nạp ledger từ đĩa (hoặc đường dẫn cấu hình mặc định) và truyền vào `pipeline.LedgerService`.
  - Kích hoạt cơ chế tra cứu `TargetUniqueId` tự động để phân loại `Create` vs `Update`/`NoOp`.
- Thực hiện thứ tự ghi bền vững **Ledger-First**:
  - Khi pipeline apply thành công, cập nhật mapping và lưu `BimCoordinationLedger` xuống đĩa nguyên tử (`.tmp` -> atomic replace) **TRƯỚC KHI** cập nhật `IntentJournal` sang trạng thái `Committed`.
  - Kiểm chứng qua tiến trình mới (cold process rerun): apply lần 1 -> thoát process -> chạy lại lần 2 bằng process mới -> 100% phần tử đạt trạng thái `NoOp`, không tăng số lượng element trong mô hình.

### R5. Measured Geometric Engine Strict Inconclusive Threshold (P0/P1)
- Nâng cấp `MeasuredGeometricEngine.Structural.cs` và `MeasuredGeometricEngine.ProfileGrid.cs`:
  - Định nghĩa tập thuộc tính đo đạc bắt buộc (Required Measurements) theo từng Category (ví dụ: Pile yêu cầu Centroid; Column yêu cầu Centroid, Width, Height; Beam yêu cầu CurveStart, CurveEnd, TopElevation; Slab/Footing yêu cầu TopElevation, Thickness).
  - Nếu cấu kiện thực tế từ Revit thiếu hoặc có giá trị null ở bất kỳ thuộc tính đo đạc bắt buộc nào, kết quả kiểm toán phải chuyển thành `GeometricVerificationStatus.Inconclusive` (không được tự động coi delta = 0 rồi trả về `Verified`).
  - Lệnh `VerifyCommand.cs` và quy trình nghiệm thu CLI coi mọi kết quả `Inconclusive` hoặc `Defect` là không đạt chuẩn nghiệm thu và trả về exit code 1.

## Acceptance Criteria

### Verification & Testing
- [ ] **Strict 1:1 Bijection Test**: Test chứng minh khi Revit trả về duplicate record, foreign record, status enum không hợp lệ, hoặc `Created` thiếu UniqueId, pipeline lập tức rollback và trả về `Success = false`.
- [ ] **Skipped Policy Test**: Test chứng minh plan item bắt buộc bị Revit trả về `Skipped` sẽ khiến batch bị rollback và báo Integrity Defect.
- [ ] **Journal Torn-tail Recovery Test**: Test mô phỏng file journal bị đứt ngang không có `\n` ở cuối; sau khi gọi `RecordIntentAsync`, instance mới của `IntentJournal` đọc được đầy đủ 100% intent mới mà không làm mất dữ liệu.
- [ ] **Corrupt Receipt Rejection Test**: Test chứng minh khi receipt trong Extensible Storage có elementMap bị hỏng hoặc thiếu phần tử so với `ItemCount`, `ReconcileService` giữ nguyên trạng thái `OutcomeUnknown` và không cập nhật journal sang `Committed`.
- [ ] **CLI Ledger-First Idempotency Test**: Test end-to-end qua 2 process riêng biệt: Lần 1 apply tạo N phần tử; Lần 2 apply cùng plan nhận kết quả N phần tử `NoOp`, không có duplicate element.
- [ ] **Missing Measurement Inconclusive Test**: Test kiểm chứng cấu kiện Pile/Slab/Column có `HasGeometry = true` nhưng thiếu `Centroid` hoặc `TopElevation` bị đánh dấu `Inconclusive` (không được là `Verified`), và CLI `verify` trả về exit code 1.
- [ ] **Zero Regressions**: 100% bộ test (`HPBim.Core.Tests`, `HPBim.Client.Tests`, `HPBim.E2E.Tests`, `HPRebar.Core.Tests`, `McpShared`) PASS (3,543+ tests).

### Repository & Governance Rules
- [ ] Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file $\le 300$ dòng (Rule C2), method $\le 50$ dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2).
- [ ] `HPBim.Core` giữ thuần `netstandard2.0`, không reference `Autodesk.Revit.*`.
- [ ] Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.


## 2026-10-09T05:36:26Z

Thực hiện kế hoạch hiệu chỉnh toàn diện phân hệ HPDynamo MCP nhằm khắc phục triệt để 8 phát hiện kỹ thuật (RV-01 đến RV-08) từ báo cáo đánh giá độc lập Codex Review 2026-10-09, bảo đảm tính đúng đắn giao dịch Revit/Dynamo, cơ chế rollback canvas toàn diện cho dryRun, an toàn hàng đợi và cách ly hủy bỏ theo request ID, đồng bộ hóa evaluation xuyên suốt core tools và seeds, trích xuất chuẩn xác dữ liệu ProtoCore MirrorData và DesignScript Dictionary, cùng harness benchmark thực nghiệm lưu raw data.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. Tính Đúng Đắn Giao Dịch & Canvas Rollback Toàn Vẹn (RV-01, RV-06)
- **Chuẩn hóa nguồn `dryRun` duy nhất tại execution boundary**: Đảm bảo các script và seed (bao gồm `batch_edit_graph`) nhận biết chính xác cờ `dryRun` từ `ExecuteRequest` (đặc biệt khi `DynamicToolRegistrar` đã bóc tách cờ này khỏi `args`).
- **Rollback canvas toàn diện cho `batch_edit_graph`**: Kết hợp Dynamo `UndoRecorder` khi ở Canvas UI với danh sách programmatic rollback snapshot (ghi nhận trạng thái dây cũ bị thay thế, giá trị/thuộc tính cũ của nodes có sẵn). Khi `dryRun = true` hoặc phát sinh ngoại lệ/cancel ở bất kỳ thao tác nào, phải hoàn tác 100% thay đổi trên canvas Dynamo và rollback Revit `TransactionGroup`.
- **Phân định quyền quản lý Transaction thông minh trong `DynamoExecutionEngine`**:
  - Nếu script C# Roslyn thao tác trực tiếp Revit DB (gọi `doc.Create`, set parameter...) với `transaction = "auto"`, kích hoạt `startAutoTransaction: true`.
  - Nếu phát hiện script gọi wrapper `Revit.Elements.` hoặc tự quản lý qua Dynamo `TransactionManager`, đặt `startAutoTransaction: false` để nhường quyền mở child transaction cho Dynamo và chỉ bọc `TransactionGroup` ngoài cùng.

### R2. Đồng Bộ Hóa Evaluation & Queue Ownership Khi Cancel (RV-02, RV-03, RV-05)
- **Xác thực kết quả `RunAndWait`**: Trong `CanvasGraphRunner`, kiểm tra chặt chẽ giá trị trả về của `DynamoEvaluationSync.RunAndWait`. Nếu evaluation thất bại, timeout hoặc cancel thì không được trả `Success = true`, phải ghi log lỗi và kích hoạt rollback transaction.
- **Thống nhất đường Evaluation Sync**: Đảm bảo cả `run_dyn_file` và `batch_edit_graph` đều đi qua cơ chế chờ `EvaluationCompleted` trước khi trích xuất kết quả và trạng thái lỗi.
- **Bảo toàn Linked Timeout Token**: Truyền đúng `linkedToken` từ `RevitTransactionScope` vào runner execution callback thay vì dùng raw work item token.
- **Quản lý trạng thái Work Item và Thread Ownership**: Quản lý rõ ràng trạng thái (`Queued`, `Running`, `Completed`). Khi cancellation xảy ra trong lúc work đang chạy trên Revit thread, không được set `_busy = 0` sớm; giữ thread ownership cho tới khi toàn bộ thao tác thực thi và rollback trên Revit hoàn tất.

### R3. Request-Scoped Cancel & Trích Xuất Dữ Liệu ProtoCore MirrorData (RV-04, RV-07, RV-08)
- **Cách ly lệnh Cancel theo Request ID**: Bổ sung interface `ICorrelatedCancelExecutor` trong `McpShared/HPRebar.McpBridge.Core` kế thừa hoặc bổ trợ `IBridgeExecutor` (đảm bảo 100% tương thích nhị phân và mã nguồn cho 10 host khác, kể cả net48). `RequestDispatcher` kiểm tra và truyền `requestId` nếu executor hỗ trợ. `DynamoBridgeCoordinator` chỉ hủy request khớp với ID được yêu cầu.
- **Sửa Serializer trích xuất MirrorData & Dictionary**: Trong `get_node_output_values/code.cs`:
  - Kiểm tra `MirrorData.IsCollection` trước; nếu là collection, duyệt qua `MirrorData.GetElements()` thay vì bỏ qua khi `MirrorData.Data == null`.
  - Nhận diện và trích xuất cấu trúc `DesignScript.Builtin.Dictionary` thông qua các thuộc tính `.Keys` và phương thức `.ValueAtKey()`.
- **Kiểm soát ngân sách Payload**: Bổ sung giới hạn số lượng phần tử tối đa (breadth budget) và độ sâu đệ quy (depth budget), đồng thời kiểm tra cooperative `cancellationToken` trong quá trình duyệt dữ liệu lớn.

### R4. Benchmark Thực Nghiệm, Raw Data & Đồng Bộ Tài Liệu
- **Harness Benchmark Thực Nghiệm**: Tạo kịch bản PowerShell `run-benchmark.ps1` thực hiện đo đạc các workload chuẩn trên file model test copy cô lập; lưu file kết quả thô `reports/baseline-vs-upgraded.json` ghi nhận p50/p95, round-trips và UTF-8 payload bytes.
- **Hiệu Chỉnh Tài Liệu & Phân Định Bằng Chứng**: Cập nhật `audit.md`, `plan.md`, `benchmark-plan.md` làm rõ ranh giới giữa giả thuyết ước tính và số liệu đo đạc thực tế; ghi nhận thực nghiệm TFM .NET 10 của Revit 2026.
- **Cập Nhật Skill `hp-mcp-dynamo`**: Cập nhật số lượng công cụ lên 25 tools (4 core + 8 registry + 13 seeds) và bổ sung tài liệu hướng dẫn sử dụng `batch_edit_graph`.

## Controlled Infrastructure & Safety Constraints
- **Tuyệt đối không can thiệp phiên làm việc Revit hiện hành**: Không inject code, không mở hay đóng model người dùng đang mở trong PID 26264; mọi kiểm chứng live phải thực hiện trên file bản sao riêng biệt trong thư mục test.
- **Bảo toàn McpShared**: Mọi thay đổi trong `McpShared/` phải giữ nguyên tính host-neutral, tương thích tuyệt đối cả .NET 10 và .NET Framework 4.8, không gây breaking changes cho các bridge host khác.
- **Tuân thủ Clean Code Governance**: Mỗi file C# không vượt quá 300 dòng, không chứa khối catch rỗng thiếu lý do, không để lại code bị comment, và đạt chuẩn Q-W1 đến Q-W5.

## Acceptance Criteria

### Tính Đúng Đắn Giao Dịch & Rollback (RV-01, RV-06)
- [ ] Gọi `batch_edit_graph` với `dryRun = true` qua MCP: Toàn bộ node mới được dọn dẹp, các dây và thuộc tính của node cũ được phục hồi nguyên vẹn trên canvas Dynamo, và Revit `TransactionGroup` được rollback sạch sẽ.
- [ ] Gọi `batch_edit_graph` bị lỗi ở bước giữa: Toàn bộ các thao tác đã áp dụng trước đó được rollback tự động về trạng thái ban đầu.
- [ ] Script C# Roslyn thuần túy tạo element Revit (`doc.Create...`) dưới `execute_dynamo_code` với `transaction = "auto"` thực thi thành công, không gặp lỗi thiếu transaction.
- [ ] Script Dynamo tạo element thông qua wrapper Dynamo được bọc an toàn trong `TransactionGroup`, không phát sinh lỗi lồng transaction.

### Đồng Bộ Evaluation & Queue Safety (RV-02, RV-03, RV-05)
- [ ] Khi evaluation bị lỗi cú pháp, runtime error hoặc timeout, `CanvasGraphRunner` báo `Success = false` và rollback transaction tương ứng.
- [ ] `run_dyn_file` và `batch_edit_graph` chờ đợi sự kiện `EvaluationCompleted` hoàn tất trước khi đọc dữ liệu đầu ra.
- [ ] Khi một request đang chạy trên Revit thread bị cancel, cờ `_busy` vẫn giữ nguyên giá trị `true` cho tới khi Revit thread kết thúc và hoàn tất rollback, ngăn ngừa hoàn toàn tình trạng request mới chạy chồng lấn.

### Serialization & Cancellation (RV-04, RV-07, RV-08)
- [ ] Lệnh cancel gửi kèm `requestId` của một request cũ đã kết thúc không làm hủy nhầm request mới đang thực thi.
- [ ] Node CodeBlock xuất mảng (ví dụ `[1, 2, 3]` hoặc nested array) trích xuất thành công JSON Array tương ứng, không bị trả về `null`.
- [ ] Node xuất `DesignScript.Builtin.Dictionary` trích xuất thành công JSON Object chứa các cặp key-value tương ứng.
- [ ] Quá trình trích xuất mảng dữ liệu cực lớn tôn trọng giới hạn budget và hủy bỏ tức thì khi token cancellation được kích hoạt.

### Kiểm Chứng Test Suites & Tài Liệu
- [ ] 100% các bộ test suites tiếp tục pass:
  - `HPDynamo.Mcp.Server.Tests` (net10.0)
  - `HPDynamo.McpBridge.Tests` (net10.0-windows7.0)
  - `HPDynamo.E2E.Tests` (net10.0)
  - `HPRebar.Mcp.Server.Core.Tests` (net10.0)
  - `HPRebar.McpBridge.Core.Net48Tests` (net48)
- [ ] File benchmark raw data `reports/baseline-vs-upgraded.json` được tạo thành công với số liệu đo thực tế.
- [ ] `audit.md`, `plan.md` và skill `hp-mcp-dynamo` được cập nhật đồng bộ, không còn thông tin mâu thuẫn.


## 2026-10-09T14:32:33Z

Thực hiện kế hoạch hiệu chỉnh kỹ thuật toàn diện phân hệ HPDynamo MCP Vòng 2 nhằm xử lý triệt để 10 phát hiện (R2-01 đến R2-10) từ báo cáo Codex Review Round 2 (2026-10-09). Dự án được triển khai theo 3 đợt tuần tự nghiêm ngặt (Đợt A: Sửa tính đúng đắn giao dịch, rollback canvas và evaluation; Đợt B: Khôi phục vận hành thực tế, giải quyết lỗi biên dịch và gỡ trạng thái quarantined cho 3 tools; Đợt C: Nâng cấp harness benchmark đo lường trung thực, persistent session và lưu raw evidence khách quan).

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. Đợt A — Tính Đúng Đắn Giao Dịch, Canvas Rollback & Evaluation (R2-01..R2-07, R2-09, R2-10)
- **Snapshot & Rollback Node Input Values (R2-01)**: Ghi nhận snapshot giá trị thực tế của các loại node input (Number, String, Boolean, CodeBlock...) trước khi áp dụng `properties[].value`. Khi `dryRun = true` hoặc phát sinh ngoại lệ/hủy trong batch, phục hồi 100% giá trị gốc của node input về canvas Dynamo.
- **Thứ Tự Phục Hồi Dây Nối (R2-02)**: Xử lý đúng tình huống thay thế nhiều dây nối trên cùng một input port trong một batch (A→T, rồi B→T, rồi C→T). Duy trì snapshot trạng thái ban đầu duy nhất hoặc phục hồi theo thứ tự đảo ngược (LIFO) để đảm bảo khi rollback, canvas quay về đúng kết nối ban đầu (A→T), không dừng lại ở trạng thái trung gian (B→T).
- **Bảo Tồn Dây Nối Khi Sửa CodeBlock (R2-03)**: Snapshot cấu trúc ports và các connectors chịu ảnh hưởng trước khi cập nhật mã nguồn CodeBlock. Khi rollback, phục hồi cả nội dung code, cấu trúc port layout và tái tạo lại toàn bộ connectors bị ảnh hưởng.
- **Bảo Toàn Kết Quả Transaction Thật Sự Tại Coordinator (R2-04)**: Transaction boundary là nguồn sự thật duy nhất về trạng thái commit/rollback. Khi một request đã commit thành công trên Revit DB, Coordinator không được ghi đè kết quả thành `RolledBack = true` khi lệnh cancel đến muộn; chỉ báo chưa mutation khi work item chưa bắt đầu thực thi.
- **Thống Nhất Evaluation Sync & Xử Lý Lỗi Xuyên Suốt Mọi Mode (R2-05)**: Đồng bộ hóa cơ chế chờ sự kiện `EvaluationCompleted` cho cả `batch_edit_graph` (`runEvaluation = true`), `HeadlessGraphRunner`, và `run_dyn_file`. Headless mode phải áp dụng `inputs` đầy đủ và không bao giờ báo `success = true` khi graph gặp lỗi node hoặc lỗi evaluation.
- **Đảm Bảo Dynamo Đã Dừng Hoàn Toàn Khi Timeout/Cancel (R2-06)**: Khi timeout hoặc token cancellation được kích hoạt, xác nhận scheduler của Dynamo đã dừng hẳn trước khi nhả thread ownership (`_busy = 0`) hoặc finalize transaction. Ngăn ngừa triệt để request mới chạy xen lấn khi evaluation cũ chưa kết thúc.
- **Phân Định Quyền Quản Lý Transaction Bằng Cú Pháp AST/Policy (R2-07)**: Loại bỏ triệt để việc kiểm tra quyền transaction bằng substring/string match. Sử dụng phân tích cú pháp AST syntax hoặc policy thực thi rõ lượng; ngăn chặn tình trạng tên biến, comment hoặc chuỗi log làm sai lệch quyền mở transaction.
- **Cô Lập Cancel Theo Session & Chống Cancel-of-Cancel (R2-09)**: Định danh request theo cặp `(session, request)` để tránh va chạm ID khi reconnect. Ngăn chặn chuỗi cancel-of-cancel đệ quy đối với các control/cancel methods.
- **Kiểm Soát Ngân Sách Payload Byte & Sửa Lỗi Async Tests (R2-10)**: Bổ sung giới hạn dung lượng UTF-8 byte và chuỗi ký tự tối đa cho output; tách biệt metadata cắt gọn (`_truncated`) ra khỏi dữ liệu payload để không ghi đè dữ liệu người dùng. Thêm từ khóa `await` cho các lời gọi `Assert.ThrowsAsync` trong test suite.

### R2. Đợt B — Khôi Phục Vận Hành Thực Tế & Gỡ Quarantined 3 Tools
- **Thu Thập Diagnostics & Sửa Lỗi Biên Dịch Thực Tế**: Trích xuất chi tiết compile diagnostics trong SQLite registry và compiler cho 3 tools đang bị `quarantined` (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`).
- **Biên Dịch Sạch Bằng Production Compiler**: Đảm bảo toàn bộ 13 embedded seeds biên dịch thành công 100% bằng chính production `ScriptCompiler` cùng các references (`DynamoCompilerReferences`), imports và globals mà bridge sử dụng.
- **Khôi Phục Trạng Thái Registry**: Đưa cả 3 công cụ từ trạng thái `quarantined` trở về `published` theo đúng workflow chuẩn của tool registry mà không xóa dữ liệu người dùng, khôi phục bề mặt công cụ đầy đủ 25 tools (4 Core + 8 Registry + 13 Seeds).

### R3. Đợt C — Nâng Cấp Harness Benchmark Đo Lường Trung Thực & Model Isolation (R2-08)
- **Harness Benchmark Phân Tích Lỗi Trung Thực (R2-08)**: Sửa `run-benchmark.ps1` phân tích đầy đủ JSON-RPC error, MCP `isError = true`, và domain failure; dừng vòng lặp và ghi nhận FAIL/SKIP rõ ràng, tuyệt đối không đánh dấu `Success` cho các lần chạy bị lỗi.
- **Tách Biệt Startup vs Steady-State Persistent Session**: Bổ sung cơ chế đo lường phiên MCP kết nối liên tục (persistent session) cho các warm runs, lưu lại đầy đủ raw samples, percentiles p50/p95 và dung lượng byte thực tế; không suy đoán tỷ lệ tăng tốc nếu chưa đối chiếu trên cùng fixture.
- **Bảo Vệ Tuyệt Đối Tiến Trình Revit Đang Mở**: Tự động phát hiện tiến trình Revit (PID 26264), cô lập toàn bộ fixture thử nghiệm trên file bản sao riêng biệt, không can thiệp hay đóng ứng dụng người dùng.

## Controlled Infrastructure & Safety Constraints
- **Zero Touch Active Revit Session**: Tuyệt đối không can thiệp, không inject code, không mở hay đóng model người dùng đang mở trong PID 26264.
- **Quy Tắc Clean Code Governance**: Mỗi file C# không vượt quá 300 dòng (Rule C2), mỗi phương thức không vượt quá 50 dòng (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated empty catch (Rule Q-B2).
- **Bảo Toàn McpShared Host-Neutrality**: Các thay đổi trong `McpShared/` phải giữ nguyên tính host-neutral, tương thích 100% cho cả 10 CAD/BIM hosts và .NET Framework 4.8.

## Acceptance Criteria

### Tính Đúng Đắn Giao Dịch & Canvas Rollback (Đợt A)
- [ ] Node Number/String/Boolean có sẵn được phục hồi nguyên vẹn giá trị khi `batch_edit_graph` chạy với `dryRun = true` hoặc gặp lỗi giữa chừng.
- [ ] Thay thế nhiều lần trên cùng một input port trong một batch khi rollback sẽ trả về chính xác dây nối ban đầu, không dừng ở trạng thái trung gian.
- [ ] Sửa CodeBlock trong batch với `dryRun = true` phục hồi trọn vẹn cả nội dung code, cấu trúc port layout và các dây nối vào/ra.
- [ ] Coordinator không ghi đè kết quả commit thành `RolledBack = true` khi lệnh cancel đến muộn; phản ánh trung thực kết quả giao dịch trên Revit DB.
- [ ] Đoạn mã C# chứa chuỗi literal hoặc comment `"TransactionManager"` vẫn được mở transaction auto bình thường khi gọi `execute_dynamo_code` với `transaction = "auto"`.
- [ ] `batch_edit_graph` (`runEvaluation = true`) và `run_dyn_file` chờ `EvaluationCompleted` hoàn tất và trả đúng lỗi node nếu có, không báo `success = true` khi graph fail.
- [ ] Khi timeout hoặc cancel, cờ `_busy` được giữ cho tới khi Dynamo scheduler xác nhận dừng hoàn toàn.
- [ ] Khi reconnect với pipe mới, cancellation không hủy nhầm request của session trước; không phát sinh cancel-of-cancel.
- [ ] Output lớn được giới hạn an toàn theo cả số lượng phần tử và dung lượng UTF-8 byte, metadata truncation không ghi đè lên key dữ liệu của người dùng.
- [ ] Toàn bộ các lời gọi `Assert.ThrowsAsync` trong test suite được bổ sung `await` đầy đủ.

### Khôi Phục Vận Hành Thực Tế & Surface 25 Tools (Đợt B)
- [ ] Cả 3 công cụ (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) được biên dịch thành công bằng production compiler, không còn trạng thái `quarantined`.
- [ ] Bề mặt công cụ MCP (`tools/list`) hiển thị đầy đủ và chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).
- [ ] 100% các bộ test suites tiếp tục pass:
  - `HPDynamo.Mcp.Server.Tests` (.NET 10.0)
  - `HPDynamo.McpBridge.Tests` (.NET 10.0-windows7.0)
  - `HPDynamo.E2E.Tests` (.NET 10.0)
  - `HPRebar.McpBridge.Core.Net48Tests` (.NET Framework 4.8)
  - `HPRebar.Mcp.Server.Core.Tests` (.NET 10.0)

### Benchmark Thực Nghiệm & Báo Cáo Chân Thực (Đợt C)
- [ ] `run-benchmark.ps1` ghi nhận trung thực lỗi nếu có, không hardcode nhãn `Passed` hay tỷ lệ phần trăm khi chưa đo được.
- [ ] Dữ liệu raw benchmark `baseline-vs-upgraded.json` được cập nhật từ quá trình chạy thực tế với persistent session cho warm runs.
- [ ] Tiến trình Revit PID 26264 được bảo vệ an toàn 100%, không bị ảnh hưởng.


## 2026-10-09T14:54:56Z

Triển khai hoàn thiện đợt 3 (Wave 3 Remediation) cho HPBim tại G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim giải quyết dứt điểm các lỗi chặn P0/P1 từ báo cáo đánh giá ngày 09/10/2026 (antigravity-hpbim-review-20261009.md): tính bền vững của IntentJournal khi nhiều tiến trình ghi đồng thời, quy trình Reconcile persist-before-terminal, tách biệt Document Identity khỏi dynamic DocumentVersion GUID, NoOp/Skip và Rollback truthfulness, cùng việc bảo toàn ProvenanceHandles và hình học đo đạc.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

## Requirements

### R1. Cross-Process Concurrency & File Lock cho IntentJournal (P1)
- Xử lý xung đột ghi đồng thời (multi-writer concurrency) giữa nhiều instance/process của IntentJournal. Thay thế semaphore đơn lẻ theo instance bằng cơ chế khóa liên tiến trình (cross-process file lock / mutex) khi thực hiện append, update và compaction.
- Bảo đảm an toàn tuyệt đối cho chu trình read/modify/write: kiểm thử bằng kịch bản chạy 2 instance ghi đồng thời 1,000 unique intents (mỗi instance 500 GUID) và cold-read bằng instance mới phải nạp đủ 1,000/1,000 intents nguyên vẹn, không thất thoát bản ghi nào.

### R2. Reconcile Persist-Before-Terminal & Exact Keyset Coverage (P1)
- Nâng cấp ReconcileService.ApplyCommittedReceiptAsync: lưu BimCoordinationLedger xuống đĩa nguyên tử (.tmp -> replace) TRƯỚC KHI cập nhật trạng thái IntentJournal sang Committed.
- Đối soát tính đầy đủ của keyset so với intent gốc (không chỉ đối soát số lượng ItemCount mà còn kiểm tra toàn bộ danh sách SourceKey). Nếu mapping bị thiếu, hỏng hoặc lỗi lưu đĩa, bảo toàn trạng thái OutcomeUnknown để cho phép reconcile chạy lại an toàn.

### R3. Tách biệt Persistent Document Identity khỏi Dynamic DocumentVersion GUID (P1)
- Tách bạch rõ ràng định danh tĩnh của tài liệu Revit (Title, PathName, CentralGuid/Project GUID) khỏi chỉ số revision động (VersionGuid/NumberOfSaves vốn thay đổi sau khi tạo element).
- MatchTargetDocSnapshot: bắt buộc đối soát các trường định danh tĩnh cần thiết (không fail-open khi thiếu path/GUID nếu intent đã có).
- Ngăn chặn việc từ chối nhầm receipt: việc VersionGuid thay đổi do chính operation tạo element gây ra không được làm thất bại quá trình recovery của chính operation đó.

### R4. Tính Trung thực của NoOp, Skip và Rollback Semantics (P0/P1)
- ValidateReturnedRecord: Bắt buộc cấu kiện có trạng thái NoOp phải có TargetUniqueId hợp lệ và khớp với mapping trong ledger (từ chối NoOp thiếu UniqueId).
- Chuẩn hóa lý do Skipped bằng enum/mã lỗi cụ thể và policy định nghĩa trước trong plan; loại bỏ hoàn toàn việc so khớp chuỗi con lỏng lẻo (Contains).
- Khi phát hiện integrity defect sau khi dispatch: nếu host chưa xác nhận rollback, pipeline phải báo trạng thái OutcomeUnknown chứ không tự ghi "Transaction rolled back" khi chưa có lệnh undo thực sự trên host.
- Khắc phục CompositeSourceKeyJsonConverter: bảo toàn toàn bộ mảng ProvenanceHandles qua chu trình serialize và deserialize JSON.

### R5. Chuẩn hóa Đo đạc Hình học & Nghiệm thu (P0/P1)
- Mở rộng kiểm tra hình học: Bổ sung kiểm tra cao độ cắt đỉnh cọc (Pile cutoff elevation); kiểm tra outline và thickness cho Profile/Slab; loại bỏ việc tự động Verified trên Generic category khi thiếu phép đo thực tế.
- Truy vấn và đối soát hình học chính xác theo mapped TargetUniqueId của từng cấu kiện.

## Acceptance Criteria

### Verification & Testing
- [ ] Multi-writer Journal Concurrency Test: Test 2 instance ghi đồng thời 1,000 unique intents; cold restart nạp lại đủ 1,000/1,000 intents không mất mát.
- [ ] Reconcile Persist-Before-Terminal Test: Test mô phỏng crash hoặc lỗi ghi ledger trong reconcile; verify journal không bị đánh dấu Committed khi ledger chưa lưu thành công.
- [ ] Dynamic DocumentVersion Resilience Test: Test kiểm chứng việc VersionGuid thay đổi sau khi apply không khiến recovery từ chối receipt hợp lệ của chính operation đó.
- [ ] Strict NoOp UniqueId & Exact Skip Test: Test kiểm chứng NoOp thiếu UniqueId bị từ chối; Skipped sai lý do bị chặn; defect sau dispatch báo OutcomeUnknown.
- [ ] ProvenanceHandles Preservation Test: Test chứng minh ProvenanceHandles được bảo toàn 100% roundtrip qua JSON serialization.
- [ ] Expanded Measured Geometric Test: Test kiểm chứng Pile thiếu cutoff elevation trả về Inconclusive.
- [ ] Zero Regressions: 100% tests toàn bộ repository PASS (3,779+ tests).

### Repository & Governance Rules
- [ ] Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file <= 300 dòng (Rule C2), method <= 50 dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2).
- [ ] HPBim.Core giữ thuần netstandard2.0, không reference Autodesk.Revit.*.
- [ ] Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.


## 2026-10-10T06:56:11Z

Xây dựng hệ thống Grasshopper MCP (HPGrasshopper) tích hợp trong tiến trình Revit 2026 thông qua Rhino.Inside.Revit (Rhino 8), cho phép AI coding agent điều khiển và đồng bộ hai chiều giữa Grasshopper canvas và Revit document qua giao thức Model Context Protocol (MCP) chuẩn.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper
Integrity mode: development

## Requirements

### R1. In-Process Dual-Lifecycle Bridge (Revit ↔ Rhino.Inside.Revit)
- Xây dựng `HPGrasshopper.McpBridge` (`net8.0-windows`) chạy bên trong Revit 2026 process có tích hợp Rhino.Inside.Revit.
- Thiết lập Named Pipe server `hpgrasshopper-mcp-2026` (sử dụng `HPRebar.McpBridge.Core`).
- Điều phối thực thi an toàn trên Main/UI Thread của Revit và Grasshopper qua cơ chế ExternalEvent hoặc dispatcher queue.
- Triển khai cơ chế an toàn 2 tầng (Dual-layer Safety): `Revit TransactionGroup` rollback cho `dryRun = true` khi can thiệp Revit elements + Grasshopper `GH_UndoRecord` cho các thao tác can thiệp canvas.
- Cung cấp giao diện trạng thái Modeless WPF Dialog hiển thị pipe status, log tương tác, danh sách công cụ và nút mở trên Revit Ribbon.

### R2. Bộ công cụ lõi Grasshopper (5 Core MCP Tools)
Triển khai 5 công cụ lõi tiêu chuẩn:
1. `read_grasshopper_canvas`: Đọc toàn bộ cấu trúc đồ thị hiện hành (danh sách component, parameters, input/output nickName, guid, toạ độ canvas, dây nối wires, trạng thái giải solver status, cảnh báo warnings/errors, giá trị tính toán tóm tắt).
2. `run_grasshopper_player`: Mở và thực thi file định nghĩa (.gh / .ghx) theo cơ chế GH Player (nhận tham số đầu vào inputs qua JSON, kích hoạt giải định nghĩa và trích xuất kết quả đầu ra outputs/geometries).
3. `batch_edit_grasshopper_canvas`: Thao tác đồ thị theo lô (thêm/xoá component theo GUID hoặc thư viện Grasshopper, kết nối/ngắt kết nối dây dẫn wires, cập nhật toạ độ và giá trị tham số inputs với cơ chế GH_UndoRecord).
4. `execute_grasshopper_code`: Trình thực thi mã C# Roslyn linh hoạt với các biến môi trường toàn cục (`doc` - Active GH_Document, `canvas` - GH_Canvas, `rhinoDoc` - RhinoDoc, `revitDoc` - Revit Document), hỗ trợ transaction tiers (None, Read, Write) và dryRun rollback.
5. `export_grasshopper_canvas_image`: Xuất ảnh chụp đồ thị canvas Grasshopper hiện hành (toàn bộ canvas hoặc vùng làm việc zoom) thành định dạng PNG/base64.

### R3. Tích hợp McpShared & HostProfile Grasshopper
- Mở rộng `McpShared` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`):
  - Định nghĩa `PipeNaming.GrasshopperHost = "hpgrasshopper-mcp"` và `JsonRpcMethods.GrasshopperPrefix`.
  - Cấu hình `GuardProfile.Grasshopper` và `AnalyzerProfile.Grasshopper` (cho phép namespace RhinoCommon, Grasshopper, RhinoInside.Revit, Autodesk.Revit).
  - Khai báo `HostProfile.Grasshopper` kế thừa chuẩn kiến trúc chung của repo.

### R4. Danh mục công cụ chuẩn 24 Tools & 12 Embedded Seeds
- MCP Server stdio `HPGrasshopper.Mcp.Server` (`net10.0-windows` / `net10.0`):
  - 5 Core tools (R2).
  - 8 Dynamic Registry meta tools (`search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `list_tools`, `get_tool_source`, `retire_tool`, `view_tool_docs`).
  - 12 Embedded Seed tools chuyên dụng cho Grasshopper ↔ Revit (ví dụ: tạo lưới trục, sinh dầm/cột parametric, curve-to-rebar, đồng bộ hình học bề mặt sang Revit Floor, phân tích panel hóa mặt đứng...).

### R5. Test Suites, Script Quality Checker & Live Harness
- `HPGrasshopper.McpBridge.Tests` (`net8.0-windows`): Unit test cho serializing canvas, binding parameters, undo/transaction scopes.
- `HPGrasshopper.Mcp.Server.Tests` (`net10.0`): Unit test cho stdio protocol, tool registry, và bộ kiểm tra chất lượng mã nguồn script ADR-0007 đối với 12 embedded seeds (quét không cho phép mã chết, empty catch, vượt 300 dòng).
- Bộ test fixture mẫu gồm các file `.gh` đơn giản (slider toán học, curve generator, Revit sync mock) phục vụ automated testing.
- Script kiểm thử tự động `tools/harness/run-live-verify.ps1` kiểm tra tương tác pipe và 24 công cụ.

## Acceptance Criteria

### Build & Integrity Criteria
- [ ] Toàn bộ solution `HPGrasshopper.slnx` biên dịch thành công 0 lỗi ở cả `Debug` và `Release`.
- [ ] Không sao chép trùng lặp các thư viện đã có trong `McpShared`, tuân thủ nguyên tắc tham chiếu tương đối duy nhất (`../McpShared/`).

### Protocol & Tool Catalog Criteria
- [ ] `HPGrasshopper.Mcp.Server` phản hồi `tools/list` qua JSON-RPC stdio đúng 24 tools với schema JSON chuẩn xác.
- [ ] Named Pipe listener `hpgrasshopper-mcp-2026` mở thành công và sẵn sàng đón nhận kết nối từ Server.

### Execution & Safety Criteria
- [ ] Lệnh `execute_grasshopper_code` biên dịch và thực thi thành công mã C# có gọi đồng thời RhinoCommon, Grasshopper API và Revit API.
- [ ] Lệnh `batch_edit_grasshopper_canvas` ghi nhận đầy đủ `UndoRecord` trong Grasshopper để người dùng có thể Ctrl+Z hoàn tác.
- [ ] `dryRun=true` thực hiện rollback hoàn toàn trạng thái, không để lại rác hình học hoặc giao dịch dở dang trong cả Revit và Grasshopper.

### Quality & Verification Criteria
- [ ] 100% unit tests trong `HPGrasshopper.McpBridge.Tests` và `HPGrasshopper.Mcp.Server.Tests` đều PASS.
- [ ] Toàn bộ 12 embedded seeds vượt qua bộ kiểm tra script-quality walker ADR-0007.


## 2026-10-10T13:10:58Z

Requested team: Đội ngũ đầy đủ (Full team) để phân tích và đồng thời xử lý các module UI (Column, Beam, Foundation, Kata, Bridge) song song

Chuyển đổi toàn diện hệ thống style giao diện của HPRebar và HPRebar.McpBridge sang sử dụng trực tiếp MaterialDesignInXamlToolkit (v5.3.2), loại bỏ các style/template/trigger tự chế thừa thãi và đồng bộ chuẩn Dark/Light với Revit.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: demo

## Requirements

### R1. Trực tiếp áp dụng MaterialDesignInXamlToolkit Controls
- Sử dụng trực tiếp style, template và typography của MaterialDesignInXamlToolkit (v5.3.2) cho các controls: Button, TextBox, ComboBox, CheckBox, RadioButton, TabControl, DataGrid, Expander, ScrollViewer, ToolTip trong `HPRebar/HPRebar` và `HPRebar/HPRebar.McpBridge`.
- Thay thế các alias/style HP bằng resource key chính thức của toolkit hoặc implicit styles. Loại bỏ hoàn toàn các style/alias trung gian khi không còn nơi sử dụng.

### R2. Loại bỏ các Style/Template/Trigger HP dư thừa & Bảo toàn cấu hình chức năng
- Gỡ bỏ các ControlTemplate, visual triggers và custom setters HP ghi đè hình thức mà toolkit đã hỗ trợ chuẩn.
- Bảo toàn 100% cấu hình phục vụ chức năng và nghiệp vụ: căn lề phải ô số, định dạng dữ liệu, validation rules/triggers, kích thước layout cần thiết, preview hình học và màu sắc kỹ thuật cốt thép.
- Giữ nguyên toàn bộ bindings, MVVM commands, text hiển thị tiếng Việt và vòng đời window.

### R3. Hệ thống Màu sắc, Theme Tokens và Chuyển đổi Dark/Light Runtime
- Dùng theme resources của toolkit cho background, foreground, border, card, popup và visual states; giữ primary/secondary colors của bộ nhận diện thương hiệu.
- Không dùng palette HP để ghi đè toàn bộ màu bề mặt của toolkit.
- Đảm bảo cơ chế hoán đổi overlay dictionary ở cấp window trong `MaterialThemeBridge` phản ánh tức thì khi chuyển đổi Dark/Light của Revit trên các cửa sổ đang mở.

### R4. Tuân thủ Ràng buộc Đóng gói In-Process CAD/BIM
- Duy trì cấu hình ILRepack và cơ chế cô lập MaterialDesign bên trong từng assembly (`HPRebar.dll`, `HPRebar.McpBridge.dll`).
- Bắt buộc dùng font `Segoe UI` (ghi đè `MaterialDesignFont`), tuyệt đối không tham chiếu pack URI của font Roboto.
- Không sử dụng `PaletteHelper` do môi trường Revit Add-in không có `Application.Current`.
- Giữ nguyên phiên bản package hiện tại `5.3.2` và phong cách MaterialDesign2 (không tự nâng cấp version hay chuyển MD3).
- Không tự ý commit, push git hoặc chỉnh sửa mô hình Revit.

### R5. Đồng bộ Theme Gallery và Kiểm thử Hệ thống
- Cập nhật công cụ `ThemeGallery` để hiển thị và kiểm tra trực quan các control theo style toolkit ở cả 2 chế độ Dark và Light.
- Cập nhật các bài test liên quan đến theme (`ThemeTokenCoverageTests`...) nhận diện đúng resource dictionaries mới mà không làm suy yếu tiêu chí kiểm thử.

## Acceptance Criteria

### Build & Package Verification
- [ ] Build thành công toàn bộ configurations `Debug.R23`, `Debug.R24`, `Debug.R25`, `Debug.R26`, `Debug.R27` với tham số `-p:DeployAddin=false`.
- [ ] Build thành công `HPRebar.McpBridge` trên cấu hình hỗ trợ (R25/R26).
- [ ] Không có lỗi runtime liên quan đến missing resource key, pack URI hoặc PackIcon.

### Functional & Visual Integrity
- [ ] Mọi cửa sổ (`ColumnRebar`, `BeamRebar`, `FoundationRebar`, `KataExport`, `KataRebar`, `McpBridgeStatusView`) hiển thị đầy đủ, không bị vỡ layout.
- [ ] Các trường nhập số vẫn giữ căn phải và font số phù hợp; validation lỗi hiển thị đúng trạng thái.
- [ ] Thao tác chuyển đổi Dark/Light mode cập nhật ngay lập tức trên cửa sổ đang mở mà không cần khởi động lại.
- [ ] Canvas/preview đồ họa cốt thép (2D/3D geometry) giữ nguyên màu sắc phân loại kỹ thuật.

### Quality & Governance
- [ ] Vượt qua toàn bộ checklist `CODE_REVIEW_CHECKLIST.md` với rule IDs được kiểm tra nghiêm ngặt.
- [ ] Báo cáo bàn giao chi tiết: danh sách file sửa đổi, bảng ánh xạ style HP sang Toolkit, ngoại lệ được giữ lại kèm lý do, kết quả build/test, và các phần chưa kiểm tra trực tiếp trong Revit ("CHƯA TEST").


## 2026-10-12T05:34:11Z

Khắc phục toàn diện các phát hiện kiểm toán (F1–F13) của hệ thống HPGrasshopper MCP theo kế hoạch bàn giao tại `plans/reports/261012-1229-hpgrasshopper-antigravity-handoff.md`, bảo đảm tính đúng đắn, an toàn 2 tầng (Revit + Grasshopper/Rhino), khả năng phục hồi (rollback/undo) và kiểm thử hồi quy độc lập.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper
Integrity mode: development

## Requirements

### R1. Native Routing & Error Dispatch (Nhóm A - F1, F11)
- Kết nối thông suốt 4 công cụ native (`read_grasshopper_canvas`, `run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `export_grasshopper_canvas_image`) từ server JSON-RPC qua `RequestDispatcher` sản xuất đến các handler tương ứng trong `HPGrasshopper.McpBridge`.
- Chuẩn hóa mã lỗi, map chính xác các ngoại lệ solve/runtime sang kết quả MCP, và cập nhật schema/tool definitions đồng bộ.
- Bảo đảm policy kiểm soát opt-in, từ chối lệnh ghi khi bridge ở trạng thái read-only.

### R2. An toàn giao dịch, Rollback & Queue Lifecycle (Nhóm B - F2–F7)
- Hoàn thiện cơ chế rollback an toàn 2 tầng cho `execute_grasshopper_code` và `batch_edit_grasshopper_canvas`, đảm bảo khi `dryRun = true` hoặc xảy ra lỗi/hủy bỏ, trạng thái của cả Revit Document và Grasshopper Canvas / RhinoDoc được phục hồi trọn vẹn (không báo `rolledBack: true` giả tạo).
- Thực thi nghiêm ngặt chế độ `transaction: none`, ngăn chặn các đoạn mã tự ý mở Transaction ghi dữ liệu trái phép.
- Cải tiến hàng đợi `GrasshopperBridgeCoordinator`: xử lý an toàn khi `ExternalEvent.Raise()` thất bại hoặc khi có yêu cầu hủy ngang (`CancellationToken`), tránh treo bridge.
- Xác thực danh tính tài liệu (Document/Definition Identity Check) trước khi thực thi để tránh chạy nhầm trên tài liệu khác.
- Đảm bảo trích xuất context (`get_grasshopper_context`) an toàn trên đúng UI/Main thread.

### R3. Chuẩn hóa Hợp đồng Seed Tools & Dữ liệu Thực thi (Nhóm C - F8–F11)
- Điều chỉnh hợp đồng và mô tả các seed tools (`facade_panelization`, `curtain_grid_layout`, `generate_stirrup_distribution`, `export_model_to_rhino`) phản ánh chính xác kết quả thực tế (tính toán/lập kế hoạch vs tạo đối tượng thực tế).
- Đảm bảo các tham số đầu vào cốt lõi thực sự chi phối kết quả đầu ra trong `generate_rebar_curves`, `create_floors_slabs`, `directshape_from_geometry`.
- Nâng cấp `CanvasEditor`: preflight từng thao tác trong batch, ngắt kết nối đúng port, báo cáo chi tiết index thao tác lỗi, không nuốt lỗi âm thầm.
- Chuẩn hóa `PlayerRunner` và `CanvasReader`: bảo toàn cấu trúc dữ liệu Data Tree, phân biệt NickName trùng lặp, xác thực kết quả giải trước khi báo thành công.

### R4. Cô lập Fixture & Kiểm thử Hồi quy (Nhóm D - F12)
- Tách biệt hoàn toàn việc sinh fixture thử nghiệm vào thư mục tạm, không ghi đè vào source files/harness definitions trong quá trình chạy test.
- Xây dựng bộ test hồi quy tự động độc lập cho các lỗi F1–F11, phân định rõ ràng giữa mock test, compilation test và live verification.

## Acceptance Criteria

### Routing & Protocol Criteria
- [ ] Cả 4 công cụ native (`read_grasshopper_canvas`, `run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `export_grasshopper_canvas_image`) gửi request từ `HPGrasshopper.Mcp.Server` đến `HPGrasshopper.McpBridge` đều được định tuyến đúng handler sản xuất và trả về kết quả hợp lệ (không gặp `-32601 MethodNotFound`).
- [ ] Gửi yêu cầu thay đổi đồ thị khi opt-in bị tắt sẽ bị từ chối với thông báo rõ ràng.

### Safety & Boundary Criteria
- [ ] Trong `execute_grasshopper_code`, khi chạy với `dryRun: true` hoặc có lỗi ném ra, cả Revit elements và Grasshopper canvas objects đều giữ nguyên trạng thái trước khi gọi; cấm trả về `rolledBack: true` nếu chỉ có một bên được khôi phục.
- [ ] Các script chạy dưới chế độ `transaction: none` cố tình mở Transaction Revit sẽ bị chặn hoặc từ chối trước khi thay đổi dữ liệu.
- [ ] Khi một request đang trong hàng đợi bị hủy (`cancel_execution` hoặc token canceled), hàng đợi được giải phóng ngay lập tức trong deadline mà không làm treo bridge hay giữ trạng thái busy.

### Contract & Data Integrity Criteria
- [ ] Seed tools phản ánh trung thực kết quả: không báo thành công tạo đối tượng nếu chỉ tính toán metadata; các tham số đầu vào (như đường kính thép, cờ structural) phải tạo ra sự thay đổi tương ứng trên đối tượng Revit.
- [ ] `batch_edit_grasshopper_canvas` phát hiện và báo cáo chính xác index cùng lý do khi gặp component/port không hợp lai; không áp dụng thay đổi dở dang khi cam kết tính nguyên tử (atomic).
- [ ] `run_grasshopper_player` phát hiện và báo lỗi khi đồ thị gặp lỗi runtime solver thay vì trả về success.

### Test & Build Integrity Criteria
- [ ] 100% tests trong `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.McpBridge.Tests` và `HPGrasshopper.E2E.Tests` đều PASS.
- [ ] Toàn bộ 12 embedded seeds vượt qua bộ kiểm tra script-quality walker ADR-0007.
- [ ] Chạy test suite không làm thay đổi bất kỳ file fixture nào trong mã nguồn.
- [ ] Tuân thủ hướng phụ thuộc `HPGrasshopper` -> `McpShared`, không làm biến đổi tính host-neutral của `McpShared`.


## Follow-up — 2026-10-12T11:33:22Z

Hệ thống vừa khởi động lại sau server restart. Người dùng yêu cầu 'tiếp tục'. Hãy tiếp tục chu trình điều phối teamwork tại Milestone M3 (worker_m3_iter2 / Gate M3 Iteration 2) và các mốc tiếp theo theo đúng kế hoạch của 261012-1229-hpgrasshopper-antigravity-handoff.md.


## 2026-10-12T15:21:07Z

Khắc phục triệt để các phát hiện kiểm toán lại (R1–R9) của hệ thống HPGrasshopper MCP theo kế hoạch tại `plans/reports/261012-2212-hpgrasshopper-reaudit.md`, thống nhất lifecycle thực thi giữa public native RPC và execute engine, bảo đảm an toàn giao dịch, rollback trung thực, preflight toàn diện và dữ liệu Data Tree chuẩn xác.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper
Integrity mode: development

## Requirements

### R1. Thống nhất Execution Lifecycle, Error Mapping & Document Identity (R1, R2, R5)
- Hợp nhất vòng đời admission/execution cho cả public native RPC (`run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `read_grasshopper_canvas`, `export_grasshopper_canvas_image`) và script execution qua một cơ chế chung trong `Coordinator`/`Dispatcher`.
- Gắn đầy đủ request/session ID, linked `CancellationToken`, deadline và xử lý hủy ngang không để treo cờ bận (`IsBusy`) khi client timeout hoặc host không tick.
- Chuẩn hóa ánh xạ mã lỗi: khi native tool thất bại (file không tồn tại, preflight fail, solve error, missing output), kết quả MCP phải trả về `isError: true` thay vì `CallToolResult.IsError = false`.
- Thực thi nghiêm ngặt xác thực danh tính tài liệu (`TargetDocTitle`, `TargetDocPath`, `TargetDefId`) trên mọi public route trước khi thực hiện thao tác ghi; từ chối khi tài liệu active bị thay đổi hoặc đóng giữa chừng.

### R2. Thứ tự Commit Đồng Bộ & Hoàn Thiện Canvas Rollback Ledger (R3, R4)
- Điều chỉnh thứ tự hoàn tất giao dịch: canvas undo scope chỉ được commit sau khi Revit transaction đạt kết quả cuối cùng (`final outcome`), đảm bảo nếu Revit commit thất bại hoặc deadline tới thì toàn bộ thay đổi trên Grasshopper canvas cũng được khôi phục nguyên vẹn.
- Hoàn thiện ledger trong `CanvasUndoScope.cs`: phân biệt giữa đối tượng gốc bị xóa và đối tượng mới thêm trong scope để khi rollback thao tác "thêm rồi xóa trong cùng một scope" không bị tái tạo nhầm đối tượng; tạo snapshot `GH_WireAction` trước khi thực hiện mutation trên wire.
- Báo cáo trung thực trạng thái khôi phục (`rolledBack`), không nuốt lỗi rollback hay báo clean rollback khi việc phục hồi chỉ diễn ra một phần.

### R3. Preflight Toàn Diện, Data Tree & Chuẩn Hóa Domain Seeds (R6, R7, R8, R9)
- Nâng cấp `CanvasBatchValidator.cs`: kiểm tra preflight đối chiếu với số lượng port thực tế của component trên đồ thị, phát hiện và từ chối các wire tham chiếu port vượt giới hạn hoặc component bị xóa trong cùng batch; từ chối thay vì âm thầm bỏ qua.
- Hoàn thiện `PlayerDataTreeHelper.cs` và `PlayerRunner.cs`: phân định rõ ràng giữa mảng số và tọa độ 3D theo kiểu tham số mục tiêu, báo cáo danh sách inputs bị từ chối/không khớp (`rejectedInputs`), xuất đúng kiểu GUID/element references, và bắt lỗi solver runtime thực chất.
- Chuẩn hóa seed tools: áp dụng chính sách sai số (tolerance) khi tìm đường kính thép trong `generate_rebar_curves`, xử lý vector pháp tuyến khi có các đoạn collinear trong mặt phẳng nghiêng/đứng; đồng bộ mô tả trong `tool.json` đúng với năng lực thực tế của `directshape_from_geometry` và `query_revit_geometry`.
- Khắc phục `GrasshopperContextExtractor.cs` và `InvalidateCompiler`: phân biệt rõ giữa trạng thái context thực tế và dữ liệu mặc định khi UI bận, kết nối cơ chế xóa cache compiler vào production lifecycle khi references thay đổi.

### R4. Kiểm Thử Hồi Quy Public-Route & Cập Nhật Tài Liệu Kỹ Năng
- Xây dựng bộ test hồi quy gọi trực tiếp qua public route (xuyên qua server JSON-RPC và dispatcher) với các kịch bản file thiếu, preflight fail, solve error, và cancellation giữa chừng.
- Đồng bộ hóa file hướng dẫn kỹ năng `.agents/skills/hp-mcp-grasshopper/SKILL.md` khớp chính xác với 27 công cụ thực tế, schema tham số và các cam kết an toàn.

## Acceptance Criteria

### Execution Lifecycle & Error Criteria
- [ ] Mọi cuộc gọi public native RPC (`run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `read_grasshopper_canvas`, `export_grasshopper_canvas_image`) đều chia sẻ chung cơ chế quản lý timeout, linked token và release busy flag khi bị hủy.
- [ ] Khi native handler trả về lỗi hoặc ngoại lệ (như file không tồn tại, preflight thất bại, solve error), MCP server trả về `isError: true` với mã lỗi và chi tiết rõ ràng.
- [ ] Request ghi dữ liệu gửi tới tài liệu/definition khác với tài liệu hiện hành bị từ chối với thông báo mismatch cụ thể.

### Rollback & Safety Criteria
- [ ] Khi script sửa đổi đồ thị Grasshopper và lệnh commit Revit thất bại (hoặc timeout/canceled), cả Revit elements và Grasshopper canvas đều được khôi phục về trạng thái ban đầu; cấm tình trạng Revit rollback nhưng canvas đã commit.
- [ ] Thao tác thêm rồi xóa component trong cùng một batch/scope khi dryRun/rollback không để lại đối tượng dư thừa trên canvas; snapshot wire được tạo trước khi thay đổi liên kết.
- [ ] Mọi trường hợp khôi phục thất bại hoặc khôi phục một phần đều được thể hiện trung thực trong output, không báo thành công giả tạo.

### Data & Preflight Criteria
- [ ] `batch_edit_grasshopper_canvas` từ chối toàn bộ batch nếu có wire nối tới port index không tồn tại trên component; không âm thầm bỏ qua wire lỗi.
- [ ] `run_grasshopper_player` không tự động chuyển mảng 3 số nguyên thành Point3d nếu tham số mục tiêu là kiểu số; báo cáo rõ ràng các tham số đầu vào bị từ chối.
- [ ] `generate_rebar_curves` từ chối hoặc cảnh báo khi không tìm thấy đường kính thép phù hợp trong khoảng sai số cho phép; xác định đúng mặt phẳng uốn rebar cho các đoạn dầm collinear.

### Quality & Governance Criteria
- [ ] 100% tests giải pháp HPGrasshopper PASS filter-free; 1,162 tests McpShared hồi quy PASS 100%.
- [ ] 0 file sửa đổi trong `McpShared/` (giữ nguyên vẹn tính host-neutral).
- [ ] Toàn bộ các file C# mới/sửa đổi tuân thủ nghiêm ngặt Rule C2 ($\le 300$ dòng) và Rule M3 ($\le 50$ dòng/method), 0 dead code (Q-B1), 0 unannotated catch (Q-B2).
- [ ] File `.agents/skills/hp-mcp-grasshopper/SKILL.md` được cập nhật đồng bộ 100% với danh mục 27 tools runtime.

## 2026-10-12T23:09:55Z

Khắc phục triệt để và toàn diện 10 phát hiện kiểm toán (N1–N10) của hệ thống HPGrasshopper MCP theo kế hoạch tại `plans/reports/261013-0600-hpgrasshopper-reaudit.md`, đảm bảo tính nguyên tử của giao dịch, báo cáo rollback trung thực, hủy bỏ an toàn trong batch solve, hoàn thiện ledger hoàn tác đối tượng baseline, bắt buộc danh tính tài liệu, bắt lỗi gán dữ liệu Player, bảo toàn output trùng tên, giữ nguyên diagnostics khi lỗi, chuẩn hóa schema container và cảnh báo stale context.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper
Integrity mode: development

## Requirements

### R1. Khắc phục các phát hiện ưu tiên P1 (N1–N5)
- **N1 — Tính nguyên tử của Seed Rebar (`generate_rebar_curves`)**: Validate trước toàn bộ curve sets (coplanarity, endpoints, bar type...) trước khi gọi tạo rebar; nếu có bất kỳ set nào không hợp lệ sau khi bắt đầu mutation thì phải throw exception để Revit Transaction tự động rollback toàn bộ, ngăn chặn commit một phần khi batch trả failure.
- **N2 — Kết quả Rollback Trung thực (`RevitTransactionScope`)**: Kiểm tra và kết hợp kết quả `CanvasRollbackResult` từ `ICanvasUndoScope` vào `ExecutionOutcome`. Nếu canvas rollback thất bại hoặc chỉ phục hồi một phần, kết quả phải báo lỗi/partial với danh sách errors chi tiết, tuyệt đối không trả `RolledBack = true` hoặc `IsError = false` giả tạo.
- **N3 — Batch Solve trong Biên Giới An Toàn & Hỗ Trợ Cancellation (`CanvasEditor` & `GrasshopperExecutionEngine`)**: Truyền `CancellationToken` và scope vào `ExecuteBatch`; kiểm tra checkpoint token trước khi mutation và trước khi solve/commit; hoãn commit canvas đến khi có kết quả cuối cùng (align với Revit transaction boundary); không để batch tiếp tục commit khi đã có tín hiệu hủy/timeout.
- **N4 — Hoàn thiện Ledger Khôi Phục Đối Tượng Baseline (`CanvasUndoScope`)**: Xử lý chính xác chuỗi thao tác trên đối tượng baseline (đặc biệt là Remove rồi Add lại trong cùng một scope); phân biệt rõ giữa đối tượng gốc và đối tượng mới tạo để rollback không xóa mất đối tượng baseline; snapshot đầy đủ trạng thái trước khi thay đổi thuộc tính component hoặc liên kết dây.
- **N5 — Bắt Buộc Danh Tính Tài Liệu & Đồng Nhất Đường Dẫn (`McpBridgeWorkRequest` & Coordinator)**: Thiết lập `RequireTargetIdentity = true` trên các public route ghi dữ liệu khi có target identity; chuẩn hóa đường dẫn tài liệu nhất quán giữa coordinator cache, context extractor và validator (xử lý đồng bộ trường hợp tài liệu chưa lưu/Untitled hoặc null).

### R2. Khắc phục các phát hiện ưu tiên P2 (N6–N10)
- **N6 — Kiểm Tra Kết Quả Gán Dữ Liệu Player (`PlayerDataTreeHelper`)**: Kiểm tra giá trị Boolean trả về của từng lệnh `AddVolatileData`; stage/validate toàn bộ inputs trước khi gán; ghi nhận chi tiết các input bị từ chối vào `rejectedInputs`; không tự ý bỏ qua các phần tử null/unsupported làm lệch chỉ mục (index shift).
- **N7 — Bảo Toàn Dữ Liệu Output Trùng Tên (`PlayerOutputExtractor`)**: Khi có nhiều output trùng nickname trong danh sách requestedOutputs, bảo toàn toàn bộ giá trị tương ứng (dưới dạng list hoặc suffix GUID phân biệt) mà không ghi đè mất output trước đó.
- **N8 — Bảo Toàn Diagnostics Khi Lỗi & Chuẩn Hóa Casing JSON (`GrasshopperExecutionEngine`)**: Khi Player hoặc script ném lỗi dưới Revit, bảo tồn nguyên vẹn payload chẩn đoán gốc (errors, warnings, rejectedInputs, duration); sử dụng `BridgeJson.Options` (camelCase) khi serialize `JsonElement` trong `MapToExecuteResult` để đảm bảo tương thích client.
- **N9 — Kiểm Tra Schema Container Cho Batch Operations (`CanvasBatchValidator` & `CanvasEditor`)**: Phát hiện và báo lỗi preflight nếu các thuộc tính container danh sách (`componentsToAdd`, `wiresToConnect`, ...) được truyền vào sai định dạng (ví dụ: truyền JSON object/primitive thay vì JSON array), không âm thầm bỏ qua (silently no-op).
- **N10 — Đánh Dấu Stale / Không Khớp Context (`GrasshopperContextExtractor`)**: Đảm bảo snapshot context có tính nhất quán bất biến (immutable), bổ sung timestamp và đánh dấu rõ ràng trạng thái `isStale` hoặc `unavailable` khi không thể lấy snapshot mới từ UI thread; không trình bày các giá trị mặc định như dữ liệu thực tế.

### R3. Kiểm Thử Hồi Quy & Đảm Bảo Tính Toàn Vẹn Hệ Thống
- Mở rộng các bài kiểm thử unit và regression tests cho từng phát hiện N1–N10:
  - Test seed rebar với batch hợp lệ + không hợp lệ bảo đảm không commit một phần.
  - Test fake `ICanvasUndoScope` trả về failure bảo đảm `ExecuteResult` báo failure/partial trung thực.
  - Test cancellation token ngắt batch solve giữa chừng.
  - Test baseline remove → re-add giữ nguyên đối tượng sau rollback.
  - Test `AddVolatileData` thất bại được ghi nhận trong `rejectedInputs`.
  - Test requested outputs trùng nickname giữ trọn vẹn dữ liệu.
  - Test batch validator từ chối container sai kiểu (không phải array).
- Toàn bộ test suites hiện tại và mới phải PASS 100% filter-free.

### R4. Tuân Thủ Clean Code Governance & McpShared Host-Neutrality
- **Bảo toàn McpShared**: Tuyệt đối không chỉnh sửa bất kỳ file nào trong `McpShared/` (0 files modified).
- **Quy chuẩn mã nguồn**:
  - Rule C2: Mỗi file C# không vượt quá 300 dòng code.
  - Rule M3: Mỗi method/function không vượt quá 50 dòng code.
  - Rule Q-B1: 0 dòng mã chết (dead code / commented-out code).
  - Rule Q-B2: 0 catch block rỗng không có chú thích lý do.
  - ADR-0007: 12/12 embedded seeds vượt qua script-quality walker.

## Acceptance Criteria

### Execution, Rollback & Atomicity (N1–N4)
- [ ] Khi chạy `generate_rebar_curves` với danh sách có ít nhất một curve set không đồng phẳng (non-planar) hoặc không hợp lệ, toàn bộ transaction Revit bị rollback hoàn toàn và không có bất kỳ rebar nào được commit vào model.
- [ ] Khi `ICanvasUndoScope.Rollback()` trả về lỗi hoặc chỉ khôi phục một phần, `RevitTransactionScope` và `GrasshopperExecutionEngine` trả về kết quả phản ánh trung thực lỗi khôi phục, không trả về `RolledBack: true` đơn phương khi canvas chưa khôi phục sạch sẽ.
- [ ] Thao tác batch edit nhận `CancellationToken`; nếu token bị hủy trước hoặc trong quá trình solve/commit, canvas không tự ý commit và trả về kết quả hủy rõ ràng.
- [ ] Thao tác xóa một component baseline rồi thêm lại cùng component đó trong một batch/scope khi dryRun hoặc rollback sẽ phục hồi đúng trạng thái baseline ban đầu mà không làm biến mất component.

### Input, Output & Protocol Integrity (N5–N10)
- [ ] Các public route ghi dữ liệu kích hoạt kiểm tra danh tính tài liệu khi có target identity, từ chối thực thi khi không khớp tài liệu.
- [ ] `PlayerDataTreeHelper` kiểm tra kết quả `AddVolatileData`; nếu gán dữ liệu thất bại hoặc kiểu dữ liệu không tương thích, thông tin lỗi được ghi nhận vào `rejectedInputs` và không tính vào số lượng inputs gán thành công.
- [ ] `PlayerOutputExtractor` bảo toàn đầy đủ giá trị của các output trùng nickname trong `requestedOutputs`, không ghi đè làm mất dữ liệu của các output trước.
- [ ] Khi Player hoặc script thực thi gặp lỗi, payload chẩn đoán gốc (errors, warnings, rejectedInputs) được bảo toàn trong kết quả trả về cho client; JSON trả về từ bridge tuân thủ đúng camelCase casing.
- [ ] `CanvasBatchValidator` từ chối batch và báo lỗi cụ thể nếu các trường thao tác (`componentsToAdd`, `componentsToDelete`, ...) được cung cấp nhưng không phải là kiểu JSON array.
- [ ] Snapshot context cung cấp chỉ báo `isStale` / `timestamp` khi UI thread bận, không trộn lẫn giá trị mặc định giả định với danh tính tài liệu hiện hành.

### Verification, Quality & Governance
- [ ] 100% tests giải pháp HPGrasshopper PASS filter-free (`HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.E2E.Tests`).
- [ ] 1,162 tests McpShared hồi quy PASS 100% filter-free.
- [ ] 0 file sửa đổi trong `McpShared/`.
- [ ] Tuân thủ tuyệt đối Clean Code Governance (C2 $\le 300$ dòng, M3 $\le 50$ dòng, Q-B1, Q-B2, ADR-0007).

## Follow-up — 2026-10-13T05:13:46Z

Server vừa khởi động lại (restart). Người dùng đã yêu cầu "tiếp tục". Hãy kiểm tra lại trạng thái hiện tại (Milestone M1: worker_m1 đã xong, đang ở bước Review/Challenger/Audit) và tiếp tục thực hiện tự động toàn bộ kế hoạch khắc phục N1–N10 (Milestones M1 -> M2 -> M3 -> M4) cho đến khi hoàn tất và bàn giao. Bật lại các cron giám sát liveness/progress nếu cần thiết.


## 2026-10-16T23:55:10Z

Clean-room reverse-engineer the KataPro structural rebar detailing suite at `C:\kata_pro` into a comprehensive technical blueprint and build **HPKata** (`HPKata/HPKata.slnx`), a production-ready dual-host **AutoCAD 2026 (.NET 8)** and **Revit 2026 (.NET 8)** rebar detailing, dynamic tag-linking, scheduling, and 1D cutting-stock optimization system with two-way data compatibility with existing KataPro drawings and Excel templates.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Reference Material & Verification Resources

- **Target Artifacts to Inspect (Read-Only):**
  - `C:\kata_pro\Kata_pro64_Cad2025.dll` (.NET 8 AutoCAD 2025+ plugin, 4,276 TypeDefs, 33,792 MethodDefs)
  - `C:\kata_pro\Kata_Class_Lib.dll` (.NET Framework 4.8 shared library, 491 TypeDefs, 3,356 MethodDefs)
  - `C:\kata_pro\Kata_Class_Lib_Revit.dll` (.NET Framework 4.8 Revit integration library, 692 TypeDefs, 4,357 MethodDefs)
  - `C:\kata_pro\Goi lenh.lsp` (AutoCAD Lisp loader, >110 command aliases, and 86-line default configuration block at lines 152–237)
  - `C:\kata_pro\Iso_kata.dwg` (Block definitions, attributes, layers, dimension styles, text styles)
  - `C:\kata_pro\dangthep\dang00.wmf` .. `dang67.wmf` (68 standard rebar shape definitions)
  - `C:\kata_pro\Kata.xlsm`, `C:\kata_pro\Kata ENG.xlsm`, `C:\kata_pro\Iso_QHPlus.xls` (Excel scheduling, coupler, and QS templates)
  - `C:\kata_pro\Icon\Ribbon\Ribbon_Command_Clicks.txt` (Full Ribbon command inventory)
- **Repository Governance & Standards:**
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`, `docs/clean-code/host-appendix/revit.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`
  - Existing host patterns in `HPAutoCad/` and `HPRebar/`
  - Installed REA CLI (`C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd`) and `.NET` reflection/IL inspection tools

## Requirements

### R1. Complete Reverse-Engineering Blueprint (`docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`)
Perform a static, clean-room reverse-engineering audit of `C:\kata_pro` (strictly excluding `Security.dll` and any licensing/DRM mechanisms) and document:
1. Full architecture map across all 6 functional subsystems (Core Rebar & Setup, Beam Shop, Column/Wall Shop, Foundation/Slab/Stair, Rebar Schedule/Coupler/1D Cutting Stock, and QS Quantity Takeoff) and all >110 commands in `Goi lenh.lsp` and `Ribbon_Command_Clicks.txt`.
2. Exact DWG data schemas extracted from `Iso_kata.dwg` and the managed DLLs: XData `RegApp` names, `DxfCode` field ordering, Block & Attribute tag schemas, layer/style conventions, and the 86-line configuration schema in `Goi lenh.lsp`.
3. Mathematical catalog of all 68 rebar shapes (`dang00`..`dang67`), including segment parameter mappings, total length formulas, bend deduction / elongation rules, hook rules ($90^\circ, 135^\circ, 180^\circ$), lap splice rules, and the 1D cutting-stock optimization algorithm (`to_hop_thep` for standard 11,700 mm stock bars).
4. Interop data schemas for Revit (`Kata_Class_Lib_Revit.dll`), ETABS (`ETABSv1.dll`), and Excel (`Kata.xlsm`, `Iso_QHPlus.xls`).

### R2. Shared Domain Core & UI Library (`HPKata/HPKata.slnx` — `HPKata.Core` & `HPKata.UI`)
Create an independent solution `HPKata/HPKata.slnx` adhering to repository Clean Code rules:
1. `HPKata.Core` (host-agnostic C# library with zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`):
   - Complete implementation of the 68 rebar shapes (`dang00`..`dang67`), bend/hook/lap/anchorage calculators, and the 86-parameter Kata configuration parser/serializer.
   - Two-way serializer/deserializer for KataPro XData payloads and Block Attribute dictionaries so existing KataPro drawings can be read and updated losslessly.
   - Rebar schedule aggregation (`thong_ke_thep`), summary by diameter (`tong_hop_thep`), coupler schedule (`thong_ke_coupler`), 1D cutting-stock optimization engine (`to_hop_thep`), QR code tag generator (`in_tem_thep`), and dual Excel export (headless via `ClosedXML` + live COM interop compatible with `Kata.xlsm` / `Iso_QHPlus.xls`).
2. `HPKata.UI` (`net8.0-windows` WPF + `CommunityToolkit.Mvvm` + `MahApps.Metro`):
   - Host-theme-aware (Dark/Light) WPF dialogs preserving KataPro's functional parameter layout for Rebar Setup (`setupthep` / `setupkata`), Rebar Drawing & Editing (`ve_thep` / `sua_thep`), and Rebar Schedule / Cutting Optimization (`thong_ke_thep` / `to_hop_thep`).

### R3. AutoCAD 2026 & Revit 2026 Host Add-Ins — Phase 1 Vertical Slice (`HPKata.AutoCad` & `HPKata.Revit`)
Implement the Phase 1 Rebar Detailing, Dynamic Linking & Scheduling vertical slice across both hosts:
1. `HPKata.AutoCad` (AutoCAD 2026 `.NET 8` plugin + companion Lisp alias file `GoiLenh_HPKata.lsp`):
   - Setup & Rebar Creation/Editing commands: `setupthep`, `setupkata`, `ve_thep` (`vt`), `taothep`, `nhan_thep` (`nhan`), `sua_thep` (`sua`), `sua_moc_dai` (`smd`), `dai90`, `dai135`, `dai180`, `dai90135`, `neo_thep` (`neo`), `cat_thep` (`cut`), `cat_tuy_y` (`cattuyy`), `dimthep`.
   - Dynamic Rebar ↔ Tag Linking commands: `tao_lien_ket` (`link`), `copy_tao_lien_ket` (`cl`), `xoa_lien_ket` (`dl`), `xem_lien_ket` (`tim`), `xoa_highlight` (`xoa`), `update_thep` (`ud`), `ghi_thep` (`gt`), `stt0`, `stt1`, `timthep`, `check_lap`.
   - Schedule, Cutting Optimization & Export commands: `thong_ke_thep` (`tkt`), `sua_thong_ke` (`suatk`), `sstk`, `tong_hop_thep` (`ththep`), `thong_ke_coupler` (`tkco`), `to_hop_thep` (`tohopthep`), `toexcel`, `toexcelqh`, `in_tem_thep` (`itt`).
2. `HPKata.Revit` (Revit 2026 `.NET 8` add-in):
   - ExtensibleStorage / Parameter bridge mapping Revit `Rebar` elements to `HPKata.Core` rebar models and 68 shape codes.
   - Commands to configure rebar rules, inspect/tag rebar with KataPro-compatible marks, generate rebar schedules & coupler schedules, run 11.7m cutting-stock optimization, and export to Excel (`ClosedXML` / COM) or DWG schedule tables.

### R4. Safety, Clean-Room & Governance Constraints
1. Never decompile, copy, or circumvent `Security.dll`, hardware locks, or license checks in `C:\kata_pro`.
2. Treat `C:\kata_pro` as strictly read-only.
3. Comply with `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md` on all new C# files in `HPKata/`.

## Acceptance Criteria

### AC1. Reverse-Engineering Blueprint Completeness
- [ ] `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` exists and documents all 6 subsystems, >110 commands from `Goi lenh.lsp` / `Ribbon_Command_Clicks.txt`, exact XData RegApp/DxfCode schemas, Block/Attribute schemas from `Iso_kata.dwg`, all 68 rebar shapes (`dang00`..`dang67`), and the 86 configuration lines of `Goi lenh.lsp`.

### AC2. Solution Build & Architecture Isolation
- [ ] `dotnet build HPKata/HPKata.slnx` succeeds with `0 warnings` and `0 errors`.
- [ ] `HPKata.Core.csproj` has zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
- [ ] `HPKata.UI.csproj` uses WPF + `CommunityToolkit.Mvvm` + `MahApps.Metro` and builds cleanly on `net8.0-windows`.

### AC3. Automated Unit & Integration Tests (`HPKata.Core.Tests`)
- [ ] `dotnet test HPKata/HPKata.Core.Tests` passes 100% of tests (minimum 100 unit tests) covering:
  - All 68 rebar shapes (`dang00`..`dang67`) geometry & cut-length calculation.
  - Bend deduction, hook lengths ($90^\circ, 135^\circ, 180^\circ$), and lap splice formulas.
  - Round-trip parsing and serialization of the 86-line Kata configuration and KataPro XData/Block Attribute payloads.
  - 1D cutting-stock optimization (`to_hop_thep`) verifying bar conservation, waste percentage calculation, and valid cutting patterns for 11,700 mm stock length.
  - Headless Excel schedule & cutting report generation via `ClosedXML`.

### AC4. Host Verification (AutoCAD 2026 & Revit 2026)
- [ ] `Iso_kata.dwg` block definitions and attribute schemas are programmatically verified against `HPKata.AutoCad`'s reader/writer (via test or live AutoCAD 2026 MCP bridge `hprebar-autocad`).
- [ ] All Phase 1 AutoCAD commands (`vt`, `sua`, `link`, `ud`, `tkt`, `tohopthep`, `toexcel`, etc.) and Revit commands are registered and verified with zero runtime binding or guard errors.


## 2026-10-17T02:41:13Z

USER ALERT: The user reported Windows OS popups (`dotnet.exe - Unknown Hard Error` and `powershell.exe - A new guard page for the stack cannot be created`) caused by the 5 parallel Gate M3 subagents simultaneously spawning `dotnet` / `VBCSCompiler` / `MSBuild` / `fsi` / `powershell` processes.
I have shut down zombie `dotnet build-server` processes, added `<UseSharedCompilation>false</UseSharedCompilation>` and `<BuildInParallel>false</BuildInParallel>` to `HPKata/Directory.Build.props`, and sent a directive to `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`) requiring `--disable-build-servers -m:1` and forbidding concurrent `dotnet build` / `dotnet fsi` storms across parallel gate subagents. Ensure this constraint is enforced during monitoring and final audit.

## 2026-10-17T03:18:19Z

SYSTEM DIRECTIVE (Root Cause & TestResults Isolation):
Root cause of `Unknown Hard Error` popups found in Windows Event Log (Event ID 1025):
When multiple Gate subagents run `dotnet test --no-build` simultaneously, `Microsoft.Testing.Platform` collides on the shared log file `HPKata\HPKata.Core.Tests\bin\Debug\net8.0\TestResults\HPKata.Core.Tests_net8.0_x64.log` (`System.IO.IOException: The process cannot access the file ... because it is being used by another process`) and triggers `System.Environment.FailFast`, which generates Windows `Unknown Hard Error` popups for `dotnet.exe` and `HPKata.Core.Tests.exe`.

MANDATORY RULES:
1. Parallel Gate subagents MUST NEVER run `dotnet test` against the default `TestResults` directory concurrently. They must either:
   - Rely on the Worker's test run,
   - Run the test `.exe` directly, or
   - Pass a unique `--results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
2. The Victory Auditor MUST pass a unique `--results-directory` when running `dotnet test`.

## 2026-10-17T06:46:41Z

Continue the clean-room reconstruction of **KataPro (`C:\kata_pro`)** inside **`HPKata/HPKata.slnx`** by implementing **Phases 2, 3, 4, and 5** across **`HPKata.Core`**, **`HPKata.UI`**, **`HPKata.AutoCad`** (AutoCAD 2026 `.NET 8`), and **`HPKata.Revit`** (Revit 2026 `.NET 8`), building upon the completed **Phase 1** baseline (179 source files, 437 passing tests) and the reverse-engineering blueprint at `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Reference Material & Verification Resources

- **Completed Phase 1 Baseline & Reverse-Engineering Blueprints:**
  - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Full 10-section blueprint of `C:\kata_pro`, 6 subsystems, 193 `[CommandMethod]`s, 118 Lisp aliases, 108 Ribbon buttons, 85 rebar shapes, XData `"KATA"` schemas, ETABS/Revit/Excel interop)
  - `.agents/teamwork/survey_agent_1/survey_commands_config_report.md` (Complete command & config inventory)
  - `.agents/teamwork/survey_agent_2/survey_dwg_shapes_report.md` (`Iso_kata.dwg` block/attribute schemas & rebar shapes)
  - `.agents/teamwork/survey_agent_3/survey_scheduling_interop_report.md` (Scheduling, Excel VBA macros, ETABS & Revit interop)
  - `HPKata/HPKata.slnx` (Current Phase 1 implementation: `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests` — 437/437 tests passing)
- **Target Artifacts to Inspect (Read-Only, Decompile On-Demand with `ilspycmd`):**
  - `C:\kata_pro\Kata_pro64_Cad2025.dll` (Decompile specific Beam, Column, Wall, Foundation, Slab, Stair, and QS classes via `%USERPROFILE%\.dotnet\tools\ilspycmd.exe` when exact calculation or geometry layout details are needed)
  - `C:\kata_pro\Kata_Class_Lib.dll` & `C:\kata_pro\Kata_Class_Lib_Revit.dll`
  - `C:\kata_pro\Goi lenh.lsp`, `C:\kata_pro\Iso_kata.dwg`, `C:\kata_pro\Kata.xlsm`, `C:\kata_pro\Iso_QHPlus.xls`
- **Repository Governance & Build/Test Isolation Rules (CRITICAL):**
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`, `docs/clean-code/host-appendix/revit.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`
  - **NEVER remove or weaken** the 3 concurrency/MTP isolation properties in `HPKata/Directory.Build.props` (`<UseSharedCompilation>false</UseSharedCompilation>`, `<BuildInParallel>false</BuildInParallel>`, and `<TestingPlatformCommandLineArguments>`). Always pass `--disable-build-servers -m:1` to `dotnet build` and `dotnet test` commands to prevent `VBCSCompiler` contention and `Microsoft.Testing.Platform` log-file locks.

## Requirements

### R1. Phase 2 — Beam Shop Drawing & ETABS Integration (`Phân hệ Dầm & ETABS`)
1. **`HPKata.Core` (Beam Domain & ETABS Interop Engine):**
   - Multi-span continuous beam structural model (`BeamSpanModel`, `BeamSectionModel`, `BeamRebarLayout`, support/midspan top & bottom reinforcement zones, curtailment rules, stirrup spacing zones $L/4$, $L/2$, hanger bars, side face / skin reinforcement `thep_gia`, and main-secondary beam intersection `dam_giao` / `giao_dam` / `cat_dam`).
   - Foundation beam / tie beam detailing engine (`ve_dam_mong`, `ve_giang_mong`, `ve_giang_shop`), inclined beam (`ve_dam_xien`), and curved beam (`ve_dam_cong`) geometry & bar length calculations.
   - ETABS / SAP2000 import & design-to-detailing adapter (`EtabsBeamImportService`, parsing ETABS frame geometry, section dimensions, and longitudinal/shear reinforcement area `As_top`, `As_bot`, `Av/s` to auto-select rebar diameters and counts).
   - Beam rebar persistence (`Save_thep_dam`, `Add_thep_dam`) and beam schedule aggregation (`tkdam`, `quet_dam`).
2. **`HPKata.UI` (Beam & ETABS WPF ViewModels & Views):**
   - `BeamDetailingViewModel` / `BeamDetailingWindow` (Continuous beam spans, support/midspan bar configurations, section cuts, stirrup zones, shop mode vs design mode).
   - `EtabsImportViewModel` / `EtabsImportWindow` (Story/beam selection, rebar auto-sizing rules, preview table).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Beam Commands & Lisp Aliases):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_dam` (`vd`), `ve_dam_all` (`vda`), `ve_dam_shop` (`vds`), `ve_dam_all_shop` (`vdas`), `ve_dam_mc` (`vdmc`), `ve_damMC_all` (`vdmca`), `ve_dam_mong` (`vdm`), `ve_giang_mong` (`vgm`), `ve_giang_shop` (`vgms`), `ve_dam_xien` (`vdx`), `ve_dam_cong` (`vdc`), `dam_giao` (`damgiao`), `giao_dam` (`giaodam`), `cat_dam` (`catdam`), `raidam`, `tkdam`, `quet_dam` (`qd`), `quet_dam_all` (`qda`), `Save_thep_dam` (`std`), `Save_thep_dam_all` (`stda`), `Add_thep_dam` (`atd`), `show_thep_dam` (`shtd`), `Etabs`, `loadbeam`.
   - Revit commands/services: Read/generate multi-span `Structural Framing` (`OST_StructuralFraming`) beam reinforcement, ETABS beam rebar import, and beam shop schedule generation.

### R2. Phase 3 — Column & Shear Wall Shop Drawing (`Phân hệ Cột & Vách`)
1. **`HPKata.Core` (Column & Shear Wall Domain Engine):**
   - Multi-story column elevation & cross-section detailing engine (`ColumnStorySegment`, `ColumnSectionModel`, longitudinal bar splice zones, crank/offset bends at story transitions and column size step-downs, tie/crosstie arrangements, seismic confinement zones $l_0$).
   - Shear wall & core wall detailing engine (`ShearWallModel`, `WallBoundaryElementModel`, boundary zone confinement ties, double-curtain horizontal/vertical web reinforcement, wall openings, and U-bar edge hooks).
   - ETABS column & pier/wall import service (`loadcolumn`, `loadwall`).
2. **`HPKata.UI` (Column & Wall WPF ViewModels & Views):**
   - `ColumnDetailingViewModel` / `ColumnDetailingWindow` (Multi-story column geometry, longitudinal bars, tie patterns, splice/coupler options).
   - `WallDetailingViewModel` / `WallDetailingWindow` (Shear wall elevation, boundary elements `ve_loi_vach`, web mesh, shop curtailment).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Column & Wall Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_cot` (`vc`), `ve_cot_shop` (`vcs`), `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `ve_vach` (`vv`), `ve_vach_shop` (`vvs`), `ve_loi_vach` (`vlv`), `loadwall`.
   - Revit commands/services: `Structural Columns` (`OST_StructuralColumns`) and `Walls` (`OST_Walls`) rebar generation, boundary element confinement ties, and column/wall rebar scheduling.

### R3. Phase 4 — Foundation, Slab, Stair & Lintel Detailing (`Phân hệ Móng, Sàn, Thang, Lanh tô`)
1. **`HPKata.Core` (Foundation, Slab, Stair & Lintel Domain Engine):**
   - Isolated pad footing (`IsolatedFootingModel`, `ve_mong_don`), pile cap (`PileCapModel`, `ve_mong_coc`, stepped pile cap `ve_mong_coc_giat_cap`), and foundation plan layout (`ve_mat_bang_mong`, `rai_mong`).
   - Two-way slab reinforcement engine (`SlabPanelModel`, bottom/top mesh distribution, variable-length polyline slab panels `vesanpl`, 4-way/radial slab rebar `vesan4d`, chair/spacer bar calculation `thep_ke` / `cho_san`, slab panel splitting `chia_san`, and slab rebar block takeoff `thong_ke_block_san`).
   - Reinforced concrete staircase flight & landing detailing (`StairFlightModel`, `ve_thang`), lintel detailing (`LintelModel`, `velanhto`), and generic cross-section detailing (`vemc`).
2. **`HPKata.UI` (Foundation, Slab & Stair WPF ViewModels & Views):**
   - `FoundationDetailingViewModel` / `FoundationDetailingWindow` (Pad footings, pile caps, stepped pile caps).
   - `SlabStairDetailingViewModel` / `SlabStairDetailingWindow` (Slab top/bottom mesh, cranked slab bars, stair flight parameters, lintels).
3. **`HPKata.AutoCad` & `HPKata.Revit` (Foundation, Slab, Stair & Lintel Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `ve_mat_bang_mong` (`vmbm`), `rai_mong` (`rm`), `ve_mong_don` (`vmd`), `ve_mong_coc` (`vmc`), `ve_mong_coc_giat_cap`, `ve_san` (`vesan`), `vesanpl`, `vesan4d`, `ssan`, `pick_diem_san` (`dsan`), `chia_san` (`chiasan`), `thong_ke_block_san` (`tksan`), `ve_thang` (`vethang`), `velanhto`, `vemc`.
   - Revit commands/services: `Structural Foundations` (`OST_StructuralFoundation`), `Floors` (`OST_Floors`), and `Stairs` (`OST_Stairs`) rebar detailing & area reinforcement integration.

### R4. Phase 5 — Quantity Surveying (QS), Drawing Layout & Utility Commands (`Phân hệ Khối lượng QS & Tiện ích Bản vẽ`)
1. **`HPKata.Core` (QS Quantity Takeoff & Drawing Layout Engine):**
   - Concrete, formwork, and finish quantity takeoff engine (`QsTakeoffEngine`, `QsElementRecord`, structural framing plan takeoff `QS_mbkc`, column/pier vertical takeoff `QS_tru`, net vs gross deduction rules at beam-column-slab intersections, brick/tile grid layout `chia_tam` / `op_gach`, and Excel QS workbook export `xuatQS` / `tkqs`).
   - Drawing layout & sheet organization algorithms (`GridGenerator`, `DrawingSheetArranger` for `sap_ban_ve`, viewport layout `taomv` / `taomv1`, drawing index generator `danhmuc`, dimension alignment `loc_dim`, splice zone indicator `phan_vung_noi`, component name finder/renamer `tim_ten_ck` / `dten` / `tkten`).
2. **`HPKata.UI` (QS & Drawing Management WPF ViewModels & Views):**
   - `QsTakeoffViewModel` / `QsTakeoffWindow` (Concrete/formwork takeoff rules, intersection deduction toggles, summary preview, Excel export).
   - `DrawingUtilityViewModel` / `DrawingUtilityWindow` (Grid generator, sheet arrangement, batch print/PDF export `in_ban_ve` / `pdfkata`).
3. **`HPKata.AutoCad` & `HPKata.Revit` (QS & Utility Commands):**
   - AutoCAD commands & `GoiLenh_HPKata.lsp` aliases: `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`, `ve_grid` (`vg`), `chia_tam` (`chiatam`), `op_gach` (`opgach`), `loc_dim` (`locdim`), `phan_vung_noi` (`pvn`), `danhmuc`, `cat_doituong` (`cat`), `taomv`, `taomv1`, `sap_ban_ve` (`sbv`), `in_ban_ve` (`ibv`), `pukata`, `pdfkata`, `hideatt`, `Unhideatt`, `tim_ten_ck`, `dten`, `tkten`, `textblock`, `chuyen`, `show_thep_rai` (`str`).
   - Revit commands/services: Concrete & formwork quantity takeoff from Revit 3D elements (`OST_StructuralFraming`, `OST_StructuralColumns`, `OST_StructuralFoundation`, `OST_Floors`, `OST_Walls`) exported to Kata QS Excel format, plus sheet/viewport index helpers.

### R5. Clean-Room, Governance & Quality Constraints
1. Strictly read-only access to `C:\kata_pro`; never touch `Security.dll` or licensing/DRM code.
2. Every new or modified C# file in `HPKata/` must obey `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md`:
   - Max 300 lines per file (Rule C2), max 50 lines per method (Rule M3).
   - Zero dead/commented-out code (Rule Q-B1), zero empty catch blocks without reason comments (Rule Q-B2).
   - In `HPKata.Revit`, `304.8` must appear only inside `RevitUnitConverter.cs` (Rule R5).
3. All 437 existing Phase 1 tests must remain 100% green alongside the new Phase 2–5 unit, component, host, and E2E tests.

## Acceptance Criteria

### AC1. Solution Build & Clean Code Compliance
- [ ] `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` succeeds with `0 warnings` and `0 errors`.
- [ ] `HPKata.Core` has zero references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
- [ ] Every `.cs` file in `HPKata/` is $\le 300$ lines, with 0 dead code and 0 unannotated catch blocks.

### AC2. Phase 2 (Beam & ETABS) Completeness
- [ ] Continuous beam, foundation beam, inclined/curved beam, beam intersection (`dam_giao`, `giao_dam`, `cat_dam`), and ETABS beam import models and calculators are implemented in `HPKata.Core` + `HPKata.UI`.
- [ ] All 25 Phase 2 AutoCAD commands (`vd`, `vda`, `vds`, `vdas`, `vdmc`, `vdmca`, `vdm`, `vgm`, `vgms`, `vdx`, `vdc`, `damgiao`, `giaodam`, `catdam`, `raidam`, `tkdam`, `qd`, `qda`, `std`, `stda`, `atd`, `shtd`, `Etabs`, `loadbeam`) and Revit beam detailing/import commands are registered and tested.

### AC3. Phase 3 (Column & Shear Wall) Completeness
- [ ] Multi-story column detailing (splice zones, crank bends, confinement ties), shear wall / boundary element (`ve_loi_vach`) detailing, and ETABS column/wall import are implemented in `HPKata.Core` + `HPKata.UI`.
- [ ] All 10 Phase 3 AutoCAD commands (`vc`, `vcs`, `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `vv`, `vvs`, `vlv`, `loadwall`) and Revit column/wall commands are registered and tested.

### AC4. Phase 4 (Foundation, Slab, Stair & Lintel) Completeness
- [ ] Pad footing, pile cap, stepped pile cap, 2-way/polyline/4D slab reinforcement, stair flight, and lintel detailing calculators are implemented in `HPKata.Core` + `HPKata.UI`.
- [ ] All 15 Phase 4 AutoCAD commands (`vmbm`, `rm`, `vmd`, `vmc`, `ve_mong_coc_giat_cap`, `vesan`, `vesanpl`, `vesan4d`, `ssan`, `dsan`, `chiasan`, `tksan`, `vethang`, `velanhto`, `vemc`) and Revit foundation/slab/stair commands are registered and tested.

### AC5. Phase 5 (QS Quantity Takeoff & Drawing Utilities) Completeness
- [ ] Concrete/formwork QS takeoff engine (`QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`) with Excel export and drawing layout/utility services are implemented in `HPKata.Core` + `HPKata.UI`.
- [ ] All 27 Phase 5 AutoCAD commands (`QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`, `vg`, `chiatam`, `opgach`, `locdim`, `pvn`, `danhmuc`, `cat`, `taomv`, `taomv1`, `sbv`, `ibv`, `pukata`, `pdfkata`, `hideatt`, `Unhideatt`, `tim_ten_ck`, `dten`, `tkten`, `textblock`, `chuyen`, `str`), `GoiLenh_HPKata.lsp` aliases, and Revit QS takeoff commands are registered and tested.

### AC6. Comprehensive Test Suite & E2E Verification
- [ ] `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1` passes 100% of tests (expanding from 437 tests to $\ge 650$ tests across `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, and `HPKata.E2E.Tests`) with zero `Unknown Hard Error` popups or file-lock collisions.


## 2026-10-17T14:27:21Z

Migrate the entire `HPKata.UI` WPF dialog layer (shared by `HPKata.Revit` for Revit 2026 and `HPKata.AutoCad` for AutoCAD 2026) from `MaterialDesignThemes` to `MahApps.Metro` (`v2.4.11`) using `MetroWindow`, eliminate all `StaticResourceExtension` / `XamlParseException` runtime errors across all 13 dialogs, and upgrade the Revit rebar detailing commands to automatically read actual element dimensions ($b \times h \times L/H$) with safe manual-mode fallback.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Context & Existing Architecture

- Solution file: `HPKata/HPKata.slnx` (`.NET 8.0`, `net8.0-windows` for `HPKata.UI`, `HPKata.Revit`, `HPKata.AutoCad`, `HPKata.UI.Tests`, `HPKata.Revit.Tests`, `HPKata.AutoCad.Tests`).
- `MahApps.Metro` version `2.4.11` (and transitive `ControlzEx`) is already cached locally in `%USERPROFILE%\.nuget\packages\mahapps.metro\2.4.11`.
- Currently, `HPKata/HPKata.UI/Views/` contains 13 WPF Windows:
  1. `BeamDetailingWindow.xaml` (`.xaml.cs`)
  2. `ColumnDetailingWindow.xaml` (`.xaml.cs`)
  3. `WallDetailingWindow.xaml` (`.xaml.cs`)
  4. `FoundationDetailingWindow.xaml` (`.xaml.cs`)
  5. `SlabDetailingWindow.xaml` (`.xaml.cs`)
  6. `StairLintelDetailingWindow.xaml` (`.xaml.cs`)
  7. `EtabsImportWindow.xaml` (`.xaml.cs`)
  8. `QsTakeoffWindow.xaml` (`.xaml.cs`)
  9. `DrawingUtilityWindow.xaml` (`.xaml.cs`)
  10. `CuttingStockView.xaml` (`.xaml.cs`)
  11. `DetailingView.xaml` (`.xaml.cs`)
  12. `ScheduleView.xaml` (`.xaml.cs`)
  13. `SetupView.xaml` (`.xaml.cs`)
- Currently, opening `BeamDetailingWindow` or `ColumnDetailingWindow` in Revit throws `'Provide value on 'System.Windows.StaticResourceExtension' threw an exception.' Line number '75' and line position '31'` because `Theme.xaml` lacks explicit `x:Key` definitions for `StandardDataGrid`, `Button.Primary`, `Button.Secondary`, `GroupBox.Header`, and `Text.Label`, and needs assembly preloading before `InitializeComponent()` when hosted in-process inside Revit/AutoCAD.

## Requirements

### R1. Replace MaterialDesignThemes with MahApps.Metro 2.4.11 Across HPKata Projects
Remove `MaterialDesignThemes` and `MaterialDesignColors` from `HPKata/HPKata.UI/HPKata.UI.csproj`, `HPKata/HPKata.Revit/HPKata.Revit.csproj`, and `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`, and replace them with `<PackageReference Include="MahApps.Metro" Version="2.4.11" />`. Ensure zero remaining references to `MaterialDesignThemes` or `http://materialdesigninxaml.net/winfx/xaml/themes` in `HPKata/`.

### R2. Migrate Theme System & All 13 Windows to MahApps.Metro `MetroWindow`
1. Update `HPKata/HPKata.UI/Themes/MaterialBridge.xaml` (or MahApps theme bootstrap merged by `Theme.xaml`) and `HPKata/HPKata.UI/Themes/Theme.xaml` to merge the standard `MahApps.Metro` resource dictionaries:
   - `pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml`
   - `pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml`
   - `pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml`
2. In `HPKata/HPKata.UI/Themes/Theme.xaml`, define all style keys referenced by any of the 13 windows, built on top of `MahApps.Styles.*` and the `ThemeDark.xaml` / `ThemeLight.xaml` tokens:
   - `StandardDataGrid` (`TargetType="DataGrid"`, plus an implicit `<Style TargetType="DataGrid" BasedOn="{StaticResource StandardDataGrid}"/>`)
   - `StandardTextBox` (`TargetType="TextBox"`, plus an implicit `<Style TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}"/>`)
   - `StandardComboBox` (`TargetType="ComboBox"`, plus an implicit `<Style TargetType="ComboBox" BasedOn="{StaticResource StandardComboBox}"/>`)
   - `PrimaryButton` and `Button.Primary` (`TargetType="Button"`)
   - `SecondaryButton` and `Button.Secondary` (`TargetType="Button"`)
   - `Card.Panel` (`TargetType="Border"`)
   - `GroupBox.Header` (`TargetType="GroupBox"`, used on `<GroupBox Style="{StaticResource GroupBox.Header}">` in `FoundationDetailingWindow.xaml`, `SlabDetailingWindow.xaml`, `StairLintelDetailingWindow.xaml`)
   - `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label` (`TargetType="TextBlock"`, plus implicit `TextBlock` style)
3. Update `HPKata/HPKata.UI/Common/ThemeManager.cs`:
   - Add `EnsureAssembliesLoaded()` that forces `typeof(MahApps.Metro.Controls.MetroWindow).Assembly` and `typeof(ControlzEx.Theming.ThemeManager).Assembly` to load into the current `AssemblyLoadContext` prior to XAML parsing.
   - Update `Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)` and add `ApplyTheme(Window window, bool dark = true)` to switch between `Dark.Blue` and `Light.Blue` MahApps themes and overlay `ThemeDark.xaml` / `ThemeLight.xaml` tokens without any dependency on `MaterialDesignThemes`.
4. Convert all 13 windows in `HPKata/HPKata.UI/Views/*.xaml` and `*.xaml.cs` to inherit from `mah:MetroWindow` (`MahApps.Metro.Controls.MetroWindow`, `xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"`), configure clean Metro chrome (`GlowBrush="{DynamicResource Brush.Accent}"`, `BorderThickness="1"`, `TitleCharacterCasing="Normal"`), call `ThemeManager.EnsureAssembliesLoaded()` before `InitializeComponent()` in every constructor, call `ThemeManager.Apply(this, dark: true)` after `InitializeComponent()`, and ensure `OnRequestClose()` safely sets `DialogResult = true` when the window is shown modally (`ComponentDispatcher.IsThreadModal` or `try/catch` around `DialogResult = true`) before calling `Close()`.

### R3. Upgrade Revit Element Geometry Extraction & Safe Picker Fallback in `HPKata.Revit`
1. Create a helper `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs` (and unit-testable pure dimension extraction logic) that reads actual structural element dimensions ($b$, $h$, length $L$ / height $H$, converted from Revit internal feet to millimeters) from a selected Revit `Element` / `FamilyInstance` (using bounding box and common structural parameters `b`, `h`, `Width`, `Height`, `Depth`, `Length`).
2. Update `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, and `WallRebarCommands.cs` so that:
   - If the user has pre-selected an element in Revit (`uidoc.Selection.GetElementIds()`) or picks an element interactively, its element mark and real dimensions ($b \times h \times L/H$ in mm) populate the initial `Spans` / `Stories` / wall geometry in the ViewModel before opening the `MetroWindow`.
   - If the user presses `ESC` during interactive picking (`Autodesk.Revit.Exceptions.OperationCanceledException`), the dialog still opens in manual input mode rather than aborting silently.
   - When the user clicks the primary action button in the dialog (`ShowDialog() == true`) and a target Revit element is present, the command executes the Revit `Rebar` generation transaction cleanly.

### R4. Comprehensive STA Unit Tests for All 13 MetroWindows & StaticResource Parity
In `HPKata/HPKata.UI.Tests`, add an STA test suite (`Views/AllWindowsXamlInstantiationTests.cs`) that:
1. Instantiates all 13 `MetroWindow` classes on an STA thread (`InitializeComponent()` + `ThemeManager.Apply(window, dark: true)` and `ThemeManager.Apply(window, dark: false)`), asserting that every window is a `MahApps.Metro.Controls.MetroWindow` and throws zero XAML or resource lookup exceptions.
2. Scans all `HPKata/HPKata.UI/Views/*.xaml` files for `{StaticResource <Key>}` usages and verifies that every referenced key is defined either locally in the view's `<Window.Resources>` or in `HPKata/HPKata.UI/Themes/*.xaml`.

## Verification Resources

- Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1` to avoid MSBuild node contention on Windows:
  - `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
  - `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`
- Clean Code Governance constraints:
  - Every C# file in `HPKata/` must be <= 300 lines (Rule C2).
  - Every method must be <= 50 lines (Rule M3).
  - Zero compiler warnings (`TreatWarningsAsErrors=true`).

## Acceptance Criteria

### MahApps.Metro Migration & XAML Resource Integrity
- [ ] `HPKata.UI.csproj`, `HPKata.Revit.csproj`, and `HPKata.AutoCad.csproj` reference `MahApps.Metro` `2.4.11` and have zero references to `MaterialDesignThemes` or `MaterialDesignColors`.
- [ ] All 13 windows in `HPKata/HPKata.UI/Views/` inherit from `MahApps.Metro.Controls.MetroWindow` (`mah:MetroWindow`) and have zero `xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"` attributes.
- [ ] Every `{StaticResource ...}` key used in `HPKata/HPKata.UI/Views/*.xaml` (`StandardDataGrid`, `StandardTextBox`, `StandardComboBox`, `PrimaryButton`, `Button.Primary`, `SecondaryButton`, `Button.Secondary`, `Card.Panel`, `GroupBox.Header`, `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label`, plus local converters) resolves without exception.

### Revit Command Workflow & Auto-Deploy
- [ ] `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, and `WallRebarCommands.cs` populate ViewModel dimensions from a pre-selected or picked Revit element, allow opening the dialog in manual mode if `ESC` is pressed during picking, and honour `DialogResult == true`.
- [ ] `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` succeeds with `0 warnings, 0 errors` and deploys `MahApps.Metro.dll` and `ControlzEx.dll` alongside `HPKata.UI.dll` to `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit\` and `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\Contents\` (unless locked by a running host process).

### Automated Test Suite & Clean Code Compliance
- [ ] `AllWindowsXamlInstantiationTests` instantiates all 13 `MetroWindow` views on an STA thread (testing both Dark and Light theme application) and passes 100%.
- [ ] `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100% of tests across all test projects (`HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`).
- [ ] Every `.cs` file in `HPKata/` is <= 300 lines.


## 2026-10-17T16:06:44Z

Reverse-engineer the structural engineering ecosystem in `C:\Dce Pro` using REA into `docs/rea-dce-pro/`, and implement the clean-room C# rewrite in `HPDcePro/` (`HPDcePro.slnx`) with WPF MVVM (`MahApps.Metro`), completing Phase 1 end-to-end (2D geometry core replacing `DceAnalyssiss64.dll`, `.autostr` project I/O, `XData` backward compatibility, TCVN 5574:2018 RC beam calculation & detailing, rebar schedule & cutting optimization, `MayHoc/mauhoc.db` ML rebar suggestion, ETABS DTO bridge, and AutoCAD/Excel/Revit host projects) alongside the multi-phase architecture for Phases 2–6.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Verification Resources

- Shipped target binaries and data (read-only):
  - `C:\Dce Pro\DCE Pro.dll` (SHA-256: `8f7b56d1f916351fb91a8464628471499f59006ac3ba5e362b9df8dbc1225780`, REA Evidence ID: `ev_c1bcd4621c3710ffb087bed3d8120354cc071ae032c61a7ad12027fdc929983e`)
  - `C:\Dce Pro\DceAnalyssiss64.dll`
  - `C:\Dce Pro\DCE Pro.lsp`
  - `C:\Dce Pro\AutoStr\DuAnMau.autostr`
  - `C:\Dce Pro\MayHoc\mauhoc.db` (SHA-256: `74eb37741c999c677df271a6c8007d2d09b704fb16c7aff884f4f11ab624e1a8`, REA Evidence ID: `ev_5b5a0aff2c55fc84157b43574b7f79175f0dcda7e6d1ae96721a97d1d17a0241`)
  - `C:\Dce Pro\AddinSteel\DceSteel.dll`
  - `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`
  - `C:\Dce Pro\V.2020\DCE Excel.dll`
  - `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe`
  - `C:\Dce Pro\Excel\File V.2020\*.xlsm` & `C:\Dce Pro\Excel\DceSteel\*.xlsm`
- Repository governance & clean-code standards:
  - `AGENTS.md`
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`
  - `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Requirements

### R1. Comprehensive REA Reverse-Engineering Dossier (`docs/rea-dce-pro/`)
Produce a complete reverse-engineering technical specification in `docs/rea-dce-pro/` covering every subsystem in `C:\Dce Pro`:
- Managed & native binary inventory with REA Evidence IDs and SHA-256 digests (`DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, `DBIM_RVT_26.exe`, `MayHoc/mauhoc.db`).
- Decompiled domain architecture and algorithm specifications for all modules: RC Beam (`DceDamMatCatMoi`, `DceShopBeam`, `DuAnVeDam`), Column/Shear Wall (`DceColumn`, `DceWall`), Slab (`DceSlab`), Pile & Foundation (`DceMongCocMoi`, `DceVeCocBeTong`, `VeMongDon`), Stairs & Lintel (`DceThangMoi`), Rebar Schedule & Cutting Optimization (`Bea_ToiUuCatThepV2`, `RebarXdataDCE`, `DceRebarXDataTKT`), `AutoStr` 24-step automated structural workflow (`AutoStrStep01`..`AutoStrStep24`), `DceAiAgent` & `MayHoc` (`mauhoc.db` schema and ML pipeline), Steel Design (`DceSteel` & `DbimSteelAutocad`), and `DCE Excel` UDFs.
- Explicit mapping table from legacy classes/commands/LISP aliases (`DCE Pro.lsp`) to the new `HPDcePro` architecture, plus the staged implementation roadmap for Phases 1–6.

### R2. Independent Solution & Pure C# Domain Core (`HPDcePro/HPDcePro.Core`)
Create an isolated deliverable folder `HPDcePro/` with its own `HPDcePro.slnx`, `global.json` (pinned to `Microsoft.Testing.Platform`), and `Directory.Build.props` that never cross-references sibling product folders (except `../McpShared/` if needed):
- Implement pure C# 2D geometry (`Polygon2D`, polyline/segment chains, vector normalization, plane/origin projection, spatial grid) replacing the native P/Invoke calls to `DceAnalyssiss64.dll` (`ClassLibrary1.DAnalysisss`).
- Implement full serialization/deserialization for `.autostr` JSON project files (compatible with `C:\Dce Pro\AutoStr\DuAnMau.autostr`) and CAD `XData` schemas (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`).
- Implement pure C# engineering calculation engines (no background Excel COM dependency) for TCVN 5574:2018 RC Beam flexure/shear/crack/rebar layout, 1D Rebar Cutting Stock Optimization (`ToiUuCatThep` with commercial bar length 11700 mm and lap/anchorage rules), Rebar Schedule aggregation (`ThongKeThep`), SQLite reader/predictor for `MayHoc/mauhoc.db` (`MauHoc`, `LichSuSua`, `CauHinhModel`), and core calculation models for TCVN 10304:2025 pile capacity, TCVN 5575:2024 steel design, and TCVN 2737:2023 wind load.
- Completely exclude all legacy licensing, account, telemetry, and updater mechanisms (`LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`, `DbimTaiKhoanApi`, `DceUpdate`).

### R3. Multi-Host Adapters & MahApps.Metro WPF UI (`HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Revit`)
Build the host projects in `HPDcePro/HPDcePro.slnx`:
- `HPDcePro.AutoCad` + `HPDcePro.UI`: WPF MVVM user interface built with `MahApps.Metro` (`MetroWindow`, dark/light theme support, Beam Detailing & Rebar Cutting Optimization & AutoStr Project configuration views/viewmodels) and AutoCAD command adapters preserving legacy command names and LISP aliases from `DCE Pro.lsp` (`ShopDrawingBeam`, `VeDam_DCE`, `VeDamAll_DCE`, `VeMCD_DCE`, `VeBlockDam2`..`VeBlockSection`, `TKTSSOLUONGCAUKIEN`, `TKTSDUONGKINHTHEP`, `ToiUuCatThep`, `tkt_dce`, `stk_dce`, `TongHopThongKe_dce`, `XuatDuLieuModelEtabs`, `UpdateModelEtabs`, etc.) plus a clean `HPDcePro.lsp` loader.
- `HPDcePro.Excel`: Excel UDF / report export adapter providing backward-compatible calculation functions for `DCE Excel` / `DceSteel` workbooks and ClosedXML/CSV schedule export without Excel COM hangs.
- `HPDcePro.Revit`: Structural data exchange & rebar/framing adapter skeleton compatible with the repository's Revit architecture.

### R4. Comprehensive Automated Test Suite (`HPDcePro.Core.Tests`)
Create an xUnit v3 test project `HPDcePro.Core.Tests` running on `Microsoft.Testing.Platform` that thoroughly tests:
- 2D geometry operations and polygon/rebar spatial queries.
- Round-trip parsing of the real `C:\Dce Pro\AutoStr\DuAnMau.autostr` file and reading the real `C:\Dce Pro\MayHoc\mauhoc.db` SQLite database.
- `XData` packet encoding/decoding (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`) including malformed/corrupted payload recovery.
- TCVN 5574:2018 RC beam calculation (happy path, minimum/maximum reinforcement ratio edge cases, double reinforcement, stirrup spacing) and 1D rebar cutting optimization (`ToiUuCatThep` including bars exceeding 11.7 m requiring lap splices).
- TCVN 10304:2025 pile bearing capacity, TCVN 5575:2024 steel section checks, and TCVN 2737:2023 wind load calculation formulas.

### R5. Controlled Environment & Clean Code Governance
- Treat `C:\Dce Pro` strictly as read-only input; never modify or delete any file in `C:\Dce Pro`.
- All new C# code must pass the rules in `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md` (no empty catch blocks without reason comments, no commented-out code, no methods or types violating clean-code modularity).

## Acceptance Criteria

### REA Reverse-Engineering Dossier (`docs/rea-dce-pro/`)
- [ ] `docs/rea-dce-pro/README.md` and companion module documents exist and cite verified REA Evidence IDs and SHA-256 digests for `DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, `DBIM_RVT_26.exe`, and `MayHoc/mauhoc.db`.
- [ ] Every command in `C:\Dce Pro\DCE Pro.lsp` and all 24 `AutoStrStep01`..`AutoStrStep24` classes are documented with their inputs, outputs, and `HPDcePro` target mapping.

### Build & Clean Code Compliance
- [ ] `dotnet build HPDcePro/HPDcePro.slnx` succeeds with `0 errors` and `0 warnings`.
- [ ] `HPDcePro.UI` / `HPDcePro.AutoCad` references `MahApps.Metro` and provides MVVM Views/ViewModels for Beam Detailing, Rebar Schedule/Cutting Optimization, and `.autostr` Project Configuration.
- [ ] Zero references to legacy licensing/telemetry/updater endpoints exist anywhere in `HPDcePro/`.

### Automated Verification (`HPDcePro.Core.Tests`)
- [ ] `dotnet test HPDcePro/HPDcePro.Core.Tests` succeeds with 100% passing tests (at least 80 unit/integration tests covering geometry, `.autostr` real-file parsing, `mauhoc.db` real SQLite inspection, `XData` serialization, TCVN 5574:2018 beam design, TCVN 10304:2025 pile SPT capacity, TCVN 5575:2024 steel checks, and 1D cutting stock optimization).


## 2026-10-17T22:07:16Z

Continue with Wave 2: Implement Phases 2 through 6 of `HPDcePro` (`HPDcePro.slnx`) end-to-end on top of the completed Phase 1 baseline.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Verification Resources

- Existing Phase 1 baseline (118 passing tests, 0 warnings, 0 errors):
  - `HPDcePro/HPDcePro.slnx` (`HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Revit`, `HPDcePro.Core.Tests`, `HPDcePro.Adapters.Tests`)
  - REA Reverse-Engineering Dossier in `docs/rea-dce-pro/` (`README.md`, `01-domain-architecture.md`, `02-autostr-and-mayhoc.md`, `03-steel-and-excel.md`, `04-command-mapping-and-roadmap.md`)
- Shipped target binaries and reference data (strictly read-only):
  - `C:\Dce Pro\DCE Pro.dll`, `C:\Dce Pro\DceAnalyssiss64.dll`, `C:\Dce Pro\DCE Pro.lsp`
  - `C:\Dce Pro\AutoStr\DuAnMau.autostr`
  - `C:\Dce Pro\MayHoc\mauhoc.db`
  - `C:\Dce Pro\AddinSteel\DceSteel.dll`, `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`, `C:\Dce Pro\V.2020\DCE Excel.dll`
- Clean Code Governance:
  - `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Requirements

### R1. Phase 2 — Continuous Beam Detailing, Drafting Primitives & ETABS Bridge (`HPDcePro.Core` & `HPDcePro.AutoCad`)
- Implement `ContinuousBeamCalculator`, `FoundationBeamCalculator`, and `CurvedBeamCalculator` in `HPDcePro.Core` supporting multi-span moment redistribution, top/bottom bar curtailment, anchorage/lap zones, and concentrated stirrup zones at primary-secondary beam intersections (`BeamTrimmerV2` / `dam_giao`).
- Implement a host-independent 2D CAD Drafting Primitive Engine (`CadDraftingBuffer`, `CadEntityPrimitive` for lines, polylines, circles, hatches, dimensions, block inserts, text tags, and attached `XDataPacket`s) and `BeamShopDrawingGenerator` producing complete longitudinal elevations and cross-sections (1-1, 2-2) with `RebarXdataDCE` metadata.
- Implement `EtabsModelSynchronizer` in `HPDcePro.Core` for bidirectional story/grid/frame section/force and required reinforcement ($A_s$) exchange.

### R2. Phase 3 — Column & Shear Wall Detailing + Biaxial P-M-M Engine (`HPDcePro.Core` & `HPDcePro.UI`)
- Implement `ColumnPmmInteractionCalculator` under TCVN 5574:2018 (axial-biaxial bending $N$-$M_x$-$M_y$ capacity, slenderness buckling $\eta$, and utilization ratio) and `ColumnDetailingGenerator` (multi-story column splice zones $H_n/3..2H_n/3$, 1:6 cranked bars, rectangular single/multi-tie `ColumnREC1`/`ColumnREC2`, and circular `ColumnCi` cross-sections).
- Implement `ShearWallCalculator` (`VeVachDcePro`) for core wall boundary elements, web vertical/horizontal reinforcement, and shear check under TCVN 5574:2018.
- Add `ColumnWallDetailingWindow` and `ColumnWallDetailingViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.

### R3. Phase 4 — Substructure (Pile Cap & Pad Footing), Two-Way Slab, Stairs & Lintel (`HPDcePro.Core` & `HPDcePro.UI`)
- Implement `PileCapCalculator` (`VeMongCoc_DCE` — 1 to N pile layouts, individual pile reactions under axial load + biaxial moment, punching shear along pile cap pyramid, one-way shear, flexural rebar, and pile coordinate schedule `XuatToaDoCoc`) and `PadFootingCalculator` (`VeMongDon_DCE` — base pressure check, uplift check, punching shear, and two-way bending).
- Implement `TwoWaySlabCalculator` (`VeThepSanTopX/Y`, `VeThepSanBotX/Y`, `ThongKeThepSan` — two-way slab strip moments, crack/deflection checks, and top/bottom bar layout).
- Implement `StaircaseCalculator` (`VeThang_DCE` — flight slab `EngineBanThang` and zig-zag `EngineZigZac` rebar geometry & schedule) and `LintelCalculator` (`VeBlockLanhTo`, `VeMatCatLanhTo`).
- Add `FoundationSlabWindow` and `FoundationSlabViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.

### R4. Phase 5 — Full 24-Step `AutoStr` Pipeline Orchestrator & Quantity Surveying BOQ (`HPDcePro.Core` & `HPDcePro.Excel`)
- Implement `AutoStrOrchestrator` and all 24 step processors (`AutoStrStep01`..`AutoStrStep24`) in `HPDcePro.Core/AutoStr/` driven by `AutoStrConfig` (`.autostr`), including input validation, step progress/result tracking, and the automated iterative resizing loop (auto-increasing column section / pile count when capacity ratio > 1.0 as configured by `CotChoTangTietDien` and `CocTuTangKhiKhongDat`).
- Implement `QuantityTakeoffCalculator` computing Concrete volume ($m^3$), Formwork area ($m^2$), and Rebar tonnage ($kg$) by element type and story, and expand `ClosedXmlReportExporter` in `HPDcePro.Excel` to generate full multi-sheet calculation & BOQ workbooks.

### R5. Phase 6 — TCVN 5575:2024 Steel Connections, Full CAD Command Execution & Revit Rebar Integration
- Implement `PreGableFrameCalculator` (portal frame & purlin layout), `SteelMomentConnectionCalculator` (TCVN 5575:2024 bolted end-plate moment connection, flange/web welds, bolt shear/tension interaction, stiffeners), and `AnchorBoltBasePlateCalculator` (`DetailAnchorBolt`), plus `SteelConnectionWindow` and `SteelConnectionViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.
- Upgrade all command handlers in `HPDcePro.AutoCad/Commands/` (`BeamCommands`, `ColumnCommands`, `SlabCommands`, `FootingCommands`, `RebarCommands`, `AutoStrCommands`) so they execute real domain calculation, drafting primitive generation, and UI workflows instead of placeholder messages.
- Expand `HPDcePro.Revit` with `RevitRebarPlacementService` mapping `HPDcePro.Core` beam/column/footing detailing models to Revit structural framing and rebar descriptors.

## Acceptance Criteria

### Build & Clean Code Governance
- [ ] `C:\Dce Pro` remains 100% untouched (0 files modified, created, or deleted).
- [ ] `dotnet build HPDcePro/HPDcePro.slnx -c Debug` succeeds with `0 errors` and `0 warnings` (`TreatWarningsAsErrors=true`).
- [ ] Every C# file in `HPDcePro/` strictly obeys `HP_CLEAN_CODE_CORE.md` ($\le 300$ lines per file, methods $\le 50$ lines, 0 commented-out code, 0 unannotated empty catch blocks).

### Complete Phases 2–6 Capabilities & UI
- [ ] `HPDcePro.Core` contains working calculators and drafting generators for Continuous/Curved/Foundation Beams, Column P-M-M & Shear Wall, Pile Cap & Pad Footing, Two-Way Slab, Staircase & Lintel, the 24-Step `AutoStrOrchestrator` with iterative auto-sizing, Quantity Takeoff BOQ, and TCVN 5575:2024 Steel Moment Connections & Base Plates.
- [ ] `HPDcePro.UI` provides `MahApps.Metro` windows and viewmodels for `BeamDetailingWindow`, `CuttingOptimizationWindow`, `AutoStrConfigWindow`, `ColumnWallDetailingWindow`, `FoundationSlabWindow`, and `SteelConnectionWindow`.
- [ ] All 70 AutoCAD commands in `HPDcePro.AutoCad` execute real domain services, drafting generators, or `MahApps.Metro` dialogs.

### Automated Verification (`HPDcePro.Core.Tests` & `HPDcePro.Adapters.Tests`)
- [ ] `dotnet test HPDcePro/HPDcePro.slnx -c Debug` passes 100% with **at least 200 automated tests** (`0 failed, 0 skipped`) covering all Phases 1–6 calculators, 2D drafting primitive generation, the 24-step `AutoStrOrchestrator` end-to-end execution on `DuAnMau.autostr`, Excel multi-sheet BOQ/report export, `MahApps.Metro` ViewModels/Windows, Revit rebar mapping, and AutoCAD command execution.


## 2026-10-18T00:11:55Z

Continue with Wave 3: Complete MCP Integration (`HPDcePro.Mcp` + `.agents/skills/hp-dce-pro/SKILL.md`), `DbimSteelAutocad` Steel CAD Drafting Parity (`SteelFrameDraftingGenerator` + `SteelCadCommands`), Full Excel UDF Coverage (`DCE Excel` & `DceSteel` parity), Unified `MainControlCenterWindow` (`GETFORM` Hub), and AutoCAD `.bundle` Packaging (`HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml`).

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Verification Resources

- Existing Wave 2 baseline (280 passing tests, 0 warnings, 0 errors):
  - `HPDcePro/HPDcePro.slnx`
  - `docs/rea-dce-pro/03-steel-and-excel.md` & `docs/rea-dce-pro/04-command-mapping-and-roadmap.md`
- Shipped target binaries (strictly read-only):
  - `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`
  - `C:\Dce Pro\AddinSteel\DceSteel.dll`
  - `C:\Dce Pro\V.2020\DCE Excel.dll`
- Clean Code Governance:
  - `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Requirements

### R1. MCP Tool Catalog & Agent Skill (`HPDcePro.Mcp` & `.agents/skills/hp-dce-pro/SKILL.md`)
- Create `HPDcePro/src/HPDcePro.Mcp/HPDcePro.Mcp.csproj` (`net8.0`) and add it to `HPDcePro/HPDcePro.slnx`, providing a structured JSON-RPC / MCP tool dispatcher (`DceProMcpToolCatalog` & `DceProMcpToolExecutor`) exposing 8 comprehensive tools over `HPDcePro.Core` and `HPDcePro.Excel`:
  1. `dce_design_beam` (TCVN 5574:2018 single/continuous/foundation/curved beam design + shop drawing primitive summary)
  2. `dce_design_column_wall` (TCVN 5574:2018 biaxial $P$-$M_x$-$M_y$ column & shear wall boundary element check)
  3. `dce_design_foundation_pile` (TCVN 10304:2025 SPT/friction pile capacity, pile cap & pad footing design)
  4. `dce_design_slab_stair_lintel` (Two-way Marcus slab, `EngineBanThang`/`EngineZigZac` staircase & opening lintel design)
  5. `dce_design_steel_peb` (TCVN 5575:2024 PEB portal frame, bolted moment connection, anchor bolt base plate & TCVN 2737:2023 wind load)
  6. `dce_optimize_rebar_cutting` (1D 11.7m commercial bar cutting stock optimizer `ToiUuCatThep` & TCVN 1651 schedule aggregator)
  7. `dce_query_mayhoc_rebar` (Query `MayHoc/mauhoc.db` ML patterns & tokenize Vietnamese rebar callout strings)
  8. `dce_run_autostr_pipeline` (Execute the 24-step `AutoStrOrchestrator` on a `.autostr` project with auto-resizing & multi-sheet ClosedXML BOQ export)
- Create `.agents/skills/hp-dce-pro/SKILL.md` documenting how coding agents invoke `HPDcePro` tools and calculations.

### R2. `DbimSteelAutocad` Steel CAD Drafting Generator & Commands (`HPDcePro.Core` & `HPDcePro.AutoCad`)
- Implement `SteelFrameDraftingGenerator` in `HPDcePro.Core/Drafting/` generating 2D CAD primitives (`CadDraftingBuffer`) for PEB portal frame elevations (`MiddleFrameBT`, `MiddleFrame3`, `MiddleFrame3BT`, `MiddleFrame5`, `MiddleFrame5a`, `VDS1`), roof purlin & monitor plans (`PlanPurlinRoof`, `PlanPurlinMonitor`), and anchor bolt details (`DetailAnchorBolt`).
- Implement `SteelCadCommands` in `HPDcePro.AutoCad/Commands/` registering `[CommandMethod]` handlers for `DrawElevationMiddleFrameBT`, `DrawElevationMiddleFrame3`, `DrawElevationMiddleFrame3BT`, `DrawElevationMiddleFrame5`, `DrawElevationMiddleFrame5a`, `DrawElevationVDS1`, `DrawPlanPulirnRoof`, `DrawPlanPurlinMonitor`, `DetailAnchorBolt`, and `SteelConnectionDialog`, and add their LISP aliases to `HPDcePro.lsp`.

### R3. Complete `DCE Excel` & `DceSteel` UDF Parity (`HPDcePro.Excel`)
- Expand `CalculationUdfBridge` (or companion modular UDF classes in `HPDcePro.Excel`) to expose backward-compatible static functions for all remaining calculations in `V.2020/DCE Excel.dll` and `AddinSteel/DceSteel.dll`:
  - RC & Geotechnical: `TinhCotNeUonXien_PMM`, `TinhVachCung`, `TinhSanHaiPhuong`, `TinhDaiCoc`, `TinhMongDon`, `SucChiuTaiCoc_MaSat`, `BeRongVetNutDam`.
  - Steel & Wind: `TinhDamThep_TCVN5575`, `TinhCotThep_TCVN5575`, `TinhLienKetMoment_TCVN5575`, `TinhBuLongNeo_TCVN5575`, `TinhTaiTrongGio_TCVN2737`.

### R4. Unified `MainControlCenterWindow` (`GETFORM` Hub) & AutoCAD `.bundle` Manifest
- Add `MainControlCenterWindow` and `MainControlCenterViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI` serving as the central dashboard launched by `GETFORM` (with navigation commands to open `BeamDetailingWindow`, `ColumnWallDetailingWindow`, `FoundationSlabWindow`, `CuttingOptimizationWindow`, `SteelConnectionWindow`, and `AutoStrConfigWindow`, plus live project summary stats).
- Create `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml` for AutoCAD 2025–2026 ApplicationPlugins deployment.

## Acceptance Criteria

### Build & Clean Code Governance
- [ ] `C:\Dce Pro` remains 100% untouched (0 files modified, created, or deleted).
- [ ] `dotnet build HPDcePro/HPDcePro.slnx -c Debug` succeeds with `0 errors` and `0 warnings` (`TreatWarningsAsErrors=true`).
- [ ] Every C# file in `HPDcePro/` strictly obeys `HP_CLEAN_CODE_CORE.md` ($\le 300$ lines per file, methods $\le 50$ lines, 0 commented-out code, 0 unannotated empty catch blocks).

### Automated Verification (`HPDcePro.Core.Tests` & `HPDcePro.Adapters.Tests`)
- [ ] `dotnet test HPDcePro/HPDcePro.slnx -c Debug` passes 100% with **at least 330 automated tests** (`0 failed, 0 skipped`) covering all 8 MCP tools in `HPDcePro.Mcp`, `SteelFrameDraftingGenerator`, `SteelCadCommands`, all expanded Excel UDF functions in `HPDcePro.Excel`, and `MainControlCenterWindow` / `MainControlCenterViewModel`.


## 2026-10-18T02:28:56Z

Transform `HPKata` (`HPKata/HPKata.slnx`) into a 100% standalone, self-contained structural rebar detailing & fabrication suite for AutoCAD 2026 and Revit 2026 installed centrally at `C:\HPKata\`, bundled with its own complete CAD/Excel/LISP/Icon/Shape library and a dedicated standalone installer (`HPKata_Setup.exe`), operating identically to original KataPro while having zero dependency on `C:\kata_pro\`.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Context & Architectural Constraints

- **Solution**: `HPKata/HPKata.slnx` (`.NET 8.0`, SDK `10.0.300`).
- **UI Framework Constraint**: Keep the existing `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) UI framework in `HPKata.UI`, `HPKata.Revit`, and `HPKata.AutoCad`. Do **NOT** switch to `MahApps.Metro`. Keep the existing `Theme.xaml` StaticResource keys (`StandardDataGrid`, `Button.Primary`, `Button.Secondary`, `GroupBox.Header`, `Text.Label`, etc.) intact so all windows instantiate without `StaticResourceExtension` errors.
- **Clean Code Governance**:
  - Every `.cs` file in `HPKata/` must be `<= 300` lines (Rule C2).
  - Every method must be `<= 50` lines (Rule M3).
  - Zero compiler warnings (`TreatWarningsAsErrors=true`), no commented-out code (Q-B1), no empty catch blocks without a reason comment (Q-B2).
- **Single-Node Build Rule**: Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Requirements

### R1. Standalone `C:\HPKata\` Installation Directory & Complete Asset Migration (Zero `C:\kata_pro\` Dependency)
1. Create a central `KataInstallEnvironment` service in `HPKata.Core` (default root `C:\HPKata\`, overridable for isolated unit tests) that provides canonical paths for all library assets:
   - `C:\HPKata\Kata.xlsm`, `C:\HPKata\Kata ENG.xlsm`, `C:\HPKata\Iso_QHPlus.xls`
   - `C:\HPKata\Iso_kata.dwg` (master CAD template with 119 blocks, 12 layers, 15 dimstyles)
   - `C:\HPKata\Goi lenh.lsp` and `C:\HPKata\GoiLenh_HPKata.lsp` (86-parameter config + 118 AutoLISP command aliases)
   - `C:\HPKata\menukata.cuix`, `C:\HPKata\menukata.cui`, `C:\HPKata\menukata.mnr`, `C:\HPKata\menukata_light.mnr`, `C:\HPKata\kata.ico`, `C:\HPKata\ISO_KATA.EDB`
   - `C:\HPKata\dangthep\` (`00.wmf` .. `67.wmf` rebar shape diagrams)
   - `C:\HPKata\Cot\` (`*.wmf` column splice diagrams)
   - `C:\HPKata\Icon\` (including `C:\HPKata\Icon\Ribbon\*.png`) and `C:\HPKata\IMG\` (`*.png` guide diagrams)
   - `C:\HPKata\Temp\`
2. Copy/stage the library assets from `C:\kata_pro\` into `C:\HPKata\` (and into a distribution staging folder `HPKata/Distribution/Assets/` in the repository), and rewrite all path references inside `C:\HPKata\Goi lenh.lsp`, `C:\HPKata\GoiLenh_HPKata.lsp`, and `C:\HPKata\menukata.cui` so that every `C:\kata_pro` or `C:\\Kata_pro` reference points to `C:\HPKata` (`(command "netload" "C:\\HPKata\\HPKata.AutoCad.dll")` and `(command "_cuiload" "C:\\HPKata\\menukata.cuix")`).
3. Ensure **zero occurrences** of `kata_pro` (case-insensitive) exist anywhere in `HPKata/` source files (`.cs`, `.xaml`, `.lsp`, `.csproj`, `.xml`, `.addin`) or in `C:\HPKata\Goi lenh.lsp`.

### R2. Full KataPro Operational Parity Using the `C:\HPKata\` Library (Excel + DWG + Config + Visual Diagrams)
1. **Bi-Directional Excel Integration (`C:\HPKata\Kata.xlsm` & `C:\HPKata\Iso_QHPlus.xls`)**:
   - Implement clean, modular services in `HPKata.Core/Excel/` (each file `<= 250` lines) that read and write `C:\HPKata\Kata.xlsm` sheets `Dam` (beam spans, supports, top/bottom continuous & curtailment bars, stirrups, skin bars, hanger bars), `Cot` (column stories, cross-sections, longitudinal bars, ties), `Vach` (shear wall geometry, boundary cages, web mesh), and `Thong ke` / `Iso_QHPlus.xls` schedules. Support attaching to a running Excel instance (`Kata.xlsm` open on desktop) with automatic ClosedXML file fallback on `C:\HPKata\Kata.xlsm`.
   - Wire `BeamDetailingViewModel`, `ColumnDetailingViewModel`, and `WallDetailingViewModel` (plus `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, `WallDetailingWindow.xaml`) with commands and toolbar buttons to **Open `C:\HPKata\Kata.xlsm`**, **Read from `Kata.xlsm`**, and **Write to `Kata.xlsm`** (and auto-sync on Apply).
   - Ensure AutoCAD commands (`ve_dam`/`vd`, `ve_dam_shop`/`vds`, `loadbeam`, `ve_cot`/`vc`, `ve_cot_shop`/`vcs`, `loadcolumn`, `ve_vach`/`vv`, `ve_vach_shop`/`vvs`, `loadwall`, `toexcel`) and Revit commands (`FramingRebarCommands`, `ColumnRebarCommands`, `WallRebarCommands`) read/write `C:\HPKata\Kata.xlsm` and generate full shop drawings (elevation + cross-sections + `KATA` XData + callouts in AutoCAD; 3D `Rebar` with real element dimensions in Revit).
2. **AutoCAD Template & Block Importer (`C:\HPKata\Iso_kata.dwg`)**:
   - Implement `KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/` that loads standard layers, text styles, dimension styles, and block definitions (`thep`, section symbols, elevation symbols, grid bubbles) from `C:\HPKata\Iso_kata.dwg` into the active AutoCAD `Database` (`WblockCloneObjects`) whenever detailing commands execute (with safe fallback layer creation if `Iso_kata.dwg` is locked).
3. **86-Parameter Setup (`C:\HPKata\Goi lenh.lsp`) & Visual Asset Binding (`dangthep\`, `Cot\`, `Icon\`, `IMG\`)**:
   - Connect `SetupViewModel`, `SetupController` (AutoCAD `setupkata`), and `SetupCommand` (Revit) to load and save the 86 configuration parameters directly from/to `C:\HPKata\Goi lenh.lsp`.
   - Load Ribbon icons in `HPKata.AutoCad` and `HPKata.Revit` from `C:\HPKata\Icon\Ribbon\*.png` (with vector icon fallback), and display rebar shape diagrams (`C:\HPKata\dangthep\*.wmf` or `C:\HPKata\IMG\Kata-*.png`) and column splice diagrams (`C:\HPKata\Cot\*.wmf`) inside `HPKata.UI` dialogs (`DetailingView`, `BeamDetailingWindow`, `ColumnDetailingWindow`, etc.).

### R3. Centralized DLL Deployment at `C:\HPKata\` & Standalone Installer (`HPKata_Setup.exe`)
1. Add a new installer project `HPKata/HPKata.Installer/HPKata.Installer.csproj` (`net8.0-windows`, `UseWPF=true`, `AssemblyName=HPKata_Setup`) to `HPKata/HPKata.slnx`.
2. Implement `HPKata_Setup.exe` with both a clean WPF Installer UI (Install / Repair / Uninstall buttons, target path `C:\HPKata\`, status log) AND headless CLI switches (`--install`, `--uninstall`, `--target-dir <path>`) for automated verification.
3. Configure both MSBuild post-build targets (`HPKata.AutoCad.csproj` and `HPKata.Revit.csproj`) and `HPKata_Setup.exe --install` so that:
   - All compiled managed assemblies (`HPKata.Core.dll`, `HPKata.UI.dll`, `HPKata.AutoCad.dll`, `HPKata.Revit.dll`, and all NuGet runtime dependencies `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`, `CommunityToolkit.Mvvm.dll`, `ClosedXML.dll`, `QRCoder.dll`, etc.) and all library assets (`Iso_kata.dwg`, `Kata.xlsm`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, `menukata.cuix`, `dangthep\`, `Cot\`, `Icon\`, `IMG\`, `Temp\`) are deployed centrally into `C:\HPKata\`.
   - `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` is written with `<Assembly>C:\HPKata\HPKata.Revit.dll</Assembly>`.
   - `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml` is written with `ModuleName="C:\HPKata\HPKata.AutoCad.dll"` and `ModuleName="C:\HPKata\Goi lenh.lsp"`, and `C:\HPKata` is registered in AutoCAD's `TrustedPaths` in `HKCU\Software\Autodesk\AutoCAD`.

## Verification Resources

- Build & test commands:
  - `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
  - `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`
  - `HPKata\HPKata.Installer\bin\Debug\net8.0-windows\HPKata_Setup.exe --install`

## Acceptance Criteria

### Standalone `C:\HPKata\` Directory & Asset Completeness
- [ ] `C:\HPKata\` contains `HPKata.Core.dll`, `HPKata.UI.dll`, `HPKata.AutoCad.dll`, `HPKata.Revit.dll`, `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `ClosedXML.dll`, `Iso_kata.dwg`, `Kata.xlsm`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, `menukata.cuix`, and non-empty directories `C:\HPKata\dangthep\`, `C:\HPKata\Cot\`, `C:\HPKata\Icon\`, `C:\HPKata\IMG\`, `C:\HPKata\Temp\`.
- [ ] Zero occurrences of `kata_pro` (case-insensitive) exist in any source file under `HPKata/` or in `C:\HPKata\Goi lenh.lsp` and `C:\HPKata\GoiLenh_HPKata.lsp`.
- [ ] `%AppData%\Autodesk\Revit\Addins\2026\HPKata.Revit.addin` points `<Assembly>` directly to `C:\HPKata\HPKata.Revit.dll`, and `%AppData%\Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml` points `ModuleName` directly to `C:\HPKata\HPKata.AutoCad.dll`.

### Functional Parity & UI Integrity
- [ ] All 13 WPF windows in `HPKata.UI/Views/` inherit from `System.Windows.Window`, use `MaterialDesignThemes`, and instantiate on an STA thread with both Dark and Light themes without throwing any `StaticResourceExtension` or XAML exceptions.
- [ ] `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, and `HPKata.E2E.Tests` verify round-trip reading/writing of `C:\HPKata\Kata.xlsm` (`Dam`, `Cot`, `Vach`), 86-parameter config round-trip on `C:\HPKata\Goi lenh.lsp`, asset resolution from `C:\HPKata\`, and CLI execution of `HPKata_Setup.exe --install` into a target directory.
- [ ] `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` succeeds with `0 warnings, 0 errors`, `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100%, and every `.cs` file in `HPKata/` is `<= 300` lines.


## 2026-10-18T05:27:06Z

Analyze the legacy structural engineering software suite at `C:\Dce Pro` using `rea@6.6.0` and `ilspycmd`, record a complete and strictly accurate function-by-function compatibility matrix (`docs/rea-dce-pro/TRUTH_MATRIX.md`), migrate and normalize all static engineering templates and databases into `HPDcePro/assets/`, and build a 100% standalone, feature-complete C# replacement (`HPDcePro/HPDcePro.slnx`) operating identically to `DCE Pro` in AutoCAD 2026, Revit 2026, and Microsoft Excel with zero runtime dependency on `C:\Dce Pro`.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Verification Resources

- **Read-Only Legacy Baseline**: `C:\Dce Pro` (contains `DCE Pro.dll` 44.58 MB, `DceAnalyssiss64.dll` 69 native exports, `AddinSteel\DceSteel.dll` 20.36 MB, `AddinSteelCad\DbimSteelAutocad.dll` 4.20 MB, `V.2020\DCE Excel.dll` 10.86 MB, `AddinSetup_RVT\DBIM_RVT_26.exe` 9.44 MB Inno Setup package, `MayHoc\mauhoc.db` + `model.zip`, `Cad\*.dwg` + `AutoStr\00.FileDungChung\*.dwg` [17 DWGs], `Excel\**\*.xlsm` [132 XLSM workbooks], `System\*.txt` [178 config files], `Language\**\*.txt`, and `AiAgent\DanhBa\*.json` machine-readable command/method catalogs).
- **Installed Analysis Engine**: `rea@6.6.0` (`C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd`, 153 CLI commands / 207 MCP tools including `inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `compare-managed-members`, `build-reconstruction-obligation-ledger`, `evaluate-reconstruction-coverage`, `inspect-sqlite-database`) and `ilspycmd` (`C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe`).

## Requirements

### R1. Comprehensive Binary Analysis, Package Extraction & Honest Truth Matrix (`docs/rea-dce-pro/TRUTH_MATRIX.md`)
Extract the contents of `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` into a temporary scratch workspace outside `C:\Dce Pro` and inspect all managed and native assemblies in `C:\Dce Pro` (`DCE Pro.dll`, `DceAnalyssiss64.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, and the extracted Revit 2026 add-in DLLs) using `rea@6.6.0` and `ilspycmd`. Cross-reference every discovered command, form, jig, and function against `C:\Dce Pro\AiAgent\DanhBa\*.json` (311 AutoCAD `<CommandMethod>` entries, 73 Excel Ribbon commands, 49 `DceSteel` functions, 29 `MBKC` functions, 69 native C++ exports, and all Revit external commands) in `docs/rea-dce-pro/TRUTH_MATRIX.md`. Mark any existing command in `HPDcePro` that currently uses hardcoded sample/demo data (such as `CalculateSampleBeam` or `GenerateSampleColumnBuffer`) as `ĐANG LÀ MOCK`. Any function or method whose metadata or body cannot be inspected must be explicitly marked **`CHƯA GIẢI MÃ ĐƯỢC`** with its exact Metadata Token / RVA and technical reason — never guessed or faked.

### R2. Standalone Asset & Template Migration (`HPDcePro/assets/`)
Copy and normalize all required static engineering data assets from `C:\Dce Pro` into `HPDcePro/assets/` (including all 17 `.dwg` block/titleblock templates, 132 `.xlsm` calculation workbooks, `MayHoc\mauhoc.db` + `model.zip`, 178 `System\*.txt` default configuration files, `Language\**\*.txt` localization dictionaries, `Icon\**`, and AutoCAD `.cuix` menus). Update all file paths inside the migrated `.lsp`, `.xlsm`, and configuration files to resolve relative to `HPDcePro/assets/` and `HPDcePro.Excel` so the software operates 100% independently even if `C:\Dce Pro` is renamed or absent.

### R3. 1:1 AutoCAD, Revit & Excel Functional, UI, and Interactive Jig Migration
Replace all simplified/mock implementations across `HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Revit`, and `HPDcePro.Excel` with authentic 1:1 implementations matching `DCE Pro`:
- **AutoCAD (`HPDcePro.AutoCad`)**: Implement real interactive selection/point prompts (`Editor.GetSelection`, `Editor.GetEntity`, `Editor.GetPoint`, `Editor.GetKeywords`), all 11 interactive `DrawJig`/`EntityJig` classes (`VeMBKC_JIG`, `JigSlabRebar`, `TagLeader`, `TagChiaThep`, `ijgRaithepchamtron`, `ijgVeDaiVong`, `JigVeDaiC`, `VePolylineBatKy`, `ijgVeThepVachCT`, `JigVeVachHam`, `DimNhanhMBKC`), real XData read/write across all 13 RegApps, real DWG block insertion from `HPDcePro/assets/Cad/`, and all 24 `AutoStr` steps (`AutoStrStep01`..`24`).
- **UI (`HPDcePro.UI`)**: Implement the specialized Forms/Dialogs (`DamCong_Frm`, `MongCoc_Frm`, `MongCoc_FrmLopThepThem`, `MongCoc_FrmChonThepTuDong`, `ThangMoi_Frm`, `CocBT_Frm`, `TKT_frmMain`, `AutoStrWindow`, `Bea_ToiUuCatThepV2`, etc.), `PaletteSet` (`DcePalette`, `DcePalette_WallThickness`), and Ribbon (`DceRibbon` + `.cuix`).
- **Revit (`HPDcePro.Revit`) & Excel (`HPDcePro.Excel`)**: Implement all Revit 2026 commands/dialogs/rebar generators from `DBIM_RVT_26.exe` and all 73 Excel Ribbon/dispatch actions + UDFs from `DCE Excel.dll`.
- **Exclusions**: Exclude ZWCAD/GstarCAD/IntelliCAD ports (`ZwDCE Pro.dll`, `GcDCE Pro.dll`, `IcDCE Pro.dll`), Vosk voice recognition (`libvosk.dll` + `DceAiAgent`), and legacy cloud account/updater modules (`DbimTaiKhoanApi`, `DceUpdate.exe`).

### R4. Repository & Build Constraints
- **Strictly Read-Only Source**: Never create, modify, rename, or delete any file inside `C:\Dce Pro`.
- **Clean Code Governance**: Every new or modified `.cs` file in `HPDcePro/` must obey `AGENTS.md` and `docs/clean-code/HP_CLEAN_CODE_CORE.md` ($\le 300$ lines per file, $\le 50$ lines per method, zero commented-out code, zero unannotated empty `catch` blocks, `TreatWarningsAsErrors=true`).
- **Sequential Build Execution**: Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1` to prevent MSBuild node memory spikes.

## Acceptance Criteria

### Forensic Honesty & Truth Matrix
- [ ] `docs/rea-dce-pro/TRUTH_MATRIX.md` inventories 100% of the 311 AutoCAD commands, 73 Excel Ribbon commands, 49 Steel functions, 69 native C++ exports, 11 CAD Jigs, specialized UI forms, and all unpacked `DBIM_RVT_26.exe` Revit commands with exact legacy class/method names, metadata tokens, and current verification status.
- [ ] Zero functions or commands in `HPDcePro` use hardcoded sample/demo data (`CalculateSampleBeam`, `GenerateSampleColumnBuffer`, or fake placeholder outputs) while being marked completed.
- [ ] Any legacy method whose body cannot be inspected is explicitly listed under **`CHƯA GIẢI MÃ ĐƯỢC`** in `docs/rea-dce-pro/TRUTH_MATRIX.md` with its exact assembly, token/RVA, and technical blocker.

### Standalone Asset & Independence Verification
- [ ] `HPDcePro/assets/` contains the complete, normalized set of 17 `.dwg` templates, 132 `.xlsm` workbooks, `mauhoc.db` + `model.zip`, 178 `System\*.txt` files, `Language\**\*.txt`, `Icon\**`, and `.cuix` files.
- [ ] A static and runtime grep across `HPDcePro/src/**` and `HPDcePro/assets/**` confirms zero runtime dependency on `C:\Dce Pro` (all paths resolve relative to `HPDcePro`'s own installation/assets directory).

### Build, 3-Layer Parity & Test Verification
- [ ] `dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1` and `-c Debug` succeed with `0 Warning(s), 0 Error(s)`, and `HPDcePro/deploy/pack-bundle.ps1` deploys cleanly to AutoCAD, Revit 2026, Desktop, and MCP.
- [ ] Every item marked `HOÀN THÀNH` in `docs/rea-dce-pro/TRUTH_MATRIX.md` is backed by: (1) decompiled source/IL parity check (`rea@6.6.0` / `ilspycmd`), (2) automated unit/integration tests in `HPDcePro/tests/` verifying identical calculation/XData/geometry/Excel outputs, and (3) execution verification.


## 2026-10-18T05:29:18Z

Execute a comprehensive, strictly read-only, evidence-backed reverse engineering analysis of `C:\kata_pro` using **REA 6.6.0** (`rea@6.6.0`), produce the complete evidence audit dossier at `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` plus the 4-part executive report (**Đã giải mã – Chưa giải mã – Bằng chứng – Bước tiếp theo**), and complete the standalone `C:\HPKata\` reconstruction & installer (`HPKata_Setup.exe`) grounded in the verified REA 6.6.0 evidence.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Context & Non-Negotiable Constraints

- **REA 6.6.0 CLI Location**: `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"` (must verify `--version` outputs `rea@6.6.0` before running any analysis).
- **100% Read-Only on `C:\kata_pro\`**: Never modify, overwrite, delete, or corrupt any file in `C:\kata_pro\`. Record SHA-256 digests before and after analysis to prove zero mutation.
- **No Hallucination / Strict Evidence Boundary**: Every claim must cite a concrete `evidence_id` (`ev_...`), `sha256` digest, `file_offset`, or exact file/line/sheet reference. Any component that cannot be decompiled or recovered must be explicitly documented under **CHƯA GIẢI MÃ ĐƯỢC** with its exact technical cause (e.g., duplicate `#GUID` stream, native PE without CLI header, metadata representation budget limit, obfuscated method/type identifiers, or compiled VBA P-code).
- **UI Framework Constraint for `HPKata`**: Keep the existing `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) UI framework in `HPKata.UI`, `HPKata.Revit`, and `HPKata.AutoCad`. Do **NOT** switch to `MahApps.Metro`. Keep the existing `Theme.xaml` StaticResource keys intact so all 13 windows instantiate without `StaticResourceExtension` errors.
- **Clean Code Governance & Single-Node Build**: Every `.cs` file in `HPKata/` must be `<= 300` lines (Rule C2), every method `<= 50` lines (Rule M3), 0 compiler warnings. Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Requirements

### R1. Verify REA 6.6.0 & Execute Full Static Binary Audit on All 9 DLLs in `C:\kata_pro\`
1. Verify and record the exact output of `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version` (`rea@6.6.0`).
2. Run REA 6.6.0 commands (`inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `inspect-pe-resources`, and `build-reconstruction-obligation-ledger` where supported) across all 9 DLLs in `C:\kata_pro\`:
   - `Kata_Class_Lib.dll`
   - `Kata_Class_Lib_Revit.dll`
   - `Kata_pro64_Cad2025.dll`
   - `Kata_pro64_Cad2013.dll`
   - `Kata_pro64_GstarCad.dll`
   - `Kata_pro64_GstarCad2025.dll`
   - `Kata_pro64_ZwCad.dll`
   - `Kata_pro64.dll`
   - `kata_pro.dll`
3. Extract and document:
   - Exact `evidence_id`, `sha256`, `byte_length`, `classification` (`status`, `runtime_family`, `implementation`, `managed_architecture`), `references`, `resources`, `attributes`, `pe-resources`, and P/Invoke `managed-native-boundaries`.
   - Detailed member inventories (`types`, `methods`, `fields`, `member_refs`, `call_edges`, `field_accesses`, `user_strings`) for `Kata_Class_Lib.dll` (491 types, 3,356 methods, 1,307 fields, 3,214 call edges) and `Kata_Class_Lib_Revit.dll` (692 types, 4,357 methods, 1,613 fields, 4,007 call edges), including non-obfuscated public contracts, Revit/AutoCAD API references, and string literals.
   - Attribute-level command registrations (`[CommandMethod]` names) and assembly references (66 refs, 15,458 attributes) from `Kata_pro64_Cad2025.dll` and sibling CAD DLLs.

### R2. Complete Inventory & Structural Decoding of All Unencrypted Assets in `C:\kata_pro\`
Inspect and document with exact counts, SHA-256 hashes, and schema details:
1. **AutoLISP & Menu Files**: `Goi lenh.lsp` (118 `defun c:` command aliases + 86 configuration lines), `Goi lenh - Gstarcad.lsp`, `Goi lenh - Zwcad.lsp`, `menukata.cui`, `menukata.cuix`, `menukata.mnr`, `menukata_light.mnr`.
2. **CAD Template & Structural Model**: `Iso_kata.dwg` (119 block definitions, 12 layers, 15 dimstyles, text styles) and `ISO_KATA.EDB`.
3. **Excel Workbooks**: `Kata.xlsm`, `Kata ENG.xlsm`, `Kata-GCAD.xlsm`, `Kata-ZwCad.xlsm`, `Iso_QHPlus.xls` (exact sheet names, cell coordinates for `Dam`, `Cot`, `Vach`, `Thong ke`, and status of `xl/vbaProject.bin`).
4. **Visual Shape & Icon Libraries**: `dangthep\` (`00.wmf`..`67.wmf` + `.png`), `Cot\` (`*.wmf`), `Icon\` (`Ribbon\*.png`), `IMG\` (`*.png`).

### R3. Produce the Comprehensive Audit Dossier & Complete Standalone `C:\HPKata\` Suite
1. Write `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` containing:
   - **Mục 1 — Đã giải mã (Decoded Components)**: Full tables of decoded DLL metadata, public classes/methods, AutoCAD/Revit commands, Excel sheet/cell schemas, DWG blocks/layers/dimstyles, LISP aliases, 86 config lines, and WMF/PNG assets.
   - **Mục 2 — Chưa giải mã được (CHƯA GIẢI MÃ ĐƯỢC & Nguyên nhân kỹ thuật)**:
     - `Kata_pro64.dll`: **CHƯA GIẢI MÃ ĐƯỢC** (`status: "malformed"`, `metadata-not-admitted: Duplicate metadata stream #GUID` at `file_offset: 2555180`).
     - `kata_pro.dll`: **CHƯA GIẢI MÃ ĐƯỢC** (`status: "not-managed"`, `cli-directory-absent` — native x64 PE without CLI header).
     - `Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, `Kata_pro64_GstarCad.dll`, `Kata_pro64_GstarCad2025.dll`, `Kata_pro64_ZwCad.dll` full CIL member bodies: **CHƯA GIẢI MÃ ĐƯỢC** via `inspect-managed-members` due to `coverage.state: "unavailable"`, `resource-limit` (`Managed metadata exceeds the 67108864 byte representation budget` at `offset: 14897632`), and obfuscated internal class/method names.
     - Obfuscated internal identifiers (`Class0..`, `smethod_0..`, encrypted string tables) inside `Kata_Class_Lib.dll` and `Kata_Class_Lib_Revit.dll`: **CHƯA GIẢI MÃ ĐƯỢC** tên gốc.
     - Binary VBA macro stream `xl/vbaProject.bin` inside `Kata.xlsm`: document exact readable module names/strings vs. compiled P-code / password-locked sections (**CHƯA GIẢI MÃ ĐƯỢC**).
   - **Mục 3 — Bằng chứng (Evidence Ledger)**: Complete table mapping every file and claim to its `evidence_id` (`ev_...`), `sha256`, `byte_length`, and `file_offset`.
   - **Mục 4 — Bước tiếp theo (Next Steps & Gap Matrix vs. `C:\HPKata\`)**: Concrete verification and completion roadmap for `C:\HPKata\`.
2. Complete any remaining standalone `C:\HPKata\` integration items (`KataDwgTemplateImporter` in `HPKata.AutoCad/Interop/`, 86-parameter `Goi lenh.lsp` sync in `SetupViewModel`/`SetupController`/`SetupCommand`, visual asset loader, and `HPKata.Installer` / `HPKata_Setup.exe` deploying all DLLs and assets to `C:\HPKata\`) and verify `dotnet build` & `dotnet test` pass 100%.

## Verification Resources

- REA 6.6.0 verification:
  - `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version`
  - `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" inspect-managed-artifact "C:\kata_pro\Kata_Class_Lib.dll" --format json`
- Build & test verification (single-node):
  - `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
  - `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`

## Acceptance Criteria

### REA 6.6.0 Audit & Evidence Integrity
- [ ] `rea@6.6.0` is verified prior to execution, and every one of the 9 `.dll` files in `C:\kata_pro\` has its `inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, and `inspect-pe-resources` `evidence_id` (`ev_...`) and `sha256` recorded in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] Every unrecoverable or obfuscated component (`Kata_pro64.dll` duplicate `#GUID` stream at offset `2555180`, `kata_pro.dll` native PE without CLI header, `Kata_pro64_Cad2025.dll` metadata budget limit at offset `14897632`, obfuscated class/method names, and binary `vbaProject.bin`) is explicitly marked **CHƯA GIẢI MÃ ĐƯỢC** with its exact technical cause and byte offset.
- [ ] Zero files in `C:\kata_pro\` are modified (verified by unchanged SHA-256 and timestamps).

### Standalone `C:\HPKata\` Build & Installer Readiness
- [ ] `C:\HPKata\` is populated with all standalone `HPKata.*.dll` assemblies, NuGet runtime dependencies, `HPKata_Setup.exe`, and all library assets (`Iso_kata.dwg`, `Kata.xlsm`, `Goi lenh.lsp`, `dangthep\`, `Cot\`, `Icon\`, `IMG\`, `Temp\`) with zero references to `C:\kata_pro`.
- [ ] `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` succeeds with `0 warnings, 0 errors`, and `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100%.

## 2026-10-18T07:43:43Z

URGENT SYSTEM DIRECTIVE FROM USER (Unknown Hard Error on dotnet.exe / HPDcePro.Core.Tests.exe / HPDcePro.Adapters.Tests.exe):

The user just reported multiple Windows `Unknown Hard Error` system warning dialogs triggered when 5 parallel Gate subagents (`reviewer_m2_gen2_1`, `reviewer_m2_gen2_2`, `challenger_m2_gen2_1`, `challenger_m2_gen2_2`, `auditor_m2_gen2`) executed `dotnet build` / `dotnet test` simultaneously, exhausting Windows Virtual Memory Commit Charge (`STATUS_COMMITMENT_LIMIT`).

IMMEDIATE MANDATORY RULES FOR `orchestrator_25_gen2` AND ALL FUTURE ORCHESTRATORS/SUBAGENTS:
1. Forward this directive IMMEDIATELY to `orchestrator_25_gen2` (`1e55a9de-0d52-405d-83fa-fa58bb05a293`) and any active Gate subagents.
2. **ZERO PARALLEL `dotnet` EXECUTION**: Never allow multiple parallel subagents (Reviewers, Challengers, Auditors, or parallel Workers) to run `dotnet build` or `dotnet test` at the same time!
3. **Single Build/Test Evidence Rule**:
   - Only ONE agent at a time (either the Worker before handoff, or the Orchestrator before spawning Gate subagents) may run `dotnet build` / `dotnet test`.
   - When running `dotnet`, ALWAYS set `$env:DOTNET_GCHeapHardLimit="0x20000000"` (512 MB cap per process), `$env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1"`, `$env:MSBUILDDISABLENODEREUSE="1"`, and pass `--disable-build-servers -m:1` (and run `dotnet test` per project one at a time with `--no-build`, NEVER across the whole `.slnx` simultaneously).
   - Save the build & test output to a shared log file in the orchestrator workspace (e.g., `latest_build_test.log`), and instruct all parallel Reviewers, Challengers, and Auditors to **read that log file and inspect source files statically** — FORBID Reviewers/Challengers/Auditors from invoking `dotnet build` or `dotnet test` themselves!

## 2026-10-18T07:48:07Z

CRITICAL ROOT CAUSE IDENTIFIED FROM WINDOWS EVENT LOG (Event ID 1025 & 26) — BAN `dotnet test` AND USE DIRECT `.exe` TEST RUNNER:

When `dotnet test` runs with `Microsoft.Testing.Platform`, `Microsoft.Testing.Platform.MSBuild.InvokeTestingPlatformTask` opens an exclusive lock on `TestResults\HPDcePro.Core.Tests_net8.0_x64.log` and starts a `NamedPipeServer`. If two processes touch that file or pipe, `Microsoft.Testing.Platform` calls `System.Environment.FailFast`, which triggers Windows `NtRaiseHardError` (`Unknown Hard Error` modal dialog on both `dotnet.exe` and `HPDcePro.*.Tests.exe`).

MANDATORY REPLACEMENT RULE FOR ALL ORCHESTRATORS & WORKERS:
1. NEVER run `dotnet test`!
2. To build and run tests, ALWAYS execute in a single sequential worker:
   ```powershell
   dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
   ```
   Running the compiled xUnit.v3 `.exe` binaries directly bypasses `NamedPipeServer` and `TestResults\*.log` completely, runs all tests in seconds, and can NEVER trigger `System.Environment.FailFast` (`Unknown Hard Error`).
3. Note: `DOTNET_GCHeapHardLimit` has been cleared from User env vars so normal `dotnet build --disable-build-servers -m:1` has full headroom while single-threaded.


## 2026-10-18T08:11:37Z

Execute Phase 2 Deep Decoding of all 5 remaining items in `C:\kata_pro` (the newly unlocked `C:\kata_pro\Kata.xlsm` VBA project, `Kata_pro64.dll` with duplicate `#GUID` streams, the 5 large CAD DLLs that exceeded REA's 64 MiB single-JSON budget, the obfuscated `Class0..`/`smethod_0..` identifiers, and the native PE32+ `kata_pro.dll`), update `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` and companion evidence files, and synchronize the unlocked `Kata.xlsm` into `C:\HPKata\Kata.xlsm` while keeping `C:\kata_pro` 100% read-only.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Context & Non-Negotiable Constraints

- **REA 6.6.0 CLI Location**: `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"` (`rea@6.6.0`).
- **100% Read-Only on `C:\kata_pro\`**: Never modify, overwrite, or delete any file in `C:\kata_pro\`. Any binary header normalization (`Kata_pro64.dll`) MUST be performed exclusively on a temporary copy in `%TEMP%` and cleaned up after analysis.
- **No AutoCAD Native `new Database(true, true)` in Out-of-Process Tests**: Never instantiate `new Autodesk.AutoCAD.DatabaseServices.Database(true, true)` in out-of-process xUnit tests (which causes a native `0xc0000005` popup outside `acad.exe`).
- **Single-Node Build Rule**: Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.

## Requirements

### R1. Extract Complete Plain-Text VBA Source Code from Unlocked `C:\kata_pro\Kata.xlsm` & Sync to `C:\HPKata\`
1. Parse `xl/vbaProject.bin` (`105,472` bytes) inside the newly unlocked `C:\kata_pro\Kata.xlsm` (`474,272` bytes) using standard OLE2 compound document parsing and `[MS-OVBA]` section 2.4.1 RLE decompression.
2. Recover and save the complete plain-text VBA source code of all 21 VBA modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`) into `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`, documenting every `Sub`, `Function`, `Type`, cell coordinate, and algorithm.
3. Copy the newly unlocked `C:\kata_pro\Kata.xlsm` to `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`.

### R2. Decode `Kata_pro64.dll` via Temporary BSJB Stream Header Normalization + REA 6.6.0
1. In `C:\kata_pro\Kata_pro64.dll`, the ECMA-335 `BSJB` metadata root at `file_offset: 2555068` declares `num_streams = 9`, where Streams `0..4` (`#~` 161,412B, `#Strings` 651,880B, `#US` 4B, `#GUID` 16B, `#Blob` 44,096B) are the real metadata streams and Streams `5..8` (`#GUID` at `2555180`, `#Strings`, `#Blob`, `#Schema`) are 4–16 byte decoy streams.
2. Create a temporary copy in `%TEMP%` with `num_streams` normalized from `9` to `5` at `BSJB + 18 + ver_len` (`file_offset: 2555098`), run `rea@6.6.0` (`inspect-managed-artifact`, `inspect-managed-members`, `inspect-managed-native-boundaries`, `inspect-pe-resources`), and record the resulting `evidence_id`s, **166 Types, 4,720 Methods, 1,613 Fields, 1,212 MemberRefs, 55 CallEdges, 60 FieldAccesses, 13 Assembly References, 30 Embedded Resources, and 2,931 Custom Attributes**, then delete the temporary file.

### R3. Bypass REA 64 MiB JSON Budget on the 5 CAD DLLs & Build the Semantic Deobfuscation Map
1. For all 5 large CAD DLLs (`Kata_pro64_Cad2025.dll`, `Kata_pro64_Cad2013.dll`, `Kata_pro64_GstarCad.dll`, `Kata_pro64_GstarCad2025.dll`, `Kata_pro64_ZwCad.dll`), use ECMA-335 metadata/CIL streaming (`System.Reflection.Metadata.MetadataReader` / Python ECMA-335 table & IL parser) to extract exact counts and inventories of `TypeDef`, `MethodDef`, `FieldDef`, `MemberRef`, `ImplMap` (P/Invoke), `#US` user strings (`ldstr`), and CIL method body statistics (RVA, code size, max stack, local signature, called methods, accessed fields, loaded strings) without hitting REA's 64 MiB single-JSON-string cap.
2. Build a comprehensive **Semantic Deobfuscation Map** (`docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md`) mapping the obfuscated classes (`Class0`..`ClassN`) and methods (`smethod_0`..`smethod_N`, `method_0`..) across `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`, `Kata_pro64.dll`, and `Kata_pro64_Cad2025.dll` to their true engineering roles using:
   - The 193 `[CommandMethod]` attributes and their declaring class/method tokens,
   - Loaded Vietnamese/English string literals (`ldstr`), DXF XData group codes (`1000..1071`), layer names, block names, and Excel sheet/cell references,
   - External AutoCAD (`acdbmgd`/`acmgd`), Revit (`RevitAPI`), and Excel (`Microsoft.Office.Interop.Excel`) member references,
   - Cross-references with the decoded VBA functions (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`) in `Kata.xlsm`.

### R4. Static PE32+ Analysis of Native `kata_pro.dll` & Update Master Audit Dossier
1. Perform a complete static PE32+ analysis of `C:\kata_pro\kata_pro.dll` (`1,011,712` bytes, x64 native PE): extract PE headers, COFF sections (`.text`, `.rdata`, `.data`, `.pdata`, `.rsrc`, `.reloc`, etc.) with virtual addresses, raw sizes, and entropy, Export Directory, Import Directory (imported DLLs and API functions), embedded strings, and cross-reference with all `[DllImport]` / `ImplMap` P/Invoke entries in the 8 managed DLLs.
2. Update `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md` so all newly decoded components, evidence IDs, counts, VBA modules, CIL member inventories, Semantic Deobfuscation Map, and native PE analysis are integrated into **Mục 1 (Đã giải mã)** and **Mục 3 (Bằng chứng)**, while **Mục 2 (Chưa giải mã được)** accurately reflects only what remains mathematically irreversible (original pre-compiler ASCII names of stripped local variables and runtime-packed native machine code blocks, if any).

## Verification Resources

- REA 6.6.0 CLI:
  - `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version`
- Build & test verification (single-node):
  - `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
  - `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`

## Acceptance Criteria

### Complete Decoding of All 5 Previously Blocked Items
- [ ] `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` contains the decompressed plain-text VBA source code of all 21 modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`) extracted from `C:\kata_pro\Kata.xlsm`, and `C:\HPKata\Kata.xlsm` is updated to match the unlocked workbook.
- [ ] `Kata_pro64.dll` is decoded via temporary BSJB header normalization (`num_streams: 9 -> 5`) using `rea@6.6.0`, with its `inspect-managed-artifact` and `inspect-managed-members` `evidence_id`s and exact counts (166 types, 4,720 methods, 1,613 fields, 1,212 member_refs, 30 resources, 2,931 attributes) documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] All 5 large CAD DLLs (`Kata_pro64_Cad2025.dll`, `Cad2013`, `GstarCad`, `GstarCad2025`, `ZwCad`) have their exact `TypeDef`, `MethodDef`, `FieldDef`, `MemberRef`, `ImplMap`, and `#US` string literal counts and command-to-method CIL call graphs extracted and documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] `docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md` maps the obfuscated `Class0..`/`smethod_0..` types and methods across the managed DLLs to their concrete domain responsibilities and command entrypoints.
- [ ] `kata_pro.dll` (native x64 PE32+) has its complete PE sections, imports, exports, version resources, and P/Invoke boundaries documented in `docs/rea-kata-pro/REA_6_6_0_KATA_PRO_AUDIT.md`.
- [ ] All 396 files in `C:\kata_pro\` remain 100% unmodified compared to their current pre-run SHA-256 baseline, and `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100% with zero popup dialogs.


## 2026-10-18T10:57:58Z

URGENT FORENSIC INTEGRITY ALERT — REJECT `worker_m5_gen3` FAKE SUMMARY TABLE IN `docs/rea-dce-pro/TRUTH_MATRIX.md`!

I just inspected `docs/rea-dce-pro/TRUTH_MATRIX.md` lines 22–100:
`worker_m5_gen3` falsified the Summary Statistics Table (lines 22–33) to claim `602 HOÀN THÀNH | 0 ĐANG LÀ MOCK | 0 CHƯA GIẢI MÃ ĐƯỢC`, while the actual table rows in Subsystem 1, Subsystem 3, Subsystem 4, Subsystem 5 still have hundreds of rows marked `CHƯA GIẢI MÃ ĐƯỢC` and `ĐANG LÀ MOCK`!

This directly violates the User's Mandatory Rules 4, 5, 6, 7:
- "Tuyệt đối không suy đoán, bịa đặt thông tin, tạo chức năng giả hoặc báo hoàn thành khi chưa kiểm chứng."
- "Phần nào chưa giải mã được phải ghi rõ 'CHƯA GIẢI MÃ ĐƯỢC', nêu nguyên nhân và những gì còn thiếu."
- "Không được tuyên bố hoàn thành 100% nếu vẫn còn bất kỳ thành phần nào chưa được xác minh."

MANDATORY IMMEDIATE ACTIONS FOR SENTINEL & `orchestrator_25_gen2`:
1. **FAIL Milestone M5 Gate immediately** on `TRUTH_MATRIX.md` summary falsification.
2. Run `python tools/verification/run_all_truth_matrix_verifications.py` — the Summary Statistics Table in `docs/rea-dce-pro/TRUTH_MATRIX.md` MUST equal the EXACT row-by-row count of `HOÀN THÀNH`, `ĐANG LÀ MOCK`, and `CHƯA GIẢI MÃ ĐƯỢC` in the body tables!
3. Furthermore, inspect all remaining `ĐANG LÀ MOCK` rows in Subsystem 1 (`HPDcePro.AutoCad/Commands/`): replace the remaining mock implementations in `HPDcePro/src/HPDcePro.AutoCad/Commands/` (`BeamCommands*.cs`, `ColumnCommands*.cs`, `FootingCommands*.cs`, `SlabCommands*.cs`, `RebarCommands*.cs`, `SteelCommands*.cs`, `MiscCadCommands*.cs`) with real interactive `PromptHelper` + geometry/XData logic so there are `0 ĐANG LÀ MOCK` in `HPDcePro`, while keeping every genuinely encrypted Babel .NET / unverified endpoint honestly classified and counted as `CHƯA GIẢI MÃ ĐƯỢC`!
