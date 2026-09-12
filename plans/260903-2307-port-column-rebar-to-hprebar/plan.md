---
title: "Port feature Column Rebar (R01_ColumnsRebar) sang HPRebar"
status: partial
created: 2026-09-03
validated: 2026-09-03
scope: project
mode: hard
supersedes: [260530-column-rebar-ctie-and-real-rebar]
blockedBy: []
blocks: []
source: "F:/1-CONG VIEC/05-AI/01_Revit/02_Csharp/RebarAddin-master/RebarAddin-master/R01_ColumnsRebar"
target: "HPRebar/HPRebar/Column Rebar/ + HPRebar/HPRebar.Core/ColumnRebar/"
---

# Port Column Rebar → HPRebar

Port tool `R01_ColumnsRebar` (~19.5k dòng, .NET 4.8, Revit 2021, MVVM tự viết) sang HPRebar (Nice3point SDK, R23–R27, CommunityToolkit.Mvvm). Giữ nguyên thuật toán, bỏ dependency `WpfCustomControls` + `DSP`, tách pure math ra project riêng để xUnit, sửa 6 bug đã xác định.

**Scope đã chốt (user):** 7 output · 8 tab + canvas mặt đứng · R23–R27 · xUnit + TUnit.
**Đã tách khỏi scope (Validation 1):** đường `IsRebar=false` (Detail Item `DT*`) → plan riêng sau.

## Quyết định kiến trúc

| # | Quyết định | Lý do |
|---|---|---|
| D1 | Pure math → project riêng `HPRebar.Core` (netstandard2.0, không ref RevitAPI) | `docs/code-standards.md` §8 bắt buộc; xUnit không chạy được nếu assembly load RevitAPI. ILRepack merge vào 1 DLL khi deploy |
| D2 | Toàn bộ Core tính bằng **mm** (`double`); convert sang feet chỉ ở boundary Revit qua `UnitUtils.ConvertToInternalUnits` | Bỏ hack `double.Parse(UnitFormatUtils.Format())` phụ thuộc culture; bỏ luôn error 15 |
| D3 | Không dùng `Rebar.CreateFromCurves` | Tool gốc dùng `CreateFreeForm` + `CreateFromRebarShape`. **[cook Phase 4]** `CreateFromRebarShape` signature giống hệt R23→R27. `CreateFreeForm` thì **KHÔNG**: overload `out RebarFreeFormValidationResult` bị xóa ở R27, overload `RebarStyle` chỉ có từ R26 → cần 1 `#if REVIT2026_OR_GREATER` trong `MainBarCreator` |
| D4 | Canvas → custom `FrameworkElement` với DependencyProperty, vẽ trong `OnRender` | Source vẽ trực tiếp từ VM lên `Canvas` (vi phạm MVVM, không test được). Ưu tiên `DrawingContext` thay `Canvas.Children` |
| D5 | i18n EN/VN → `LocalizationService : ObservableObject` giữ 1 `UiStrings` record, swap runtime | Giữ hành vi đổi ngôn ngữ live như gốc, KISS hơn resx + culture reload |
| D6 | `System.Windows.Forms.MessageBox` → `TaskDialog` | net8/net10 không cần WinForms ref; đồng bộ UX Revit |
| D7 | Modal dialog, `Owner` = `UiApplication.MainWindowHandle` | Không cần ExternalEvent; progress bar vẫn pump qua `Dispatcher` như gốc |
| **D8** | **`ColumnRebarOrchestrator` là chủ sở hữu DUY NHẤT của `TransactionGroup`**; service chỉ mở `Transaction`; command không chạm group | Validation 1 — bản đầu có group lồng 3 tầng (command + orchestrator + service). Orchestrator sở hữu → test được độc lập trong TUnit, undo 1 bước, Cancel dialog không cần rollback vì chưa gọi Run |

## Phases

