## 2026-09-07T07:24:36Z

<USER_REQUEST>
You are the Project Orchestrator for HPRebar (conversation ID to be tracked).
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_1
Repository root: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar
Source reference (read-only): F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar

Authoritative User Request is in:
f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md

Mission:
Refactor and migrate the complete continuous beam rebar generation module R02_BeamsRebar into the production architecture of HPRebar (AddinRebar), ensuring:
1. R1: Pure domain logic & geometry engine in HPRebar.Core/BeamRebar/ (netstandard2.0, zero Autodesk.Revit.* dependencies).
2. R2: Pure domain unit test suite in HPRebar.Core.Tests/BeamRebar/ (xUnit v3, 100% pass rate under dotnet test HPRebar.Core.Tests).
3. R3: Revit Add-In feature implementation in HPRebar/HPRebar/Beam Rebar/ adhering strictly to feature folder conventions, proper PascalCase namespaces, zero deprecated APIs, and atomic TransactionGroup.
4. R4: WPF MVVM UI in HPRebar/HPRebar/Beam Rebar/View/ & View Models/ with CommunityToolkit.Mvvm, Revit light/dark DynamicResource styling, and interactive WPF preview canvas.
5. R5: Ribbon integration in HPRebar/HPRebar/Application.cs.
6. Multi-version builds:
   - dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false (0 errors)
   - dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false (0 errors)

Follow all repository rules in AGENTS.md.
Maintain progress.md and BRIEFING.md in your directory. Keep progress.md regularly updated as you execute.
When all requirements and acceptance criteria are met, report completion to the Sentinel for Victory Audit.
</USER_REQUEST>
