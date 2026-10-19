# revit-xaml-styles Skill Local Dump
Source: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\revit-xaml-styles\SKILL.md

Key Rules:
1. MaterialDesignInXamlToolkit v5.2.1 / v5.3.2 required.
2. MaterialBridge.xaml merged first in Theme.xaml.
3. Segoe UI mandatory override: <FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>. Roboto pack URIs strictly forbidden.
4. Dynamic theme switching via {DynamicResource} and MaterialThemeBridge.
5. In-process CAD/BIM packaging requires ILRepack merge. No PaletteHelper.
6. Standard controls inherit implicit MaterialDesign styles via Theme.xaml.
7. Buttons tiered: PrimaryButton (Raised), SecondaryButton (Outlined), DangerButton, IconButton, LinkButton.
8. TextBoxes/ComboBoxes outlined with floating hints.