| # | Phase | File | Effort | Depends |
|---|---|---|---|---|
| 0 | ✅ Scaffold feature folder + Core project + Theme + logging + baseline build | [phase-00](phase-00-scaffold-feature-folder-and-baseline.md) | 0.5d | — |
| 1 | ✅ Multi-version compat + R27 restore verify | [phase-01](phase-01-multi-version-compat-shims.md) | 0.5d | 0 |
| 2 | ✅ Core pure logic (layout / splice / stirrup / schedule) + xUnit | [phase-02](phase-02-pure-logic-domain-and-xunit.md) | 2d | 0 |
| 3 | 🟡 Revit geometry readers + validation (fix 6 bug) + fixture + TUnit | [phase-03](phase-03-revit-geometry-readers-and-validation.md) | 2d | 1, 2 |
| 4 | 🟡 Rebar creation service (stirrup / main / add / dowels) | [phase-04](phase-04-rebar-creation-service.md) | 2d | 3 |
| 5 | 🟡 Views + dimension + tag + detail shop + orchestrator | [phase-05](phase-05-views-dimensions-tags-detail-shop.md) | 1.5d | 4 |
| 6 | 🟡 WPF shell + 8 tab ViewModels/Views + Theme Dark/Light | [phase-06](phase-06-wpf-shell-and-tab-viewmodels.md) | 3d | 2, 3 |
| 7 | 🟡 Preview canvas (elevation + section + dowels + diagrams) | [phase-07](phase-07-preview-canvas.md) | 3d | 6 |
| 8 | 🟡 Integration, F5 smoke, Release all configs, docs | [phase-08](phase-08-integration-f5-release-docs.md) | 1.5d | 4, 5, 7 |

Tổng ≈ 16 ngày. Phase 2 và 6 chạy song song được sau Phase 0.

## Ma trận version (verified — SDK 6.2.3 props)

| Revit | TFM | Runtime | Máy dev | Mức verify |
|---|---|---|---|---|
| R23, R24 | `net48` | .NET Framework 4.8 | ❌ không cài | build-only |
| R25 | `net8.0-windows7.0` | .NET 8 | ✅ | F5 phụ (case 1–3) |
| R26 | `net8.0-windows7.0` | .NET 8 | ✅ | **F5 chính + TUnit** |
| R27 | `net10.0-windows7.0` | **.NET 10** | ❌ | build-only; package `RevitAPI 2027.2.0` **đã restore + build pass** (Phase 1) |

## Build gate (mọi phase)

```bash
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26        # chính — máy có Revit 2026
dotnet build HPRebar.slnx -c Debug.R23        # net48 — bắt lỗi API/TFM sớm
dotnet test HPRebar.Core.Tests                # từ Phase 2
```

## Tài liệu tham chiếu

- [reports/source-analysis-r01-columnsrebar.md](reports/source-analysis-r01-columnsrebar.md) — thuật toán + bug list + API surface, đọc trước khi cook bất kỳ phase nào
- Skills: `revit-addin`, `revit-wpf-mvvm`, `revit-xaml-styles`, `revit-test`, `revit-debug`
- Plan cũ (code đã mất): `plans/260530-column-rebar-ctie-and-real-rebar/plan.md` — SUPERSEDED, chỉ còn giá trị ghi chú API

## Rủi ro chính

1. ~~**R27 không verify được runtime**~~ **HẠ RỦI RO (Phase 1):** package `2027.2.0` restore + build pass, TFM `net10.0-windows7.0`. Vẫn build-only (máy không cài Revit 2027) nhưng rủi ro compile đã loại — cả 5 config build 0 error.
   ⚠️ **[cook Phase 4] Đính chính Phase 1:** kết luận "0 `#if REVIT` cần thiết" **sai**. API sweep chỉ so **tên** type/member nên bỏ sót `Rebar.CreateFreeForm` — overload đổi cả signature lẫn return type giữa R25→R26→R27. Đã cần 1 `#if`. Khi cook phase sau, **đừng tin sweep tên là đủ** cho API sẽ gọi thật; check signature tay.
