# Multi-Version Strategy — Revit 2022–2027

Quản lý code support nhiều version Revit cùng codebase.

## Configurations

Project `.csproj` mặc định scaffold ra:

```xml
<PropertyGroup>
  <Configurations>Debug.R22;Debug.R23;Debug.R24;Debug.R25;Debug.R26;Debug.R27</Configurations>
  <Configurations>$(Configurations);Release.R22;Release.R23;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*" />
  <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitVersion).*" />
  <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitVersion).*" />
</ItemGroup>
```

`$(RevitVersion)` được Nice3point SDK auto-set theo configuration đã chọn:
- `Debug.R22` → `RevitVersion=22` → pull `Nice3point.Revit.Api.RevitAPI` 22.*
- `Debug.R27` → `RevitVersion=27` → pull 27.*

## Target Framework Matrix

| Revit | Target Framework | .NET Runtime | Note |
|---|---|---|---|
| 2022 | `net48` | .NET Framework 4.8 | Legacy |
| 2023 | `net48` | .NET Framework 4.8 | Legacy |
| 2024 | `net48` | .NET Framework 4.8 | Legacy |
| 2025 | `net8.0-windows` | .NET 8 | Modern, assembly isolation |
| 2026 | `net8.0-windows` | .NET 8 | |
| 2027 | `net8.0-windows` | .NET 8 | |

SDK tự switch — không cần config tay.

## Preprocessor Constants

| Constant | Đúng khi build cho |
|---|---|
| `REVIT2022` | Đúng Revit 2022 |
| `REVIT2023` | Đúng Revit 2023 |
| `REVIT2024` | Đúng Revit 2024 |
| `REVIT2025` | Đúng Revit 2025 |
| `REVIT2026` | Đúng Revit 2026 |
| `REVIT2027` | Đúng Revit 2027 |
| `REVIT2022_OR_GREATER` | Revit ≥ 2022 (luôn true với matrix này) |
| `REVIT2023_OR_GREATER` | Revit ≥ 2023 |
| `REVIT2024_OR_GREATER` | Revit ≥ 2024 |
| `REVIT2025_OR_GREATER` | Revit ≥ 2025 |
| `REVIT2026_OR_GREATER` | Revit ≥ 2026 |
| `REVIT2027_OR_GREATER` | Revit ≥ 2027 |

## API Breaking Change Cheatsheet (2022–2027)

### Units API (R21+ giới thiệu ForgeTypeId)

```csharp
#if REVIT2021_OR_GREATER
    var mm = UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters);
#else
    var mm = UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_MILLIMETERS);
#endif
```

### ElementId.IntegerValue (deprecated từ R23, dùng `.Value`)

```csharp
// Multi-version: ElementId.IntegerValue → ElementId.Value
#if REVIT2024_OR_GREATER
    long id = elementId.Value;
#else
    int id = elementId.IntegerValue;
#endif
```

### Built-in Category cast

```csharp
#if REVIT2024_OR_GREATER
    var builtinCategory = (BuiltInCategory)category.Id.Value;
#else
    var builtinCategory = (BuiltInCategory)category.Id.IntegerValue;
#endif
```

### Parameter API

```csharp
// Multi-version: BuiltInParameter access
#if REVIT2022_OR_GREATER
    var param = element.GetParameter(ParameterTypeId.HostAreaComputed);
#else
    var param = element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
#endif
```

### Document path API (R23+)

```csharp
#if REVIT2023_OR_GREATER
    var modelPath = document.GetWorksharingCentralModelPath();
#else
    var modelPath = document.GetCloudModelPath();   // R22 API name khác
#endif
```

## Quy tắc viết code đa version

1. **Mỗi `#if` block phải kèm comment `// Multi-version: <topic>`** để grep tìm được tất cả chỗ phụ thuộc version.
2. **Không lồng nested `#if`** quá 2 cấp — tách method riêng nếu phức tạp.
3. **Encapsulate API khác biệt vào helper class** dùng partial class theo version:
   ```
   Helpers/
   ├── ElementIdHelper.cs           ← shared signature
   ├── ElementIdHelper.R22.cs       ← #if REVIT2022 && !REVIT2024_OR_GREATER
   └── ElementIdHelper.R24Plus.cs   ← #if REVIT2024_OR_GREATER
   ```
4. **Test mỗi config trước release:**
   ```bash
   dotnet build -c Debug.R22
   dotnet build -c Debug.R24
   dotnet build -c Debug.R27
   ```

## Reference tài liệu

- Nice3point wiki: https://github.com/Nice3point/RevitTemplates/wiki/Managing-API-Compatibility
- Autodesk Revit API changelog: https://www.revitapidocs.com/
- Community ApiDocs: https://www.revitapidocs.com/2027/

## Anti-patterns

- ❌ Hardcode `RevitAPI.dll` path — Nice3point SDK tự resolve via NuGet.
- ❌ Build all configs cùng lúc khi không cần — tốn thời gian, chỉ build config đang dev.
- ❌ Reference `Autodesk.Revit.DB.Wall` từ namespace gốc — wrap qua extension nếu cần custom logic.
