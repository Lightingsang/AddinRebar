# revit-xaml-styles Skill Reference (Dump)
Summary: Standardizing WPF XAML Styles and Material Design 5.x for CAD/BIM Add-Ins.
- Obligatory MaterialDesignInXamlToolkit v5.2.1 or v5.3.2.
- Master Theme.xaml merges MaterialBridge.xaml FIRST.
- Font override: Segoe UI (Roboto FORBIDDEN).
- Color sync via DynamicResource (no hardcoded HEX, no StaticResource on colors/brushes).
- Card layouts (md:Card / Card.Panel).
- Outlined inputs with HintAssist.
- Button hierarchy (PrimaryButton, SecondaryButton, DangerButton, IconButton, LinkButton).
- ILRepack configuration.
