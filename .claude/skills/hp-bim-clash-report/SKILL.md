---
name: hp-bim-clash-report
description: "Chạy các clash test HP (ma trận RuleClash(HP)) của giai đoạn hồ sơ trong model Navisworks theo lô, rồi xuất báo cáo xung đột có ảnh từng clash qua HPNavis MCP. Seed bim_run_phase_tests chạy test theo lô; bim_export_clash_snapshot ghi snapshot JSON và report HTML (Tabular) của Navisworks (ảnh do tab Report render) vào ClashReports\\ cạnh NWD; script build-clash-report.py ghi xlsx gồm Summary + CLASH DESCRIPTION (bố cục agenda họp phối hợp, ảnh ở cột Clash Photo Name), Tổng hợp, Theo tầng, Clash và Thay đổi so với lần xuất trước. TRIGGER when: user gõ /hp-bim-clash-report, hoặc nói 'báo cáo clash', 'báo cáo xung đột có ảnh', 'chạy clash test', 'xuất kết quả clash', 'theo dõi clash', 'so sánh clash tuần này'."
user-invocable: true
argument-hint: "[auto | 200 | 300 | 350-400] [maxPictures]"
when_to_use: "Khi cần chạy các test HP của giai đoạn và lập báo cáo clash có ảnh cho buổi họp phối hợp, hoặc so sánh với lần xuất trước."
category: navisworks
keywords: [navisworks, clash, clash detective, báo cáo, report, ảnh, picture, xlsx, theo dõi, snapshot, bimcoordinator]
metadata:
  author: hoang
  version: "2.0.0"
  mcp-server: hprebar-navis
---

# Báo cáo clash HP có ảnh

