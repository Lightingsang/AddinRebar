# Cấu hình đóng gói ILRepack cho MaterialDesignInXamlToolkit trong `.csproj`

Khi sử dụng `MaterialDesignInXamlToolkit` trong môi trường CAD/BIM (Revit, AutoCAD, Civil 3D,...), add-in chạy in-process trong cùng tiến trình host. Để tránh lỗi thiếu DLL (`FileNotFoundException`) hoặc xung đột phiên bản giữa các add-in, **bắt buộc phải gộp (merge) DLL của MaterialDesign vào DLL chính của add-in**.

---

## 1. Dành cho dự án Revit Add-In dùng `Nice3point.Revit.Sdk`

SDK Nice3point đã tích hợp sẵn ILRepack tự động. Chỉ cần khai báo trong file `.csproj`:

```xml
<Project Sdk="Nice3point.Revit.Sdk">
    <PropertyGroup>
        <!-- Bật tự động gộp dependencies vào DLL chính -->
        <IsRepackable>true</IsRepackable>
    </PropertyGroup>

    <ItemGroup>
        <!-- Khai báo MaterialDesignThemes (khuyên dùng 5.2.1 hoặc 5.3.2) -->
        <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
        <PackageReference Include="MaterialDesignColors" Version="5.2.1" />
    </ItemGroup>
</Project>
```

> **Lưu ý:** Với Nice3point, khi `<IsRepackable>true</IsRepackable>`, toàn bộ các DLL NuGet (trừ Revit API) sẽ tự động được đóng gói vào file assembly cuối cùng trong thư mục deploy `%AppData%\Autodesk\Revit\Addins\<version>\`.

---

## 2. Dành cho dự án AutoCAD / Civil 3D hoặc Standalone có ILRepack

Với các dự án AutoCAD.NET hoặc add-in không dùng SDK Nice3point, sử dụng package `ILRepack` kèm MSBuild Target `RepackMaterialDesign`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0-windows</TargetFramework>
        <UseWPF>true</UseWPF>
    </PropertyGroup>

    <ItemGroup>
        <!-- MaterialDesign Packages -->
        <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
        <PackageReference Include="MaterialDesignColors" Version="5.2.1" />
        <!-- ILRepack tool (PrivateAssets + ExcludeAssets để không copy ra output) -->
        <PackageReference Include="ILRepack" Version="2.0.46" PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true" />
    </ItemGroup>

    <!-- Target tự động gộp các DLL MaterialDesign vào assembly chính ngay sau khi build -->
    <Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
        <PropertyGroup>
            <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
            <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
        </PropertyGroup>
        <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;" />
        <!-- Xóa các file DLL rời rạc sau khi đã merge -->
        <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml" />
        <Message Importance="high" Text="RepackMaterialDesign: MaterialDesign đã được gộp thành công vào $(AssemblyName).dll" />
    </Target>
</Project>
```

---

## 3. Checklist kiểm tra sau khi build

- [ ] File output `.dll` của add-in có kích thước tăng lên (thường > 3MB do đã chứa BAML của MaterialDesign).
- [ ] Không còn file `MaterialDesignThemes.Wpf.dll` hay `MaterialDesignColors.dll` nằm trôi nổi trong thư mục deploy/output.
- [ ] Khởi chạy trong CAD/BIM không xuất hiện lỗi `System.IO.FileNotFoundException: Could not load file or assembly 'MaterialDesignThemes.Wpf'`.
- [ ] Font chữ hiển thị chuẩn `Segoe UI`, không bị crash liên quan đến `Roboto`.
