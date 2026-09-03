# Prompt Infographic: Quy Trình Sử Dụng Nice3point RevitTemplates

Prompt dưới đây được tối ưu cho các model AI image hiện đại (Midjourney v7, FLUX.1 Pro/Ultra, Imagen 4, Ideogram 3.0, DALL·E 4, Recraft V4). Mỗi prompt được viết với **mật độ chi tiết tối đa** để AI có thể render đúng layout, typography, icon, và flow.

---

## 🎯 PROMPT MASTER — Bản Đầy Đủ Nhất (Khuyên Dùng)

> Copy nguyên đoạn dưới đây paste vào Midjourney / FLUX / Ideogram.

```
A hyper-detailed, ultra-professional vertical infographic poster titled "Nice3point RevitTemplates — From Zero to Revit Add-In in 30 Seconds", 9:16 aspect ratio (or 2:3 for print), portrait orientation, designed for software engineers and BIM developers.

OVERALL STYLE: Modern flat-design tech infographic, isometric 3D accents, clean Swiss typography, generous white space, restrained color palette inspired by Autodesk Revit's brand (deep navy #1A2A3F background base, accent blue #2D7FF9, Revit signature blue #0696D7, success green #22C55E, warning amber #F59E0B, pure white #FFFFFF, charcoal text #0F172A on white panels). Subtle dotted grid background. Soft long shadows under floating cards. High-contrast, magazine-quality, awwwards-grade visual hierarchy. Inspired by Stripe documentation, Linear changelog, and Vercel marketing pages.

HEADER SECTION (top 15%):
- Large bold sans-serif title "RevitTemplates" in white, weight 800, kerning -2
- Subtitle in lighter weight: "Bộ scaffolder C#/.NET chính thức cho Autodesk Revit Add-In"
- Small badge row showing: "MIT License" + "v6.2.2" + "422★ on GitHub" + "Multi-version: Revit 2023→2027"
- A small isometric icon cluster top-right: stylized Revit house wireframe + .NET logo + NuGet hexagon, connected by glowing blue lines

STEP 1 PANEL — "CÀI ĐẶT MỘT LẦN DUY NHẤT" (Install Once):
- Numbered circle "01" in bright blue
- Icon: download arrow into a terminal window
- Sub-step A: ".NET SDK installed" with checkmark — small Microsoft .NET purple icon
- Sub-step B: Code block on dark terminal background showing exactly:
    dotnet new install Nice3point.Revit.Templates
- Tooltip note: "Tải từ NuGet — đăng ký vào .NET Template Engine"
- Right side: small illustration of a NuGet package box flying into a folder labeled "Template Engine"

STEP 2 PANEL — "TẠO PROJECT" (Create Project):
- Numbered circle "02"
- Title: "Chọn 1 trong 6 templates"
- Six template cards arranged in a 2x3 grid, each card has icon + short-name + description:
    1. "revit-addin" — single-project quickstart, icon: small puzzle piece
    2. "revit-application" — modular entry-point, icon: anchor
    3. "revit-module" — business-logic module, icon: lego brick
    4. "revit-solution" — enterprise + CI/CD + installer, icon: factory building
    5. "revit-benchmark" — perf testing in Revit thread, icon: speedometer
    6. "revit-test" — TUnit unit tests with full Revit API, icon: flask/beaker
- Below the grid, a terminal snippet:
    dotnet new revit-addin --addinManifestType application --addinDiMode hosting --addinLogging true

STEP 3 PANEL — "CHỌN CẤU HÌNH" (Pick Your Revit Version):
- Numbered circle "03"
- Title: "Multi-version build từ 1 codebase"
- Visualization: a dropdown menu UI mockup labeled "Solution Configuration" showing options:
    Debug.R23  |  Debug.R24  |  Debug.R25  |  Debug.R26  |  Debug.R27 ✓
- Arrows pointing from each option to a Revit version badge (2023/2024/2025/2026/2027) with year colors
- Side callout: "MSBuild SDK tự động pick đúng RevitAPI.dll bằng `Version=\"$(RevitVersion).*\"`"
- A small code snippet showing #if REVIT2027_OR_GREATER preprocessor magic

STEP 4 PANEL — "CẤU TRÚC PROJECT" (Project Structure):
- Numbered circle "04"
- Title: "Những gì được sinh ra tự động"
- A clean file-tree visualization (monospace font, light-on-dark or dark-on-light):
    ├── Application.cs           ← Entry point (ExternalApplication)
    ├── Host.cs                  ← DI Container bootstrap
    ├── *.csproj                 ← SDK="Nice3point.Revit.Sdk"
    ├── *.addin                  ← Revit XML manifest
    ├── Commands/
    │   └── StartupCommand.cs    ← Logic của button Ribbon
    ├── Configuration/
    │   ├── HostingConfiguration.cs
    │   └── LoggerConfiguration.cs
    ├── Models/                  ← (Data layer)
    ├── ViewModels/
    │   └── MainViewModel.cs     ← CommunityToolkit.Mvvm
    ├── Views/
    │   ├── MainView.xaml        ← WPF UI
    │   └── MainView.xaml.cs
    └── Resources/Icons/
        ├── RibbonIcon16.png
        └── RibbonIcon32.png
