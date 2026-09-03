---
title: "Refactor .claude → Tập trung phát triển Revit Add-In (Nice3point + WPF + MVVM Toolkit)"
date: 2026-05-17
status: pending-approval
owner: hoang.tran
scope: project
---

# Plan: Cleanup & Refactor `.claude/` cho Revit API 2027 Development

## 1. Mục tiêu (Why)

Hiện tại `.claude/` chứa **91 skills + 13 agents + 8 rules** — đa số định hướng web/mobile/Node/Python/Shopify/Stripe/React/etc, **không liên quan** đến stack thực tế của dự án này:

- **Stack thực tế:** C# / .NET 8 / Revit API 2023–2027 / WPF / CommunityToolkit.Mvvm / Nice3point.Revit.Sdk / Serilog / Microsoft.Extensions.Hosting.
- **IDE:** JetBrains Rider hoặc Visual Studio (Windows).
- **Workflow:** `dotnet new revit-addin` → F5 → debug trực tiếp trong Revit.

Việc giữ skills không liên quan gây 3 vấn đề:
1. **Routing nhiễu** — Claude có thể gợi ý sai skill (vd. `/bs:frontend-development` thay vì WPF).
2. **Ngốn dung lượng** — ~17 MB skills thừa, làm chậm index và làm bẩn metadata.
3. **Lệch kiến thức** — `ck-plan` / `ck-cook` hiện tại generic, không hiểu Nice3point template, không có rule WPF/XAML/MVVM.

## 2. Phạm vi (What)

### 2.1. Cleanup — Xóa skills KHÔNG liên quan

**Tiêu chí xóa:** skill chuyên cho web/mobile/JS/Python/AI media/database NoSQL/branding-thương-mại — không có khả năng được dùng cho phát triển Revit Add-In trong vòng đời dự án.

| Nhóm | Skills bị xóa | Lý do |
|---|---|---|
| Web FE | `frontend-design`, `frontend-development`, `react-best-practices`, `web-design-guidelines`, `web-frameworks`, `tanstack`, `stitch`, `ui-styling` (shadcn/Tailwind) | Stack web, không phải WPF |
| Web BE | `backend-development`, `better-auth`, `payment-integration`, `shopify` | API server, không phải desktop add-in |
| Mobile | `mobile-development`, `remotion` | iOS/Android/video — out of scope |
| Database server | `databases` (MongoDB/Postgres) | Revit dùng SQLite hoặc Revit internal model |
| DevOps web | `deploy`, `devops`, `mintlify` | Cloud/K8s/docs site — không deploy add-in lên cloud |
| Web testing | `web-testing` (Playwright) | Không test được Revit UI bằng browser |
| AI/Media | `ai-artist`, `ai-multimodal`, `media-processing`, `shader`, `threejs` | Không cần generate ảnh/video/3D-web |
| Misc | `cti-expert`, `google-adk-python`, `xia`, `agent-browser` (web only), `agentize`, `use-mcp`, `mcp-builder` | Out of scope ban đầu |
| Doc thừa | `agent_skills_spec.md` (top-level), `THIRD_PARTY_NOTICES.md` (skills root) | Doc thừa của bộ ClaudeKit |

**Tổng:** ~30 skills bị xóa (giảm ~12 MB).

### 2.2. KEEP — Skills giữ nguyên (core workflow + utility)

