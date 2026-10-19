# revit-xaml-styles Local Skill Copy

Local copy of `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\revit-xaml-styles\SKILL.md`

Summary: Standardizes WPF XAML styles and Material Design for Revit/CAD add-ins using MaterialDesignInXamlToolkit v5.3.2.
Key rules:
1. Font Segoe UI override (prohibit Roboto)
2. Master Theme.xaml merges MaterialBridge.xaml first
3. Theme synchronization Dark/Light via DynamicResource
4. Card layout partitioning (md:Card / Card.Panel)
5. Outlined inputs with floating hints
6. md:PackIcon for icons
7. Tiered buttons (Primary, Secondary, Danger, IconButton)
8. ILRepack packaging clean merge
