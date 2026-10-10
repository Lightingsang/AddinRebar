# Antigravity Workflow — Định tuyến Trả lời / Sửa nhanh / Planning Mode

Rule này quyết định **route** cho mỗi yêu cầu và độ dài phản hồi. Template, trạng thái Planned/Implemented/Built/Tested/Verified, "Quy tắc tự chủ kỹ thuật" và "Không được làm" nằm ở [CLAUDE.md](CLAUDE.md) ▸ *Response Format — Antigravity Style (MANDATORY)* — tham chiếu, không chép lại.

## 1. Bảng định tuyến

| Route | Khi nào (đủ một điều kiện) | Làm gì |
|---|---|---|
| **Trả lời trực tiếp** | Tra cứu, giải thích, câu hỏi grep được; sửa lỗi ≤ 2 file không đổi API/public surface và không cần build/test (typo, comment, chuỗi, docs) | Scout-first theo [review-audit-self-decision.md](.claude/rules/review-audit-self-decision.md) §4: ≥ 85 % confidence → trả lời + `path:line`; < 85 % → hỏi. Không plan, không template |
| **Sửa nhanh** | ≤ 2 file sửa tay (file do generator sinh ra không tính), không dependency mới, không đổi schema / manifest / `.csproj` / `.slnx` / config, có build hoặc test kiểm được | Làm luôn → chạy build/test → báo `Kết quả Triển khai` rút gọn ≤ 15 dòng (bảng trạng thái + bảng kiểm tra + bước tiếp) |
| **Planning Mode** | Bất kỳ: ≥ 3 file; folder / project / feature mới; NuGet hoặc dependency mới; đổi `.csproj` / `.slnx` / manifest (`.addin`, `PackageContents.xml`) / `.mcp.json` / hooks; đổi public API hoặc wire contract trong [McpShared/](McpShared/) (Contracts DTO, `JsonRpcMethods`, tên pipe); thao tác irreversible (xóa, ghi đè dữ liệu, push, deploy); user dùng từ "plan" / "lên kế hoạch" / "refactor" | Mục 2 |

Tie-break: một trigger Planning là đủ; sửa ≤ 2 file mà cần build/test → Sửa nhanh, không cần → Trả lời trực tiếp; còn nghi ngờ → Planning Mode.

## 2. Planning Mode

0. Yêu cầu chưa cụ thể đủ 5 trường (output, tiêu chí nghiệm thu, phạm vi trong/ngoài, ràng buộc bắt buộc, điểm chạm) → chạy `/grill-me` trước, chỉ sang bước 1 khi user xác nhận requirement contract. Đủ 5 trường hoặc user đưa sẵn plan → bỏ qua bước này.
1. Trước khi chạm code: `/bs:plan` (Stack-Aware 6 phase khi project Nice3point; mỗi phase là lát dọc ghi `type: AFK|HITL` + `dependencies`) → artifact [plans/](plans/) ▸ `<YYMMDD-HHmm>-<slug>/plan.md` < 80 dòng, thêm `phase-NN-<slug>.md` khi > 1 phase — cấu trúc theo [documentation-management.md](.claude/rules/documentation-management.md).
2. Trả `Kế hoạch Triển khai` đúng template CLAUDE.md, tóm ≤ 10 dòng + link tới plan (mục 4).
3. **DỪNG chờ xác nhận** khi có ≥ 1 mục *User Review Required* **hoặc** bất kỳ trigger irreversible / dependency mới / wire contract — kể cả khi không có câu hỏi mở.
4. Không có mục nào ở bước 3 → ghi câu chốt "Không có quyết định cần người dùng" rồi tiếp tục `/bs:cook` theo [primary-workflow.md](.claude/rules/primary-workflow.md) (build gate → test → review) — đúng "Quy tắc tự chủ kỹ thuật".
5. User đưa sẵn plan path → không plan lại, `/bs:cook <plan-path>`; kết thúc mỗi phase bằng `Kết quả Triển khai` đầy đủ.
6. Không tạo plan cho route Trả lời trực tiếp / Sửa nhanh.

