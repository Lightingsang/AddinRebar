# Revit Add-In Planning Checklist

15 câu hỏi PHẢI trả lời trước khi finalize plan Revit add-in. Dùng `AskUserQuestion` khi không tự suy luận được.

## 1. Project scope

1. **Type:** Add-in mới hay extend code có sẵn?
2. **Template:** `revit-addin` (đơn) hay `revit-application + revit-module` (modular)?
3. **Manifest type:** `Application` (Ribbon), `DBApplication` (headless), hay `Command` (1 button)?

## 2. Multi-version

4. **Revit versions support:** Liệt kê cụ thể (mặc định 2022–2027).
5. **Có code phụ thuộc API version-specific** không? (vd. ForgeTypeId R21+, ElementId.Value R23+)
6. **Cần test trên bao nhiêu version** trước khi release?

## 3. UI architecture

7. **Modal hay Modeless?** Modal đơn giản, Modeless cần ExternalEvent.
8. **DI mode:** `disabled` (1 command đơn), `container` (default), `hosting` (project lớn)?
9. **Theme:** Dark only / Light only / cả 2 (DynamicResource)?
10. **Logging:** Serilog file? Console? Cả 2?

## 4. Business logic

11. **Read-only hay modify document?** Modify cần Transaction.
12. **Performance budget:** Time limit cho command (vd. < 5s cho UI, < 30s cho batch)?
13. **Element scope:** Hoạt động trên Active Document, Selection, hay toàn workshare?

## 5. Output & deployment

14. **Output artifact:** DLL deploy local, .msi installer, hay Autodesk App Store bundle?
15. **Có cần xuất file** (PDF/Excel/IFC/JSON report) không? (Activate `document-skills` nếu có)

## Decision matrix mặc định

Nếu user không trả lời cụ thể → dùng default sau (đã chốt trong project này):

| Câu | Default |
|---|---|
| 2. Template | `revit-addin` (đơn) trừ khi project có 3+ modules logic độc lập |
| 3. Manifest | `Application` (90% case) |
| 4. Versions | 2022–2027 (6 versions) |
| 7. UI | Modal (đơn giản hơn) |
| 8. DI mode | `container` |
| 9. Theme | Dark + Light với DynamicResource |
| 10. Logging | Serilog file (`%LocalAppData%\<addin>\logs\`) |
| 12. Performance | < 5s cho UI command, > 5s phải có progress bar + Cancel |
| 14. Output | DLL local deploy (skill `revit-debug` handle) |

## Anti-patterns trong planning

❌ Plan có "Phase 1: Setup project" mà không xác định template Nice3point cụ thể.
❌ Plan modify document mà không có "Transaction strategy" rõ ràng.
❌ Plan modeless UI mà không có ExternalEvent trong architecture.
❌ Plan đa version mà không có table preprocessor matrix.
❌ Plan WPF UI mà không reference skill `revit-xaml-styles` hoặc Theme.xaml.

## Sample plan structure (Revit add-in)

```
plans/<timestamp>-<feature-name>/
├── plan.md                              ← Overview, link 6 phase
├── phase-00-scaffold.md                 ← Template + DI + Logging
├── phase-01-multi-version.md            ← Configurations + preprocessor matrix
├── phase-02-architecture.md             ← Folder structure + DI registration
├── phase-03-wpf-ui.md                   ← View/ViewModel + Theme.xaml
├── phase-04-revit-api.md                ← Transaction + ExternalEvent + Collectors
├── phase-05-test-deploy.md              ← TUnit setup + build Release
└── reports/
    └── researcher-revit-api-feasibility.md
```