2. **R23/R24 (net48) cũng build-only** — rủi ro thật ở `FormattedText` ctor (Phase 7) và Polyfill `MinBy` (Phase 3).
3. ~~**Canvas ~5.4k dòng draw code** — phase nặng nhất.~~ **ĐÃ GIẢI QUYẾT (Phase 6+7):** cả hai rủi ro khối lượng đều không thành hiện thực vì toán đã nằm ở Core. Phase 6 VM lớn nhất 58 dòng (gốc 517). Phase 7 tổng 8 file / 1015 dòng (gốc 5.4k) — vì D4 đổi sang `DrawingContext` nên canvas gọi thẳng Core calculator thay vì tính lại, đúng cái mà bản gốc làm sai (`DrawStirrupItemRectangle0` tính lại `n`/`del` tách rời khỏi `CreateStirrupTypeItem1`). Phase 7 chia 4 sub-step có gate riêng; ước tính chỉ port ~2.5k (chỉ hàm có call-site).
4. **`Reference.ParseFromStableRepresentation` hack `SURFACE→LINEAR`** cho dimension — fragile, undocumented. Không verify được R27 → Phase 5 wrap try/catch, skip dimension thay vì fail.
5. **Family phụ thuộc**: rebar shape `M_T1`/`M_T3` (bắt buộc) + detail shop `DS*` (optional, thiếu thì bỏ qua). Phase 3 tạo fixture `.rvt` có sẵn 2 shape này.

## Validation Log

### Session 1 — 2026-09-03
**Trigger:** `/bs:plan validate` sau khi tạo plan (user chọn ở Post-Plan Handoff)
**Tier:** Full (9 phase → 4 role, 15+ claim/phase)
**Questions asked:** 4 (batch 2 gồm 4 câu nữa bị user hủy → áp default khuyến nghị, ghi ở "Default áp dụng" bên dưới)

#### Verification Results
- **Claims checked:** 34 · **Verified:** 30 · **Failed:** 4 · **Unverified:** 0

Failures (đã sửa hết):
1. [Fact Checker] Phase 1 — bảng TFM ghi `R25–R27 = net8.0-windows`. Thực tế `nice3point.revit.sdk/6.2.3/Sdk/*.props`: `>= 2025 → net8.0-windows7.0`, `>= 2027 → net10.0-windows7.0`. → sửa bảng + ghi chú skill doc cũng sai.
2. [Fact Checker] Phase 3 — `dotnet new revit-test`. Thực tế `dotnet new list revit` → short name là **`revit-tunit`** ("Revit Test (TUnit)"). → sửa.
3. [Fact Checker] Phase 6 — style key `Style.Button.Primary` / `Style.TextBox.Number`. Thực tế `controls-sample.md`: `PrimaryButton`, `SecondaryButton`, `NumberTextBox`, … (SKILL.md §98 quy ước `<Element><Variant>` PascalCase). → sửa + liệt kê full key vào Phase 0.
4. [Fact Checker] Phase 0 — "copy 3 file từ skill". Thực tế `references/styles/*` là **`.md`** chứa fenced XAML (4+3+1+1 block). → đổi thành extract.

Verified (mẫu): `ExternalCommand.{Document,UiDocument,UiApplication}` + `Context` (Toolkit 2027.0.0 XML) · `Rebar.CreateFreeForm/CreateFromRebarShape` + `ScaleToBox` + `SetLayoutAsNumberWithSpacing` + `RebarFreeFormValidationResult` (RevitAPI 2023.1.90 & 2026.4.10) · `NewDimension` · `ViewSection.CreateSection` · `ElementType.Duplicate` · `TextNote.Create` · `ViewSchedule.CreateSchedule` · `LookupParameter` · `UnitUtils.ConvertToInternalUnits(double,ForgeTypeId)` · `MainWindowHandle`/`UIThemeManager`/`TaskDialog.Show`/`PickObjects` (RevitAPIUI 2026) · Polyfill 11.2.0 có `MinBy`/`IsExternalInit`/`RequiredMemberAttribute` (net461 contentFiles) · CommunityToolkit.Mvvm 8.4.0 + xunit trong cache · git-lfs 3.7.1 · line span source (`StirrupModel` region 233–830, `ErrorColumns` rule 102–399, `SettingModel` ctor 89–161, `DimensionView` 266, `TagColumn` 210, `ProcessDetailItem` 255, `CreateDetailShop` 162, `DrawDetailShop` 286).