## 3. Giao tiếp súc tích

- Trả lời trực tiếp: ≤ 10 dòng; câu trả lời đứng trước, dẫn chứng `path:line` đứng sau; không mở bài, không kết bài, không bảng trạng thái.
- Sửa nhanh / Planning Mode: đúng heading của template CLAUDE.md; mỗi mục ≤ 5 dòng; bảng thay danh sách khi ≥ 3 hạng mục; mục không có nội dung → bỏ hẳn, không viết "N/A".
- Không lặp lại yêu cầu của user; không kể quá trình (đã grep gì, thử gì, sai ở đâu rồi sửa) — chỉ kết quả + bằng chứng.
- Chưa chạy kiểm tra → dùng đúng cụm "CHƯA TEST" / "GIẢ ĐỊNH CHƯA XÁC MINH" của template, không diễn đạt lại.
- Caveman mode (hook) chỉ bỏ từ đệm; heading, bảng, 5 emoji trạng thái và số liệu giữ nguyên.

## 4. Artifacts

- Mọi tài liệu > 30 dòng đi vào file: kế hoạch → [plans/](plans/), report review / debug → [plans/reports/](plans/reports/), tài liệu dự án → [docs/](docs/). Chat chỉ tóm ≤ 10 dòng + link tới file.
- Báo cáo mỗi phase dùng `Kết quả Triển khai`; file report tuân "Sacrifice grammar for concision" của CLAUDE.md.
- Trạng thái chỉ dùng 5 emoji của CLAUDE.md: ✅ 🟡 ❌ ⚠️ 👤 — không ký hiệu khác, không màu chữ.
- Tài liệu chỉ nằm trong repo; không ghi vào `~/.claude` hay ngoài worktree.

## 5. Link & trích dẫn

- Link markdown tương đối từ gốc repo, dòng bằng `#L<n>`, khoảng dòng `#L<a>-L<b>`: [Civil3dUnitTable.cs:36](HPCivil3d/HPCivil3d.McpBridge/Service/Civil3dUnitTable.cs#L36); thư mục dạng [HPGeo/](HPGeo/).
- Không URI scheme `file:`, không đường dẫn tuyệt đối (vỡ giữa các máy), không backtick cho path — backtick chỉ cho symbol, lệnh, giá trị.
- Mỗi khẳng định về code có ≥ 1 link; kết luận đã kiểm bằng source/test ghi `verified by file:line` theo [review-audit-self-decision.md](.claude/rules/review-audit-self-decision.md) §1.
- Link tới plan / docs / report cũng tương đối: [plan.md](plans/260919-1608-express-tools-port-autocad-mcp/plan.md).

## 6. Ví dụ định tuyến

1. "Hàm `Civil3dUnitTable.Resolve` làm gì?" → **Trả lời trực tiếp**.
   Grep được; trả lời ≤ 10 dòng + [Civil3dUnitTable.cs:36](HPCivil3d/HPCivil3d.McpBridge/Service/Civil3dUnitTable.cs#L36).
2. "Sửa `partLimit` mặc định 60 trong seed `list_pipe_networks`" → **Sửa nhanh**.
   1 file sửa tay [generate-seed-library.py:455](HPCivil3d/tools/generate-seed-library.py#L455) (tool.json / code.cs sinh lại), kiểm bằng `dotnet test HPCivil3d/HPCivil3d.Mcp.Server.Tests` (`Schema_defaults_equal_the_code_fallbacks`).
3. "Thêm seed `list_pressure_networks` cho HPCivil3d" → **Planning Mode**.
   Folder seed mới + generator + tests + catalog skill = ≥ 3 file, feature mới; plan trước, `Kế hoạch Triển khai`, chờ xác nhận nếu có mục User Review Required.
