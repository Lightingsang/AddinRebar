---
name: hp-bim-clash-phase
description: "Tạo / cập nhật bộ clash test HP (ma trận RuleClash(HP)) cho MỘT giai đoạn hồ sơ trong Navisworks Manage qua HPNavis MCP: co-so = thiết kế cơ sở (LOD200 @ 50 mm), ky-thuat = thiết kế kỹ thuật (LOD300 @ 30 mm), thi-cong = bản vẽ thi công (LOD350-400 @ 10 mm). Quy trình: search set → xem trước → dryRun → ghi + đọc lại → canary → (duyệt) xoá test HP của giai đoạn khác → nhắc Save. TRIGGER when: user gõ /hp-bim-clash-phase, hoặc nói 'tạo clash test giai đoạn …', 'clash thiết kế cơ sở / kỹ thuật / thi công', 'chuyển giai đoạn clash'."
user-invocable: true
argument-hint: "co-so | ky-thuat | thi-cong"
when_to_use: "Khi cần dựng bộ clash detective HP theo giai đoạn thiết kế trên model Navisworks đang mở, hoặc chuyển model sang giai đoạn kế tiếp."
category: navisworks
keywords: [navisworks, clash, clash detective, lod, giai đoạn, thiết kế cơ sở, thiết kế kỹ thuật, thi công, ruleclash, bimcoordinator]
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-navis
---

# HP clash test theo giai đoạn hồ sơ

Một model chỉ giữ bộ test của **một** giai đoạn, để *Run All* của Clash Detective chạy đúng phạm vi.

| Tham số | Giai đoạn | `lod` | Dung sai | Cặp (ma trận) |
|---|---|---|---|---|
| `co-so` | Thiết kế cơ sở | `200` | 50 mm | 27 |
| `ky-thuat` | Thiết kế kỹ thuật | `300` | 30 mm | 154 |
| `thi-cong` | Bản vẽ thi công | `350-400` | 10 mm | 184 |

Thiếu tham số hoặc tham số lạ → hỏi lại bằng AskUserQuestion. Không đoán giai đoạn.

## Engine tự lo (không làm lại bằng tay)

- **Tên test:** `HP|LOD<lod>|P<nhóm>|<cặp bộ môn>|<A5 Doors>-<S2 Structural Walls>`. Sắp Clash Detective theo tên để đọc theo LOD rồi Nhóm, vì API không tạo được folder.
- **Selection:**
  - Base search set của nhóm.
  - Riêng M4 chọn **M4.9**: ống D ≥ 32 mm, theo ghi chú 3 của sheet.
- **Tab Rules và composite:**
  - Same group/block/cell: luôn bật.
  - Coincident snap points: chỉ bật khi hai nhóm cùng bộ môn.
  - Same layer, same file: không bao giờ bật.
  - Composite Object Clashing: bật.
- **Upsert theo tên:**
  - Test cũ được cập nhật và giữ kết quả.
  - Tên kiểu cũ (`HP|P1|LOD350|…`, `HP|LOD350|…`, `HP|LOD400|…`, thiếu tên nhóm) được đổi sang tên mới.
- **Cặp có set rỗng:** bị skip, không bao giờ mở rộng sang cả model.
- **Test không mang tên `HP|…`** (ví dụ "Test 1" do người dùng tạo): không bao giờ bị chạm tới.

## Quy trình

Gọi tool `mcp__hprebar-navis__*`. Nếu tool chưa nạp thì load bằng ToolSearch.

1. **Kết nối**: `get_navis_context`.
   - Cần `executionEnabled: true`, `hasClashModule: true` và đúng model.
   - Opt-in tắt → nhờ người dùng tick *Allow AI code execution*. Bước 6 cần thêm *Allow heavy operations*.
   - Không tự tick hộ.
2. **Search set**: `bim_sync_search_sets apply:true`.
   - Tạo 21 set nền cùng M4.9.
   - Set đã có thì báo `unchanged`.
   - Có `conflict` → dừng, báo người dùng. Không dùng `allowUpdate` khi chưa được duyệt.
3. **Xem trước**: `bim_sync_clash_tests lod:<lod> apply:false`. Báo cho người dùng:
   - số Create / Update / Unchanged / Skip;
   - `otherPhaseCount` và `duplicates`: test HP của giai đoạn khác và bản trùng.
4. **dryRun**: `bim_sync_clash_tests lod:<lod> apply:true dryRun:true`.
   - Cần `read back OK` và `rolledBack:true`.
5. **Ghi**: `bim_sync_clash_tests lod:<lod> apply:true`.
   - Cần `success:true` và `read back OK`.
6. **Canary** (heavy): `bim_run_canary_tests lod:<lod>`.
   - Báo số kết quả của từng test.
   - Có test Error → dừng và báo.
7. **Xoá test giai đoạn khác**, chỉ khi bước 3 có `otherPhaseCount > 0` hoặc có duplicates.
   - Hỏi người dùng bằng AskUserQuestion. Nêu số test sẽ xoá và cảnh báo **kết quả clash của chúng sẽ mất**; muốn giữ thì xuất báo cáo Clash Detective trước.
   - Chỉ khi người dùng đồng ý mới chạy, dryRun trước:
     - `bim_sync_clash_tests lod:<lod> apply:true removeOtherPhases:true dryRun:true`
     - `bim_sync_clash_tests lod:<lod> apply:true removeOtherPhases:true`
   - Lệnh này không được kèm `priorities` hoặc `ruleIds`.
8. **Kết thúc**: báo theo mẫu *Kết quả Triển khai* và nhắc người dùng **Ctrl+S**. Claude không bao giờ Save NWD.

## Lỗi thường gặp

| Hiện tượng | Xử lý |
|---|---|
| `-32001` / opt-in tắt | Người dùng tick *Allow AI code execution* |
| `HEAVY` ở canary | Người dùng tick *Allow heavy operations*, hoặc bỏ qua canary |
| `Navisworks did not provide the Rules-tab rule(s)` | Tạo test tên `HP Rules Template`, tick đủ 4 rule ở tab Rules, rồi chạy lại |
| Skip nhiều | Model thiếu cấu kiện của nhóm (set rỗng). Đây không phải lỗi; danh sách nằm trong output xem trước |
| `removeOtherPhases cleans the whole model` | Bỏ `priorities` / `ruleIds` khỏi lệnh xoá |

Chi tiết tool, seed và engine: skill `hp-mcp-navisworks`. Ma trận và Nhóm 1/2/3: `plans/261010-1110-bim-coordinator-navis/reports/clash-groups-analysis.md`.