| Nhóm | Skills | Vai trò |
|---|---|---|
| Core workflow | `ck-plan`, `cook`, `ck-debug`, `code-review`, `test`, `scout`, `git`, `ship`, `fix` | Workflow chính |
| Plan/Project | `project-management`, `project-organization`, `plans-kanban`, `worktree` | Quản lý plan + worktree |
| Research/Think | `research`, `brainstorm`, `ask`, `sequential-thinking`, `problem-solving`, `ck-predict`, `ck-scenario`, `ck-loop`, `ck-autoresearch`, `ck-graphify` | Hỗ trợ thinking |
| Docs/Knowledge | `docs`, `docs-seeker`, `journal`, `watzup`, `retro`, `repomix`, `gkg`, `find-skills`, `llms` | Tra cứu + ghi chép |
| Visualization | `preview`, `tech-graph`, `excalidraw`, `mermaidjs-v11`, `markdown-novel-viewer`, `show-off` | Sơ đồ + xem |
| Security | `ck-security`, `security-scan` | Bảo mật code |
| Infra | `_shared`, `common`, `coding-level`, `context-engineering`, `bootstrap`, `skill-creator` | Hạ tầng skill |
| UI/UX general | `ui-ux-pro-max` | Nguyên tắc UX chung (tham khảo cho WPF) |
| Copy | `copywriting` | Viết doc/marketing course |
| Document export | `document-skills` | Xuất PDF/docx report từ add-in nếu cần |

### 2.3. NEW — Skills MỚI chuyên cho Revit

Tạo **4 skills mới** trong `.claude/skills/`:

#### a) `revit-addin/` — Scaffold + dev add-in với Nice3point

- **SKILL.md** mô tả 6 template: `revit-addin`, `revit-application`, `revit-module`, `revit-solution`, `revit-benchmark`, `revit-test`.
- Lệnh CLI mẫu: `dotnet new revit-addin --addinManifestType {application|dbApplication|command} --addinUiWpf {true|false} --addinDiMode {disabled|container|hosting} --addinLogging {true|false}`.
- Convention: kebab-case folder, `<UseWPF>true</UseWPF>`, `<DeployAddin>true</DeployAddin>`, `<LaunchRevit>true</LaunchRevit>`, `<IsRepackable>true</IsRepackable>`, multi-config `Debug.R23..R27`.
- Preprocessor directives: `REVIT2023`..`REVIT2027`, `_OR_GREATER`.
- `references/nice3point-toolkit.md` — extension methods: `CreatePanel`, `AddPushButton<T>()`, `SetImage`, `SetLargeImage`.
- `references/multi-version-strategy.md` — quản lý compat 2023–2027.

#### b) `revit-wpf-mvvm/` — Chuẩn WPF + CommunityToolkit.Mvvm

- **SKILL.md** quy ước:
  - ViewModel: `sealed partial class XxxViewModel : ObservableObject`, dùng `[ObservableProperty]` và `[RelayCommand]` thay cho boilerplate.
  - Constructor injection cho `ILogger<T>` và services.
  - View: `XxxView.xaml` + code-behind tối thiểu (chỉ `InitializeComponent()`), `DataContext` set qua DI container, KHÔNG set trong XAML.
  - Convention naming: `View` / `ViewModel` / `Model` folders, file tên `<Feature>View.xaml`, `<Feature>ViewModel.cs`.
- `references/mvvm-toolkit-patterns.md`:
  - `[ObservableProperty]` + partial property
  - `[RelayCommand(CanExecute = nameof(CanX))]`
  - `[NotifyCanExecuteChangedFor(nameof(SaveCommand))]`
  - `IRecipient<TMessage>` + `WeakReferenceMessenger`
  - Async command với `CancellationToken`
- `references/wpf-do-dont.md`:
  - ❌ Không dùng `RelayCommand` thủ công khi đã có toolkit.
  - ❌ Không set `DataContext` trong XAML khi dùng DI.
  - ❌ Không bind trực tiếp `Window.Owner` với Revit `MainWindowHandle` trong XAML — dùng `WindowInteropHelper` trong code.
  - ✅ Dùng `Loaded` event để init thay vì constructor.
  - ✅ Mọi long-running command phải `async Task` + có `CancellationToken`.

#### c) `revit-xaml-styles/` — Chuẩn hóa style XAML

