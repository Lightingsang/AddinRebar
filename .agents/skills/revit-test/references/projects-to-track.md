# Revit Test Framework — Projects to Track

Danh sách project tham khảo + cách dùng từng project.

## ricaun-io/RevitTest

- **Repo:** https://github.com/ricaun-io/RevitTest
- **Loại:** Test runner trong Revit process, NUnit-based.
- **VS Adapter:** ✅ (NUnit Test Adapter)
- **Khi dùng:** Đã quen NUnit, muốn UI Test Explorer trong VS.
- **Cài:**
  ```bash
  dotnet add package ricaun.RevitTest.TestAdapter
  dotnet add package NUnit
  ```
- **Sample pattern:**
  ```csharp
  public class MyTests : RevitTestFixture
  {
      protected override string FileName => @"path\to\sample.rvt";

      [Test]
      public void TestSomething()
      {
          var doc = Document;
          // ...
      }
  }
  ```

## DynamoDS/RevitTestFramework

- **Repo:** https://github.com/DynamoDS/RevitTestFramework
- **Loại:** Legacy framework, dùng cho Dynamo + Revit.
- **VS Adapter:** NUnit GUI riêng.
- **Khi dùng:** Project legacy đã dùng RTF, hoặc cần Dynamo integration.
- **Note:** Maintenance giảm, ưu tiên ricaun-io cho project mới.

## specklesystems/xUnitRevit

- **Repo:** https://github.com/specklesystems/xUnitRevit
- **Loại:** xUnit syntax runner trong Revit process.
- **VS Adapter:** ✅ (xUnit Test Adapter)
- **Khi dùng:** Team xUnit-first muốn syntax quen thuộc.
- **Sample pattern:**
  ```csharp
  public class WallTests : IClassFixture<RevitFixture>
  {
      private readonly Document _doc;
      public WallTests(RevitFixture fixture) => _doc = fixture.Document;

      [Fact]
      public void GetWalls_ReturnsAll() { /* ... */ }
  }
  ```

## NeVeSpl/RevitTestLibrary

- **Repo:** https://github.com/NeVeSpl/RevitTestLibrary
- **Loại:** Helper utilities cho Revit test (assertions, fixtures).
- **Khi dùng:** Bổ sung extension method cho assertions Revit-specific.

## Nice3point revit-test (TUnit)

- **Repo:** https://github.com/Nice3point/RevitTemplates (template `revit-test`)
- **Loại:** Default test framework cho Nice3point ecosystem.
- **Framework:** TUnit (modern, fast, source-generator-based).
- **Khi dùng:** Project đã dùng Nice3point templates → match toolchain.
- **Cài:**
  ```bash
  dotnet new revit-test --name MyAddIn.Tests
  ```

## Comparison Matrix

| Project | Framework | Maintenance | Setup khó | Recommend cho |
|---|---|---|---|---|
| **Nice3point revit-test** | TUnit | Active | Easy (1 command) | Project Nice3point (default) |
| **ricaun-io RevitTest** | NUnit | Active | Medium | Cần VS Test Adapter + NUnit syntax |
| **xUnitRevit (Speckle)** | xUnit | Active | Medium | xUnit-first teams |
| **DynamoDS RTF** | NUnit | Legacy | Hard | Legacy projects only |
| **NeVeSpl** | Helper | Active | N/A | Combine với framework khác |

## Recommend cho project này

- **Mặc định:** Nice3point `revit-test` (TUnit) cho integration test.
- **Pure logic test:** xUnit thường (out-of-process, CI-friendly).
- **Fallback nếu TUnit issue trong Rider:** Switch sang ricaun-io RevitTest (VS Adapter mature hơn).

## Tham khảo bài blog gốc

[chuongmep.com — Run Revit Unit Test](https://chuongmep.com/posts/2024-06-01-run-revit-unit-test.html)
