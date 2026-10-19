# revit-xaml-styles Skill Summary (Local Copy)
Source: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\revit-xaml-styles\SKILL.md`

## Key Rules:
1. MaterialDesignInXamlToolkit 5.2.1 / 5.3.2 mandatory for all WPF Add-Ins.
2. Segoe UI mandatory (override Roboto).
3. DynamicResource for all brushes/colors (no hardcoding, no StaticResource for brush/color).
4. Card layout & Outlined input controls.
5. Tiered action buttons (Primary, Secondary, Danger, Icon).
6. Dark/Light runtime switching via ResourceDictionary swapping.
7. Host neutrality and ILRepack compatibility.