- **SKILL.md** chứa file `references/styles/` gồm:
  - `Colors.xaml` — bảng màu theo Revit dark/light theme (match Revit 2024+ UI).
  - `Typography.xaml` — font Roboto/Segoe UI cỡ chuẩn (12, 14, 16, 20).
  - `Buttons.xaml` — `PrimaryButton`, `SecondaryButton`, `IconButton`, `DangerButton`.
  - `TextBoxes.xaml` — `StandardTextBox`, `NumberTextBox`, `SearchTextBox` với placeholder.
  - `Controls.xaml` — `Card`, `Separator`, `Badge`, `Tag`.
  - `Theme.xaml` — `ResourceDictionary` merge tất cả; project chỉ cần include `<ResourceDictionary Source="pabs://application:,,,/MyAddIn;component/Themes/Theme.xaml"/>` vào `App.xaml` hoặc Window resources.
- Naming: PascalCase cho style key (`PrimaryButton`), x:Key bắt buộc, không dùng implicit style cho `Button` toàn cục để tránh đụng UI Revit gốc.
- Sizing: dùng `{StaticResource Spacing.Small/Medium/Large}` (4/8/16/24/32 px).
- `references/style-do-dont.md`:
  - ❌ Không hardcode color `#FF0000` — dùng `{StaticResource Color.Danger}`.
  - ❌ Không dùng `Margin="5,3,7,2"` random — chỉ dùng multiples của 4.
  - ❌ Không dùng `FontFamily` lung tung — chỉ `Typography.Body/Heading/Caption`.
  - ✅ Bố cục dùng `Grid` + `*` / `Auto` thay vì `Margin` hack.
  - ✅ Mọi `UserControl` mới phải reference `Theme.xaml` ở root.

#### d) `revit-debug/` — Debug add-in trong Revit process