- Annotated callouts on the right pointing to specific files, explaining each one in short Vietnamese tooltips

STEP 5 PANEL — "F5 → REVIT TỰ MỞ" (Press F5, Revit Launches):
- Numbered circle "05"
- Title: "Workflow Debug Tự Động"
- Horizontal flow diagram with 5 connected nodes (arrows between them):
    [Press F5] → [Build .csproj] → [Auto-copy DLL + .addin to %ProgramData%\Autodesk\Revit\Addins\] → [Launch Revit.exe] → [Debugger Attached, Breakpoints Active]
- Screenshot mockup of Revit ribbon at bottom showing a custom tab/panel called "Commands" with a button labeled "Execute" using the RibbonIcon
- Small floating label: "<LaunchRevit>true</LaunchRevit>" and "<DeployAddin>true</DeployAddin>"

STEP 6 PANEL — "KIẾN TRÚC BÊN TRONG" (Architecture Diagram):
- Numbered circle "06"
- Title: "MVVM + Dependency Injection + Serilog"
- A circular/hub architecture diagram with "Host (Microsoft.Extensions.Hosting)" at center
- Spokes going out to:
    - "View (WPF)" — icon: window
    - "ViewModel (ObservableObject)" — icon: gear
    - "Model" — icon: database
    - "Serilog Logger" — icon: scroll
    - "Revit API Services" — icon: Revit house wireframe
- Each spoke has a small "[Transient]" or "[Singleton]" label
- Bottom annotation: "CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection + Serilog"

STEP 7 PANEL — "DEPLOY & PHÁT HÀNH" (Ship It):
- Numbered circle "07"
- Title: "Đóng gói chuyên nghiệp"
- Three output pathways shown as three boxes side-by-side:
    A. "Local Deploy" → DLL copied automatically → icon: folder + checkmark
    B. "MSI Installer" → WixSharp build → icon: gift box with .msi label
    C. "Autodesk App Store Bundle" → PackageContents.xml → icon: storefront with Autodesk logo
- Below: small CI/CD pipeline strip showing "GitHub Actions ✓" and "Azure DevOps ✓" badges

FOOTER (bottom 8%):
- Three columns:
  - Left: "github.com/Nice3point/RevitTemplates" with small GitHub octocat
  - Middle: "nuget.org/packages/Nice3point.Revit.Templates" with NuGet hex icon
  - Right: "MIT License — Free for commercial use"
- Tiny tagline centered: "From 2 days of boilerplate to 30 seconds. Built by the Revit community."

VISUAL DETAILS TO ENFORCE:
- All Vietnamese text MUST render perfectly with correct diacritics (ă, â, ê, ô, ơ, ư, đ, etc.) — use a Vietnamese-compatible font like Inter, Be Vietnam Pro, or Plus Jakarta Sans
- All code snippets in monospace font (JetBrains Mono or Fira Code)
- Step number circles: gradient from #2D7FF9 to #0696D7, with subtle outer glow
- Connecting arrows between steps: thin, light-blue, slightly curved with arrowheads
- Small decorative isometric 3D elements: floating cubes representing DLLs, tiny Revit building wireframes, terminal windows tilted at 15°
- Iconography style: line icons with 2px stroke, consistent rounded corners (4px radius)
- Cards: white background, 24px corner radius, soft drop shadow (0 8px 24px rgba(15,23,42,0.12))
- Background: deep navy #0F172A with very subtle blue grid lines (5% opacity) and a soft radial gradient highlight behind the title
- Overall mood: trustworthy, technical, premium, like a developer documentation cover from a top-tier SaaS company

CRITICAL: render ALL TEXT CRISP AND LEGIBLE. No gibberish text, no fake code. The terminal commands must be exactly readable. Use clear hierarchy: H1 (title) 72pt, H2 (panel titles) 28pt, body 14pt, code 13pt monospace.

Style references: stripe.com infographics, linear.app launch posters, vercel.com marketing, dribbble top isometric infographics 2026, awwwards SOTD developer tools.

