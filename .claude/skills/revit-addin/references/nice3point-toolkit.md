# Nice3point.Revit.Toolkit — Extension Method Catalog

Tham khảo nhanh các extension method giúp viết Revit API ngắn gọn hơn. Full source: https://github.com/Nice3point/RevitTemplates

## UIControlledApplication / UIApplication

| Extension | Thay cho | Ví dụ |
|---|---|---|
| `CreatePanel(panelName, tabName)` | `CreateRibbonPanel` + null check | `var panel = app.CreatePanel("Commands", "MyAddIn");` |
| `CreateTab(tabName)` | `CreateRibbonTab` | `app.CreateTab("MyAddIn");` |

## RibbonPanel

| Extension | Thay cho | Ví dụ |
|---|---|---|
| `AddPushButton<TCommand>(text)` | `new PushButtonData(...)` + `AddItem` cast | `panel.AddPushButton<StartupCommand>("Execute")` |
| `AddPullDownButton(name, text)` | `new PulldownButtonData(...)` | `panel.AddPullDownButton("dropdown", "Tools")` |
| `AddSplitButton(name, text)` | `new SplitButtonData(...)` | |
| `AddStackedItems(...)` | Stacked buttons API | |

## PushButton (fluent)

| Extension | Tác dụng | Ví dụ |
|---|---|---|
| `SetImage(path)` | Set icon 16px | `.SetImage("/MyAddIn;component/Resources/Icons/RibbonIcon16.png")` |
| `SetLargeImage(path)` | Set icon 32px | `.SetLargeImage("/MyAddIn;component/Resources/Icons/RibbonIcon32.png")` |
| `SetToolTip(text)` | Tooltip ngắn | `.SetToolTip("Run main command")` |
| `SetLongDescription(text)` | Tooltip dài | |
| `SetAvailabilityController<T>()` | Disable button khi `IExternalCommandAvailability` trả false | |

## Element (Nice3point.Revit.Extensions)

| Extension | Thay cho | Ví dụ |
|---|---|---|
| `GetParameter(builtIn)` | `get_Parameter(...)` | `el.GetParameter(BuiltInParameter.HOST_AREA_COMPUTED)` |
| `GetParameterValue<T>(name)` | Manual cast | `el.GetParameterValue<double>("Width")` |
| `SetParameterValue(name, value)` | `LookupParameter(...).Set(...)` | `el.SetParameterValue("Comments", "OK")` |

## Document

| Extension | Mô tả |
|---|---|
| `GetElements<T>()` | `FilteredElementCollector` + `OfClass(typeof(T))` + cast |
| `GetElementsOfCategory(BuiltInCategory)` | Filter theo category |
| `NewTransaction(name)` | `using var t = doc.NewTransaction("..."); t.Start(); ...; t.Commit();` |

## Convention khi dùng toolkit

- ✅ Dùng generic `AddPushButton<TCommand>()` thay vì truyền `typeof(...)` string — type-safe.
- ✅ Chain fluent `.SetImage().SetLargeImage().SetToolTip()` trên cùng dòng nếu < 4 chain, xuống dòng nếu > 4.
- ❌ Không mix toolkit + Revit API gốc trong cùng method — nhất quán 1 style.
- ❌ Không tự viết lại extension đã có trong toolkit (vi phạm DRY).

## Khi nào KHÔNG dùng toolkit

- API edge case toolkit chưa wrap → fallback Revit API gốc, comment `// Toolkit chưa support <API name> tại version X`.
- Extension method conflict với extension custom của project → đặt alias namespace.

## Bổ sung resource

- Source code: https://github.com/Nice3point/RevitTemplates/tree/main/Source/Nice3point.Revit.Toolkit
- Sample app: https://github.com/jeremytammik/RevitLookup (uses toolkit production-grade)