Mỗi lần xuất ghi vào `ClashReports\` cạnh file NWD:

| File | Nội dung |
|---|---|
| `clash-LOD<giai đoạn>-<yyyyMMdd-HHmmss>.json` | Snapshot: mọi clash, dùng làm mốc so sánh cho lần sau |
| thư mục cùng tên | Report HTML (Tabular) do Navisworks ghi + `clash-report_files\cdNNNNNN.jpg` (ảnh 1024 px) |
| `.xlsx` cùng tên | Báo cáo |

Tham số `lod` mặc định `auto` = giai đoạn duy nhất của các test HP trong model. Nếu model có test của nhiều giai đoạn thì chỉ rõ `200`, `300` hoặc `350-400` (nên gom về một giai đoạn bằng `/hp-bim-clash-phase` trước). `maxPictures` mặc định 500, tối đa 2000.

## Quy trình

Gọi tool `mcp__hprebar-navis__*`. Nếu tool chưa nạp thì load bằng ToolSearch.

1. **Kết nối**: `get_navis_context`.
   - Cần `executionEnabled: true`, `heavyOperationsEnabled: true` (chạy test và ghi file đều là heavy), `hasClashModule: true`.
   - Model phải đã Save và nằm trên ổ đĩa có ký tự (không phải UNC).
   - Opt-in tắt → nhờ người dùng tick. Không tự tick hộ.
   - Nhắc người dùng **Save** trước: chạy test không undo được.
2. **Chạy test theo lô**: `bim_run_phase_tests lod:<tham số>`.
   - Gọi lại với `after:<nextAfter>` cho tới khi `nextAfter` = null (cursor theo tên test, không lệch khi test đổi giữa hai lần gọi).
   - Seed chỉ bắt đầu test mới khi test chậm nhất vẫn còn vừa `budgetSeconds` (mặc định 300). Lần gọi lỗi / timeout bị bridge rollback và **mất luôn các lần chạy của lần gọi đó** (H-13) — seed không bao giờ ném lỗi sau khi đã chạy test.
   - `failed[]` ≠ rỗng → báo tên test và lỗi, các test khác vẫn chạy.
   - Chạy dở (đóng Navisworks giữa chừng) → `onlyNotRun:true` (có thể kèm `after`) để chạy nốt.
   - Đo trên THCSLT: 121 test trong ~80 s, nên một lần gọi là xong.
3. **Xuất snapshot + ảnh**: `bim_export_clash_snapshot lod:<tham số> maxPictures:<n>`.
   - Ghi lại `path`, `html.path`, `html.pictures`/`html.reported`, `byStatus`, `warnings`.
   - Ảnh ưu tiên clash đang mở (New/Active/Reviewed) rồi mới tới Resolved, mỗi lượt theo tên test (Nhóm 1 trước), tới `maxPictures`; clash còn lại vẫn có dòng, cột ảnh ghi "chưa có ảnh". Không có clash nào cần ảnh → không ghi HTML (cảnh báo).
   - Tên trường report phải là tiếng Anh (Navisworks bản tiếng Anh); khác thì seed từ chối → làm tay theo mục dưới.
   - Report chỉ gồm clash New / Active / Reviewed / Resolved (Approved bị bỏ). Cài đặt tab Report của người dùng được trả lại như cũ.
   - Đo: 300 ảnh ~8 s. Heavy không có dryRun. Model không bị sửa.
4. **Dựng xlsx** (Bash, từ gốc repo):

   ```bash
   PYTHONIOENCODING=utf-8 python HPNavis/tools/bim-coordinator/build-clash-report.py "<path>"
   ```

   - Script tự tìm report HTML của snapshot (trường `htmlReport`) và snapshot cũ hơn cùng giai đoạn để so sánh.
   - `WARNING:` về ảnh (HTML không đọc được, clash đã có ảnh mà không thấy trong HTML) → báo người dùng, không coi là thành công trọn vẹn.
   - Lỗi file bị khoá (đang mở trong Excel / Google Drive đang sync) → nhờ người dùng đóng file rồi chạy lại.
5. **Mô tả AI (tuỳ chọn)**: thêm cột `AI_Classification` / `AI_Description` / `AI_Suggested_Action` / `AI_Confidence` (và cột ẩn `Clash GUID`):

   ```bash
   PYTHONIOENCODING=utf-8 python HPNavis/tools/bim-coordinator/clash-ai-groups.py "<path>" --pilot 10 --write-judgements
   PYTHONIOENCODING=utf-8 python HPNavis/tools/bim-coordinator/build-clash-report.py "<path>"
   ```

   - Gom clash theo kiểu (test, category + family hai bên, dải độ xuyên ≤10/≤50/≤200/>200 mm) → `<snapshot>-ai-groups.json`; nhận định theo kiểu va chạm lấy từ `tools/bim-coordinator/clash-ai-knowledge.json` (khoá `cặp bộ môn|category 1|category 2`) → `<snapshot>-ai.json` khoá theo id nhóm (mỗi dòng xlsx tìm nhóm của mình qua Clash GUID). Nhóm đã có nhận định (viết tay, đợt thử) được giữ nguyên.
   - Kiểu va chạm chưa có trong kho → script in `no judgement for …`: Claude xem dữ liệu + ảnh mẫu của nhóm (`samples[].picture`), viết thêm vào kho, chạy lại.
   - Mỗi dòng: câu sự kiện (cấu kiện, Family – Type, size, hệ, ký hiệu, Element Id, file, tầng, trục theo lưới của chính toà nhà, toạ độ, độ xuyên) + nhận định + "Dữ liệu chưa đủ: thiếu …" nếu thiếu. Độ tin cậy giảm khi thiếu ảnh/thuộc tính.
   - "Hợp lệ theo thiết kế" chỉ khi quy tắc trong `tools/bim-coordinator/clash-acceptance-rules.json` đã được người dùng duyệt (`approved: true` kiểu boolean + `approvedBy`) và clash đúng cặp bộ môn + category hai phía (`sides`, ví dụ ARC:Walls × STR:Floors) + độ xuyên ≤ giới hạn; còn lại ra "Cần xác minh" với đề xuất "Chờ BIM xác nhận: quy tắc …". Nhận định lấy từ kho tối đa độ tin cậy "Trung bình". Claude không tự đổi `approved`.
   - Thử 10 clash (`pilot` trong file groups) và cho người dùng duyệt trước khi áp toàn bộ.
6. **Báo cáo** theo mẫu *Kết quả Triển khai*: đường dẫn xlsx, tổng clash theo trạng thái, số dòng có ảnh / tổng, top tầng, số clash mới / đã xử lý so với lần trước, test lỗi.

   Không dán danh sách clash vào chat; danh sách nằm trong file.

### Khi seed không ghi được report HTML

Nếu `bim_export_clash_snapshot` báo lỗi report (API report là API nội bộ của Navisworks):

1. Xuất lại với `images:false` (chỉ snapshot).
2. Người dùng mở Clash Detective ▸ **Report**:
   - Contents: bỏ **Item Properties** (nếu không, mỗi thuộc tính của model thành một cột).
   - Include Clashes: New, Active, Reviewed, Resolved.
   - Report Type: **All Tests (combined)**. Report Format: **HTML (Tabular)**.
   - **Write Report** vào thư mục `ClashReports\<tên snapshot>\`.
3. Dựng xlsx với `--html "<file .html vừa ghi>"`.

## Đọc báo cáo

| Sheet | Nội dung |
|---|---|
| Summary | Mới / Đang chờ / Đã giải quyết theo bộ môn (ARCHITECTURE / STRUCTURE / MEPF), tổng chưa giải quyết, biểu đồ cột. Mỗi clash được đếm cho cả hai bộ môn của cặp; dòng All đếm một lần |
| CLASH DESCRIPTION | Bố cục agenda họp phối hợp: STT, ảnh (Clash Photo Name: ảnh Navisworks 1024 px nhúng nguyên byte, hiện ~9×9 cm, zoom vẫn nét; link tới file ảnh), Vị trí = tầng, Trục = Grid Location của Navisworks, mô tả (clash, cặp, Nhóm, hai cấu kiện + Element Id, khoảng cách, test), ngày phát hiện, cột 1 ở Architecture/Structure/MEPF cho cả hai bộ môn, Nhóm + mức, trạng thái Mới (New/Active) / Chờ (Reviewed) / Xong (Resolved). Cột ý kiến / giải đáp / bản vẽ để người dùng điền |
| Tổng hợp | Nhóm 1/2/3 × cặp bộ môn × New/Active/Reviewed/Approved/Resolved, tổng lần trước, chênh lệch, số mới, số đã xử lý |
| Theo tầng | Tầng × Nhóm × trạng thái. Tầng lấy từ `Item > Layer` của cấu kiện 1, nếu trống thì của cấu kiện 2; không có thì "(không rõ)" |
| Clash | Mỗi clash một dòng: test, trạng thái, khoảng cách và tâm (mm), hai cấu kiện (tên, category, Element Id, file) và GUID. Có sẵn AutoFilter |
| Thay đổi | **Mới**: GUID chưa có ở lần trước. **Đã xử lý**: lần trước đang mở (New/Active/Reviewed), lần này đã Resolved hoặc không còn trong kết quả |

Clash được so khớp theo GUID kết quả. Khi GUID đổi (test bị xoá rồi tạo lại), so khớp theo rule + Element Id của hai cấu kiện. Ảnh được ghép theo (tên test, tên clash) trong report HTML.

## Lỗi thường gặp

| Hiện tượng | Xử lý |
|---|---|
| `HEAVY … TestsRunTest` / `ExportClashSnapshot` | Người dùng tick *Allow heavy operations* |
| `the model has not been saved` | Người dùng Save model trước |
| `network share (UNC path)` | Mở model qua ổ đĩa có ký tự (ví dụ ổ Q) |
| `several phases` | Chỉ rõ `lod`, hoặc chạy `/hp-bim-clash-phase` để còn một giai đoạn |
| `no HP clash test` | Tạo test bằng `/hp-bim-clash-phase` |
| `Navisworks did not write the clash report` / `has no report format` | Làm theo mục "Khi seed không ghi được report HTML" |
| Trục giống nhau ở mọi dòng (THCSLT: `A.2d-4.2d : MÁJ`) | Grid Location là giá trị Navisworks tự tính trong report, script chỉ chép lại. Kiểm tra hệ lưới / tầng đang chọn ở View ▸ Grids & Levels rồi xuất lại (nguyên nhân chưa xác minh) |
| Ảnh mờ sau khi Save lại trong Excel | Excel nén ảnh khi lưu (mặc định ~220 ppi): File ▸ Options ▸ Advanced ▸ Image Size and Quality ▸ tick *Do not compress images in file* trước khi Save, hoặc dựng lại xlsx bằng script |
| Ảnh nặng (~400 KB/ảnh), thấy cả wireframe model | Ảnh report theo thiết lập hiển thị kết quả của Clash Detective trong phiên: chọn *Hide Other* (Results ▸ Display Settings ▸ Isolation) rồi xuất lại; ảnh chỉ còn hai cấu kiện, ~40 KB |
| `is locked` khi dựng xlsx | Đóng file xlsx trong Excel, đợi Google Drive sync xong rồi chạy lại bước 4 |