Phát hiện thêm (không phải claim của plan): `Application.cs:27` panel hiện là `"Commands"`; `HPRebar` **chưa từng restore** (không có `obj/project.assets.json`); `docs/codebase-summary.md` + `docs/project-changelog.md` chưa tồn tại (Phase 8 phải tạo mới, không phải sửa); Serilog chỉ có `Sinks.Debug`; máy cài Revit **2025 + 2026** (registry HKLM), không có 2023/2024/2027.

#### Questions & Answers

1. **[Verification]** 4 claim trong plan sai so với codebase thật: (1) R27 target `net10.0-windows7.0` không phải net8 (SDK 6.2.3 props); (2) template là `dotnet new revit-tunit`, không phải `revit-test`; (3) theme key là `PrimaryButton`/`SecondaryButton`/`NumberTextBox`, không phải `Style.Button.Primary`; (4) theme sample là `.md` có fenced XAML, phải extract chứ không copy file. Áp dụng sửa?
   - Options: Áp dụng cả 4 (Recommended) | Chỉ sửa 1–3, giữ cách copy theme | Không sửa, để cook tự xử
   - **Answer:** Áp dụng cả 4
   - **Rationale:** Đều là fact sai, không đổi thiết kế. Không sửa thì cook vấp 4 lần, tốn token + có thể bịa tên key mới làm vỡ theme.

2. **[Architecture]** Plan hiện có TransactionGroup ở 3 chỗ lồng nhau (Command, Orchestrator Phase 5, RebarCreationService Phase 4) — mâu thuẫn. Ai sở hữu TransactionGroup?
   - Options: Orchestrator sở hữu (Recommended) | Command sở hữu như tool gốc | Mỗi service tự có group riêng
   - **Answer:** Orchestrator sở hữu
   - **Rationale:** Quyết định D8. Orchestrator self-contained → TUnit test được không cần dựng command; Cancel dialog không cần rollback (chưa gọi Run); undo vẫn 1 bước nhờ Assimilate.

3. **[Scope]** Đường `IsRebar=false` (thay rebar thật bằng Detail Item family `DT00..DT07A`) — phụ thuộc family riêng của tác giả gốc, ~700 dòng, dùng cho bản vẽ 2D không model thép. Xử lý?
   - Options: Tách ra plan sau (Recommended) | Giữ trong Phase 5 như plan | Bỏ hẳn
   - **Answer:** Tách ra plan sau
   - **Rationale:** Repo không có family `DT*` → không test được. Phase 5 giảm 2d → 1.5d. Property `IsRebar` vẫn giữ trong session (checkbox disabled) để plan sau nối vào không phải sửa session.

4. **[Risk]** Máy này cài Revit 2025 + 2026 (registry), không có 2023/2024/2027. HPRebar chưa từng restore. Chiến lược verify?
   - Options: F5 + TUnit trên R26 (+R25); R23/R24/R27 build-only (Recommended) | Có máy khác có R27/R23 — sẽ verify thủ công | Bỏ R27 khỏi Configurations
   - **Answer:** F5 + TUnit trên R26 (+R25); R23/R24/R27 build-only
   - **Rationale:** TUnit project giới hạn `<Configurations>` R25/R26. Report Phase 8 phải nói rõ R23/R24/R27 chưa verify runtime — không tuyên bố "đã hỗ trợ R23–R27".