--ar 9:16 --style raw --quality 2 --v 7
```

---

## 🎯 PROMPT NGẮN GỌN — Bản Cô Đọng (Nếu Model Hạn Chế Ký Tự)

> Dành cho DALL·E 4, Imagen 4, hoặc khi cần prompt < 1500 chars.

```
Vertical 9:16 tech infographic titled "Nice3point RevitTemplates — Quy Trình Tạo Add-In Revit". Modern flat design + isometric accents. Color palette: deep navy #0F172A background, accent blue #2D7FF9, Revit blue #0696D7, white cards with soft shadows, accent green for success. Layout: 7 numbered step panels stacked vertically.

Step 1: Install — terminal showing "dotnet new install Nice3point.Revit.Templates"
Step 2: Create — six template cards (revit-addin, revit-application, revit-module, revit-solution, revit-benchmark, revit-test) each with icon and Vietnamese description
Step 3: Configure — dropdown showing "Debug.R23 ... Debug.R27", auto multi-version
Step 4: Project Structure — file tree showing Application.cs, Host.cs, Commands/, Views/, ViewModels/, Resources/Icons/
Step 5: F5 to Debug — flow: Press F5 → Build → Auto-deploy DLL → Launch Revit.exe → Breakpoint hits. Mock Revit ribbon with "Execute" button.
Step 6: Architecture — hub diagram: Host at center, spokes to View (WPF), ViewModel (MVVM), Serilog, Revit API
Step 7: Deploy — three boxes: Local copy / MSI installer / Autodesk App Store bundle. CI/CD badges: GitHub Actions, Azure DevOps.

Footer: github.com/Nice3point/RevitTemplates · MIT License · v6.2.2

Typography: Inter or Be Vietnam Pro for Vietnamese text with correct diacritics (ă â ê ô ơ ư đ). JetBrains Mono for code. Step numbers in gradient blue circles with glow. Iconography: 2px line icons, rounded corners. Style: Stripe + Linear documentation aesthetic, premium developer tool poster, awwwards quality. All Vietnamese characters must render crisply.
```

---

## 🎯 PROMPT MIDJOURNEY V7 — Tối Ưu Cú Pháp

```
infographic poster, vertical 9:16, "Nice3point RevitTemplates Workflow" :: 7 stacked numbered step panels :: Vietnamese tech documentation :: flat design with isometric 3D accents :: navy #0F172A background, electric blue #2D7FF9 accents, white cards :: terminal command "dotnet new install Nice3point.Revit.Templates" :: six template cards (revit-addin, revit-application, revit-module, revit-solution, revit-benchmark, revit-test) with line icons :: file tree visualization showing Application.cs Host.cs Commands ViewModels Views :: F5 debug flow diagram with Revit launching :: hub-and-spoke architecture diagram showing MVVM + DI + Serilog :: deploy options (local, MSI, Autodesk App Store) :: Inter font Vietnamese diacritics :: JetBrains Mono code blocks :: GitHub stars badge :: Stripe-quality typography :: awwwards 2026 :: premium developer tool poster --ar 9:16 --style raw --s 250 --v 7
```

---

## 🎯 PROMPT FLUX.1 PRO / IDEOGRAM — Cho Text Rendering Đẹp

> FLUX và Ideogram render text rất tốt — dùng prompt này khi cần tiếng Việt sắc nét.

```
A premium vertical infographic poster in Vietnamese, 9:16 ratio, designed for software developers. Title at the top in large bold white sans-serif: "RevitTemplates — Tạo Revit Add-In trong 30 Giây". Subtitle below in lighter weight: "Bộ scaffolder C#/.NET chính thức từ Nice3point".

The poster is divided into 7 numbered horizontal panels on a dark navy #0F172A background with subtle dotted grid texture. Each panel is a white rounded card with soft drop shadow.

Panel 1 "CÀI ĐẶT" — terminal with the exact command: dotnet new install Nice3point.Revit.Templates
Panel 2 "TẠO PROJECT" — 2x3 grid of template cards with names: revit-addin, revit-application, revit-module, revit-solution, revit-benchmark, revit-test. Each card has a 2px line icon.
Panel 3 "CHỌN PHIÊN BẢN" — dropdown showing Debug.R23, Debug.R24, Debug.R25, Debug.R26, Debug.R27 with Debug.R27 highlighted in green
Panel 4 "CẤU TRÚC FILE" — file tree in monospace: Application.cs, Host.cs, Commands/StartupCommand.cs, ViewModels/, Views/, Resources/Icons/
Panel 5 "NHẤN F5" — horizontal flow: Press F5 → Build → Deploy DLL → Launch Revit → Debug. Small mockup of Revit ribbon with "Execute" button.
Panel 6 "KIẾN TRÚC" — circular diagram with "Host" at center, arrows to View (WPF), ViewModel (MVVM), Serilog, Revit API
Panel 7 "DEPLOY" — three boxes: "Local Auto-copy", "MSI Installer", "Autodesk App Store Bundle". CI/CD badges below.

Footer: "github.com/Nice3point/RevitTemplates  ·  MIT License  ·  v6.2.2  ·  ⭐ 422"