- F5 workflow: build cấu hình `Debug.R<XX>`, MSBuild copy DLL vào `%ProgramData%\Autodesk\Revit\Addins\<version>\`, launch Revit.exe + attach debugger.
- Troubleshoot:
  - `FileLoadException` → bật `<IsRepackable>true</IsRepackable>` để ILRepack gộp DLL.
  - Add-in không hiện → check `<DeployAddin>true</DeployAddin>` + verify `%ProgramData%\Autodesk\Revit\Addins\` có `.addin` và DLL.
  - Build fail `RevitAPI.dll not found` → đổi configuration cho đúng Revit version đã cài.
  - F5 không mở Revit → check `<LaunchRevit>true</LaunchRevit>`.
- Logging: dùng `Serilog` với sink `File` ghi vào `%AppData%/<addin>/logs/`.

### 2.4. Refactor `ck-plan` SKILL.md

**Thay đổi chính:**
- Thêm section **"Stack-Aware Planning"** ép `ck-plan` mặc định mở rộng phase cho Revit:
  - **Phase 0 — Scaffold:** chọn template (`revit-addin` / `revit-application+module` / `revit-solution`), chốt parameters DI mode, WPF, Logging.
  - **Phase 1 — Multi-version:** chốt target Revit versions (vd 2024–2027), set `<Configurations>`.
  - **Phase 2 — Architecture:** chia `Commands/`, `ViewModels/`, `Views/`, `Models/`, `Services/`, `Resources/Icons/`. Quyết định DI mode (`disabled` / `container` / `hosting`).
  - **Phase 3 — WPF UI:** liệt kê View/ViewModel cần tạo, mapping với XAML style đã chuẩn hóa.
  - **Phase 4 — Revit API integration:** xác định Transaction strategy (`TransactionMode.Manual`), External Event nếu cần modeless UI.
  - **Phase 5 — Deploy:** build Release, msi/bundle nếu cần.
- Cập nhật `references/scope-challenge.md` thêm câu hỏi Revit-specific: "Bao nhiêu version Revit cần support?", "Modal hay modeless UI?", "Có cần background processing (DBApplication)?".
- Thêm `references/revit-checklist.md` — checklist 15 câu hỏi trước khi plan add-in.
- Xóa references không dùng: `red-team-personas.md` chỉ giữ section liên quan engineering (bỏ marketing/business personas).

### 2.5. Refactor `cook` SKILL.md

**Thay đổi chính:**
- **HARD-GATE-SCOUT-FIRST** thêm bullet: nếu phát hiện `.csproj` với `Sdk="Nice3point.Revit.Sdk"`, MUST đọc `.csproj` để lấy: `RevitVersion`, `Configurations`, `EnableDynamicLoading`, list `PackageReference`.
- **Required Subagents** thêm dòng:
  | Phase | Subagent | Requirement |
  | XAML work | `revit-wpf-mvvm` + `revit-xaml-styles` skills | **MUST** activate nếu task chạm `.xaml` hoặc `ViewModel` |
  | Build verify | dotnet CLI | **MUST** chạy `dotnet build -c Debug.R<XX>` sau implement |
- **Anti-rationalization** thêm:
  | Thought | Reality |
  | "Tôi sẽ inline style cho nhanh" | Inline phá chuẩn → debt. Dùng `Theme.xaml` |
  | "DataContext set trong XAML cho gọn" | Phá DI → ViewModel không inject được logger/service. Set qua container |
- **Workflow Process** thêm bước:
  - Sau Implement, trước Test: chạy `dotnet build -c Debug.R<active-version>` — fail thì block, không tự skip.
  - Test: dùng `revit-test` template (TUnit) chạy trong Revit process, không dùng xUnit/NUnit out-of-process.

### 2.6. Refactor Rules

| File | Action |
|---|---|
| `skill-domain-routing.md` | Viết lại toàn bộ — bỏ web/mobile/etc, thêm cây quyết định: Revit/WPF/MVVM/XAML/Build/Debug/Test/Deploy/Multi-version |
| `skill-workflow-routing.md` | Cập nhật workflow Revit: `/bs:plan` → `/bs:cook` → `revit-debug` → `test` → `/bs:code-review` → `/bs:ship` |
| `primary-workflow.md` | Thêm step Revit-specific: F5 verify build, attach process, manual smoke test trong Revit UI |
| `development-rules.md` | Thêm section **C# / .NET / WPF**: file < 300 dòng (WPF code-behind có thể dài hơn 200), naming `PascalCase` cho `.cs`, kebab-case cho XAML asset file, `nullable enable`, target framework match Revit version |
| `documentation-management.md` | Giữ nguyên |
| `orchestration-protocol.md` | Giữ nguyên |
| `review-audit-self-decision.md` | Giữ nguyên |
| `team-coordination-rules.md` | Giữ nguyên |

### 2.7. Refactor Agents

| Agent | Action |
|---|---|
| `planner.md` | Thêm section **"Revit context awareness"** — nhận diện Nice3point project và đề xuất phase Revit-specific |
| `code-reviewer.md` | Thêm checklist WPF/MVVM: không hardcode color/margin, dùng `[ObservableProperty]`/`[RelayCommand]`, DI inject đúng cách, không leak `IDisposable` |
| `tester.md` | Thêm: ưu tiên `revit-test` (TUnit, chạy trong Revit process) thay vì xUnit/NUnit |
| `debugger.md` | Thêm Revit-specific troubleshooting (5 case trong Section 2.3.d) |
| `fullstack-developer.md` | Đổi tên/scope hoặc xóa — tên "fullstack" misleading với desktop add-in. Đề xuất: **xóa** vì `cook` đã đủ |
| `ui-ux-designer.md` | Thêm bullet: thiết kế cho desktop WPF (≥ 800×600), match Revit dark theme, dùng `Theme.xaml` |
| `journal-writer.md`, `docs-manager.md`, `git-manager.md`, `project-manager.md`, `brainstormer.md`, `researcher.md`, `code-simplifier.md` | Giữ nguyên |

## 3. Kiến trúc trước/sau

### Trước

```
.claude/
├── agents/        (13 agents — 1 không phù hợp)
├── rules/         (8 rules — routing nhiễu)
├── skills/        (91 skills — ~30 thừa, 0 chuyên Revit)
└── ...
```

### Sau

```
.claude/
├── agents/        (12 agents — đã chỉnh planner/reviewer/tester/debugger/ui-ux cho Revit)
├── rules/         (8 rules — routing chỉ Revit/WPF/C#)
├── skills/
│   ├── (core workflow + utility — giữ)
│   ├── revit-addin/          ← MỚI: Nice3point scaffold + workflow
│   ├── revit-wpf-mvvm/       ← MỚI: MVVM toolkit patterns
│   ├── revit-xaml-styles/    ← MỚI: Theme.xaml + styles chuẩn
│   └── revit-debug/          ← MỚI: F5 debug + troubleshoot
└── ...
```

## 4. Implementation Phases

### Phase 1 — Cleanup (Est: 30 phút)
1. [ ] Xóa ~30 skill folders đã liệt kê ở 2.1.
2. [ ] Xóa `agent_skills_spec.md` và `THIRD_PARTY_NOTICES.md` trong `.claude/skills/`.
3. [ ] Xóa `agents/fullstack-developer.md`.
4. [ ] Verify: `ls .claude/skills/ | wc -l` ≈ 60.

### Phase 2 — Tạo skills Revit mới (Est: 2h)
1. [ ] Tạo `.claude/skills/revit-addin/SKILL.md` + `references/nice3point-toolkit.md` + `references/multi-version-strategy.md`.
2. [ ] Tạo `.claude/skills/revit-wpf-mvvm/SKILL.md` + `references/mvvm-toolkit-patterns.md` + `references/wpf-do-dont.md`.
3. [ ] Tạo `.claude/skills/revit-xaml-styles/SKILL.md` + `references/styles/Colors.xaml`, `Typography.xaml`, `Buttons.xaml`, `TextBoxes.xaml`, `Controls.xaml`, `Theme.xaml`.
4. [ ] Tạo `.claude/skills/revit-debug/SKILL.md` + troubleshoot table.

### Phase 3 — Refactor ck-plan + cook (Est: 1h)
1. [ ] Edit `ck-plan/SKILL.md`: thêm section "Stack-Aware Planning" + 6 phase Revit default.
2. [ ] Thêm `ck-plan/references/revit-checklist.md`.
3. [ ] Edit `cook/SKILL.md`: thêm scout bullet cho `.csproj` Nice3point + `dotnet build` gate + WPF subagent + anti-rationalization.
4. [ ] Update `cook/references/workflow-steps.md` cho Revit context.

### Phase 4 — Refactor rules (Est: 45 phút)
1. [ ] Viết lại `rules/skill-domain-routing.md` — bỏ web/mobile, thêm cây Revit/WPF.
2. [ ] Update `rules/skill-workflow-routing.md` — Revit workflow.
3. [ ] Update `rules/primary-workflow.md` — F5 verify step.
4. [ ] Update `rules/development-rules.md` — C#/.NET/WPF conventions.

### Phase 5 — Refactor agents (Est: 30 phút)
1. [ ] Edit `agents/planner.md` — Revit context.
2. [ ] Edit `agents/code-reviewer.md` — WPF/MVVM checklist.
3. [ ] Edit `agents/tester.md` — TUnit/revit-test.
4. [ ] Edit `agents/debugger.md` — Revit troubleshoot.
5. [ ] Edit `agents/ui-ux-designer.md` — WPF desktop guidelines.

### Phase 6 — Verify & docs (Est: 30 phút)
1. [ ] Tạo/cập nhật `docs/code-standards.md` để link tới `revit-xaml-styles` và `revit-wpf-mvvm`.
2. [ ] Tạo `docs/system-architecture.md` mô tả Nice3point project structure.
3. [ ] Cập nhật `CLAUDE.md` reference các skill mới (nếu cần).
4. [ ] Smoke test: chạy 1 lần `dotnet new revit-addin` thử trong `output/` (tùy chọn).

**Tổng thời gian ước tính:** ~5 giờ.

## 5. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Xóa nhầm skill đang dùng | Bước 1 commit riêng (`chore: cleanup unused .claude skills`) → revert dễ |
| `Theme.xaml` conflict với Revit UI gốc | Dùng `x:Key` explicit, KHÔNG implicit style `Button` global |
| Multi-version preprocessor phức tạp | Chỉ start với 1–2 version (R26+R27), mở rộng khi cần |
| Skill mới không trigger | Viết description theo pattern "TRIGGER when:" giống skills khác |
| DI Hosting nặng cho add-in nhỏ | `revit-addin` mặc định `disabled`, chỉ `hosting` khi dự án lớn |

## 6. Success Criteria

- [ ] `ls .claude/skills/` còn ≤ 65 entries (giảm > 25%).
- [ ] 4 skill Revit mới có SKILL.md ≥ 80 dòng + ≥ 1 reference doc.
- [ ] `ck-plan` khi gọi với task "tạo add-in revit XYZ" tự đề xuất 6 phase Revit-specific (Scaffold/Multi-version/Architecture/WPF/Revit API/Deploy).
- [ ] `cook` khi detect `.csproj` Nice3point bắt buộc `dotnet build` trước test.
- [ ] `rules/skill-domain-routing.md` không còn từ "React", "Vue", "Next.js", "Flutter", "Stripe", "MongoDB" trừ trong block "không dùng".
- [ ] `Theme.xaml` merge được 5 file dictionary con, build thử với template `revit-addin` không lỗi.

## 7. Quyết định đã chốt (user approved 2026-05-17)

| # | Quyết định | Hệ quả |
|---|---|---|
| 1 | **GIỮ** `mcp-builder`, `agentize`, `use-mcp` | Phase 1 KHÔNG xóa nhóm MCP |
| 2 | Support **Revit 2022–2027** (6 versions) | `<Configurations>Debug.R22;...;Debug.R27;Release.R22;...;Release.R27</Configurations>`. Preprocessor: `REVIT2022..REVIT2027` và `_OR_GREATER` |
| 3 | WPF theme **Dark + Light** dùng `DynamicResource` | `Theme.xaml` chia `ThemeDark.xaml` + `ThemeLight.xaml`, MergedDictionaries swap runtime. Mọi style dùng `{DynamicResource Color.Background}` thay vì `{StaticResource ...}` |
| 4 | DI mode mặc định **`container`** | `revit-addin` skill default `--addinDiMode container`; chỉ `hosting` cho project lớn (revit-solution) |
| 5 | **XÓA** `agents/fullstack-developer.md` | Phase 5 thêm bước xóa |
| 6 | Test: **TUnit** (in-process) + **xUnit** (pure-logic), tham khảo [ricaun-io/RevitTest](https://github.com/ricaun-io/RevitTest) | Tạo thêm skill `revit-test/` (xem 7.1) |
| 7 | **GIỮ** `document-skills` | Phục vụ xuất BOM/schedule PDF/Excel từ add-in |

### 7.1. Bổ sung Phase 2 — Skill thứ 5: `revit-test/`

User confirm Q6 yêu cầu xem xét cả TUnit + xUnit + Revit Test Framework → tách riêng skill về test.

**`revit-test/SKILL.md` matrix:**

| Loại test | Framework | Khi nào dùng |
|---|---|---|
| In-process (cần Revit context) | **TUnit** (Nice3point `revit-test` template) | Test gọi `Document`, `Transaction`, `Element`, `FilteredElementCollector` |
| In-process alternative | **xUnitRevit / RevitTest** (ricaun-io) | Team quen xUnit, muốn dùng VS Test Adapter UI |
| Out-of-process (pure logic) | **xUnit** thường | Test parser, calculator, geometry math KHÔNG cần Revit API |

**`references/test-setup-rider.md`** (theo blog chuongmep.com):
- Enable "VS Code Adapter Support" trong Settings → Unit Testing
- Switch Build → Visual Studio mode nếu .NET SDK gặp issue
- Tab NUnit → đổi "Metadata" → "TestRunner"
- Refresh Unit Test Tree

**`references/test-setup-visual-studio.md`:**
- Cài NUnit Test Adapter extension
- Reference [ricaun-io/RevitTest](https://github.com/ricaun-io/RevitTest)
- Set test model qua override:
  ```csharp
  protected override string FileName => @"C:\path\to\test-model.rvt";
  ```

**`references/projects-to-track.md`:**
- [ricaun-io/RevitTest](https://github.com/ricaun-io/RevitTest)
- [DynamoDS/RevitTestFramework](https://github.com/DynamoDS/RevitTestFramework)
- [specklesystems/xUnitRevit](https://github.com/specklesystems/xUnitRevit)
- [NeVeSpl/RevitTestLibrary](https://github.com/NeVeSpl/RevitTestLibrary)
- Blog reference: https://chuongmep.com/posts/2024-06-01-run-revit-unit-test.html

### 7.2. Cập nhật scope so với Section 2.3

- Skill mới: **5** thay vì 4 (`revit-addin`, `revit-wpf-mvvm`, `revit-xaml-styles`, `revit-debug`, **`revit-test`**).
- Skill bị xóa: **giảm còn ~27** (giữ lại nhóm MCP).
- Total Phase 2 effort: +30 phút (từ 2h → 2.5h).

### 7.3. Cập nhật Section 4 — Phase 2 checklist

- [ ] Tạo `.claude/skills/revit-test/SKILL.md` + 3 reference docs (rider/VS/projects-to-track).

### 7.4. Cập nhật Section 6 — Success Criteria (revised)

- [ ] `ls .claude/skills/` còn ≤ 70 entries (giảm ~25%, đã trừ MCP giữ lại).
- [ ] **5 skills** Revit mới đều có SKILL.md ≥ 80 dòng + ≥ 1 reference doc.
- [ ] `Theme.xaml` có 2 file con `ThemeDark.xaml` + `ThemeLight.xaml`, switch runtime qua command `SwitchTheme`.
- [ ] Mọi style dùng `DynamicResource`, KHÔNG còn `StaticResource` cho color/brush.
- [ ] Template scaffold mặc định ra config `Debug.R22..R27`, preprocessor đầy đủ 6 version.
- [ ] `revit-test/SKILL.md` mô tả cả 3 framework (TUnit/xUnitRevit/xUnit) với decision tree.
- [ ] `agents/fullstack-developer.md` đã xóa.
- [ ] `ck-plan` khi gọi với task chứa "revit/add-in/addin/ribbon/wpf" tự đề xuất 6 phase Revit-specific.
- [ ] `cook` khi detect `Sdk="Nice3point.Revit.Sdk"` bắt buộc `dotnet build -c Debug.R<XX>` trước test.

### 7.5. Tổng effort cập nhật

| Phase | Effort cũ | Effort mới |
|---|---|---|
| 1. Cleanup | 30' | 30' |
| 2. Skills mới | 2h | **2h30'** (+ skill `revit-test`) |
| 3. Refactor ck-plan + cook | 1h | 1h |
| 4. Refactor rules | 45' | 45' |
| 5. Refactor agents | 30' | 30' |
| 6. Verify & docs | 30' | 30' |
| **Tổng** | **5h** | **~5h45'** |

---

## 8. Sẵn sàng triển khai

Tất cả 7 câu hỏi đã chốt + scope đã cập nhật. Tôi sẽ chạy tuần tự **Phase 1 → 6** ở Section 4 (kèm bổ sung `revit-test` ở Phase 2.5).

**Reply "GO" để tôi bắt đầu Phase 1 (Cleanup).**