#### Default áp dụng (batch 2 bị hủy — dùng option khuyến nghị, user có thể lật lại)
- **Fixture `.rvt`:** cook tự tạo trong Revit 2026 + commit qua git LFS (`.gitattributes` `*.rvt filter=lfs`). Nếu user có file từ tool gốc thì dùng, tiết kiệm ~1h. → Phase 3.
- **Theme:** Dark + Light theo `UIThemeManager` (`#if REVIT2024_OR_GREATER`, R23 fallback Dark) — khớp default của `revit-checklist.md`. → Phase 6 (`ThemeSwitcher.cs`).
- **Logging:** thêm `Serilog.Sinks.File` → `%LocalAppData%\HPRebar\logs\`, daily rolling, giữ 7 file. Cần để debug `CreateFreeForm` fail lúc F5 (Debug sink chỉ thấy khi attach). → Phase 0.
- **Rebar shape:** hardcode `M_T1`/`M_T3` như gốc, bỏ combobox chọn shape khỏi Setting tab (gốc có combobox nhưng `ConditionButtonOK` ép về đúng 2 tên này → dead UI). → Phase 4.

#### Confirmed Decisions
- D8 TransactionGroup ownership: orchestrator — test được, undo 1 bước
- Scope: bỏ Detail Item path khỏi đợt này
- Verify: R26 là môi trường thật, phần còn lại build-only
- 4 fact correction áp dụng toàn bộ

#### Action Items
- [x] Sửa bảng TFM Phase 1 + ghi chú skill doc sai
- [x] `revit-test` → `revit-tunit` (Phase 3)
- [x] Style key đúng tên + liệt kê full vào Phase 0 (Phase 0, 6)
- [x] Theme extract từ `.md` (Phase 0)
- [x] D8 vào plan.md + Phase 4/5/6/8
- [x] Bỏ `DetailItemRebarCreator` khỏi Phase 5, effort 2d → 1.5d
- [x] Phase 3 effort 1.5d → 2d (thêm fixture + LFS)
- [x] Build gate R27 → R26 ở mọi phase
- [x] Serilog file sink (Phase 0), ThemeSwitcher (Phase 6), shape hardcode (Phase 4), fixture LFS (Phase 3)
- [x] Phase 8: `docs/codebase-summary.md` + `docs/project-changelog.md` là **tạo mới**
- [ ] Phase 1 bước 8: sửa `multi-version-strategy.md` bảng TFM 2027 (làm khi cook Phase 1)

#### Impact on Phases
- Phase 0: +Serilog file sink, +6 canvas brush key, extract theme từ md, gate R26
- Phase 1: bảng TFM viết lại theo SDK props, +bước sửa skill doc
- Phase 3: effort 2d, `revit-tunit`, config R25/R26, +fixture + `.gitattributes` LFS
- Phase 4: service không mở group, shape hardcode, gate R26
- Phase 5: bỏ Detail Item path, orchestrator sở hữu group, effort 1.5d
- Phase 6: style key đúng, +`ThemeSwitcher`, `IsRebar` checkbox disabled, command không chạm group
- Phase 7: canvas brush key trỏ về Phase 0, gate R26
- Phase 8: F5 matrix R26 + R25, docs là tạo mới, report nói rõ giới hạn verify

### Whole-Plan Consistency Sweep
Đã re-read `plan.md` + 9 `phase-*.md` sau khi propagate.
- `TransactionGroup`: chỉ Phase 5 (orchestrator) sở hữu; Phase 4 ghi rõ "KHÔNG mở group" + có success criterion grep; Phase 6/8 mô tả command không chạm group. ✅ nhất quán
- Build gate: `Debug.R26` + `Debug.R23` ở Phase 0/2/3/4/5/6/7/8. Phase 1 là ngoại lệ có chủ đích — build cả 5 config + restore R27, vì đó chính là phase verify multi-version. ✅ (sweep lần 1 bắt được 2 chỗ sót `Debug.R27` ở Phase 0 bước 1 và Phase 2 bước 9 → đã sửa)
- TFM: chỉ khai báo ở Phase 1 + bảng trong plan.md, khớp nhau. ✅
- Detail Item: Phase 5 ghi ngoài scope; Phase 6 giữ property disabled; plan.md ghi đã tách. Không còn `DetailItemRebarCreator` ở phase nào khác. ✅
- Style key: Phase 0 liệt kê nguồn, Phase 6/7 tham chiếu, không còn `Style.Button.*`/`Style.TextBox.Number`. ✅
- Effort: bảng phases (0.5+0.5+2+2+2+1.5+3+3+1.5 = 16d) khớp frontmatter từng file. ✅
- **Không còn mâu thuẫn chưa giải quyết.** Đủ điều kiện cook.

## Trạng thái (cook 2026-09-04)

| Phase | Code | Verify runtime |
|---|---|---|
| 0 Scaffold | ✅ | ⬜ F5 |
| 1 Multi-version | ✅ | — (build-only theo thiết kế) |
| 2 Core + xUnit | ✅ | ✅ 99/99 test |
| 3 Geometry + validator | ✅ | ⬜ fixture + F5 |
| 4 Tạo thép | ✅ | ⬜ fixture + F5 |
| 5 Views/dim/tag + orchestrator | ✅ | ⬜ fixture + F5 |
| 6 UI 8 tab | ✅ | ⬜ F5 |
| 7 Canvas | ✅ | ⬜ F5 |
| 8 Integration + docs | ✅ docs | ⬜ F5 matrix |

**Code: xong hết.** ~6.5k dòng feature + ~1.5k Core. Build sạch 10/10 config (Debug + Release × R23–R27), 0 error, 0 warning CS. 99 test xUnit pass.

**Verify: chưa có version Revit nào chạy thật.** 16 test TUnit skip vì thiếu fixture. Chi tiết + cách chạy: [reports/f5-smoke-results.md](reports/f5-smoke-results.md).

**2 thứ chặn:** (a) `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` — spec ở `HPRebar/HPRebar.Tests/Fixtures/README.md`; (b) 1 lần F5 với Revit đóng trước khi build.

## Unresolved questions

0. **[MỚI — cook Phase 2]** `GetItemDivision` map shape không nhất quán khi thanh **có móc dưới**: nhánh `TopDowels!=0 && LaTop==0` trả `LaBottom>0 ? DS01 : DS04`, ngược với 2 nhánh còn lại (`DS04 : DS01`). Giống typo của tác giả gốc. Đã port nguyên bản (`BarShapeClassifier.cs`) vì sửa sẽ đổi family detail-shop đặt trong bản vẽ. **Hỏi user:** giữ nguyên hay sửa cho nhất quán?

1. ~~**Package `Nice3point.Revit.Api.RevitAPI 2027.*`** có tồn tại trên nuget.org không?~~ **ĐÃ GIẢI QUYẾT (cook Phase 0, 2026-09-03):** có. `dotnet build -c Debug.R27` restore + build **pass, 0 error**; resolve `Nice3point.Revit.Api.RevitAPI/2027.2.0` + `RevitAPIUI/2027.2.0` + `Toolkit/2027.0.0`, TFM `net10.0-windows7.0` (đúng như bảng đã sửa). R27 giữ trong scope, build-only.
2. **File `.rvt` mẫu + family `DS*`** từ tool gốc — user có không? Không có thì detail shop (Phase 5) chỉ verify được đường "family thiếu → bỏ qua", không verify được đường tạo thật.
   **[cook Phase 3]** Câu hỏi này giờ CHẶN Phase 3: 11 TUnit test build pass nhưng skip hết vì thiếu `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`. Spec đầy đủ ở `HPRebar/HPRebar.Tests/Fixtures/README.md`. Cần user dựng trong Revit 2026 (~30–60 phút) hoặc cung cấp file có sẵn.
3. **[MỚI — cook Phase 5]** `ViewSchedule` (detail component schedule) **đã bỏ** — bản gốc lọc field theo tên chuỗi tiếng Anh, và nó chỉ phục vụ detail shop vốn đã cắt. Cần lại thì làm cùng plan detail-shop.
4. **[MỚI — cook Phase 5]** Family `DS*` (detail shop) — user có `.rfa` không? Không có thì `DetailShopCreator` chưa viết được (code không test được lần nào). Cùng câu hỏi với `.rvt` fixture.

5. **`Bundle:VendorName`** trong `HPRebar/build/appsettings.json` vẫn là `"Development"` (template default) — đổi trước khi pack MSI? (Phase 8, hỏi user)