Typography: Be Vietnam Pro for Vietnamese text with perfect diacritics (rendering ă, â, ê, ô, ơ, ư, đ correctly). JetBrains Mono for code. Color palette: navy background, electric blue accents #2D7FF9, Revit signature blue #0696D7, success green #22C55E for checkmarks. Step number circles: gradient blue with outer glow. Style inspired by Stripe documentation and Linear launch announcements. Ultra-detailed, premium, magazine-quality, awwwards-grade design.
```

---

## 🎯 BIẾN THỂ HORIZONTAL — Cho Landing Page / Banner

Nếu cần infographic ngang (16:9 hoặc 21:9) thay vì dọc:

```
Horizontal 16:9 infographic banner "RevitTemplates Workflow — From Zero to Revit Add-In in 30 Seconds". 

Layout: 7 steps arranged left-to-right in a horizontal swimlane with connecting arrows.

Each step is a vertical card with: numbered circle on top (01-07 in blue gradient), Vietnamese title, icon, and 2-3 lines of description.

Steps:
01 Cài Đặt — dotnet new install
02 Tạo Project — chọn template
03 Chọn Version — Debug.R23 → R27
04 Code Generated — 12 files structured
05 Press F5 — Revit auto-launches
06 Architecture — MVVM + DI + Serilog
07 Ship It — MSI / App Store

Background navy #0F172A, accent blue #2D7FF9, white cards, isometric 3D Revit building wireframes floating in background.

Vietnamese diacritics perfect. Code in JetBrains Mono. Be Vietnam Pro for headers. Stripe-quality design. Premium developer documentation aesthetic.

--ar 16:9 --style raw --v 7
```

---

## 📝 GHI CHÚ KHI DÙNG PROMPT

### Mẹo để text tiếng Việt render đúng

1. **Ưu tiên model có text-rendering tốt:** Ideogram 3.0, FLUX.1 Pro, Imagen 4 hơn Midjourney v7.
2. **Đặt text trong dấu nháy kép `"..."`** để model hiểu là phải render đúng nguyên văn.
3. **Chỉ định rõ font Vietnamese-compatible:** Be Vietnam Pro, Plus Jakarta Sans, Inter (có Vietnamese subset).
4. **Liệt kê diacritics trong prompt:** `(ă, â, ê, ô, ơ, ư, đ)` — nhắc model render đúng.

### Iteration strategy

- **Pass 1:** Generate với prompt master, lấy layout tốt nhất.
- **Pass 2:** Dùng inpainting / region edit để sửa các đoạn text bị lỗi.
- **Pass 3:** Upscale 4x bằng Magnific AI hoặc Topaz Gigapixel để in poster.

### Nếu muốn 1 panel chi tiết hơn

Tách prompt thành 7 prompt nhỏ — mỗi prompt chỉ tập trung render 1 step. Sau đó ghép trong Figma/Photoshop. Cách này cho chất lượng cao nhất nhưng tốn công.

### Output khuyến nghị

- **Aspect ratio:** 9:16 (story), 2:3 (poster in A2/A3), hoặc 16:9 (landing page hero)
- **Resolution:** ≥ 2048×3640 (FLUX Pro Ultra) hoặc upscale từ 1024×1820 (Midjourney)
- **Format:** PNG (giữ độ sắc nét text)

---

## 🎨 BẢNG MÀU CHÍNH THỨC (Cho Designer)

| Vai trò | Hex | Mô tả |
|---|---|---|
| Background | `#0F172A` | Navy đậm |
| Card BG | `#FFFFFF` | Trắng tinh |
| Primary | `#2D7FF9` | Xanh điện |
| Revit Blue | `#0696D7` | Xanh signature của Autodesk Revit |
| Success | `#22C55E` | Xanh lá (checkmark) |
| Warning | `#F59E0B` | Vàng cam (note) |
| Text Light | `#FFFFFF` | Trắng trên nền tối |
| Text Dark | `#0F172A` | Đen than trên card trắng |
| Muted | `#64748B` | Xám blue (text phụ) |
| Code BG | `#1E293B` | Slate đậm cho terminal |

## 🔤 FONT STACK GỢI Ý

- **Headers/Titles:** Be Vietnam Pro (Black/ExtraBold) — render diacritics hoàn hảo
- **Body:** Inter (Regular/Medium) hoặc Plus Jakarta Sans
- **Code:** JetBrains Mono (Regular/Bold) hoặc Fira Code
- **Numbers:** SF Mono hoặc IBM Plex Mono

---

Prompt này được thiết kế để có thể dùng ngay với tất cả các AI image generator hiện đại. Copy đoạn **PROMPT MASTER** ở đầu file là đủ để có infographic chất lượng cao nhất.
