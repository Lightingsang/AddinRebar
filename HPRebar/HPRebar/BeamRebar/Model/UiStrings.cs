namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Complete set of UI strings for the Continuous Beam Rebar dialog in one language.
/// </summary>
public sealed record UiStrings
{
    // Window Chrome
    public string WindowTitle { get; init; } = "Continuous Beam Rebar";
    public string Ok { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancel";
    public string LanguageToggle { get; init; } = "VN";
    public string Working { get; init; } = "Creating beam reinforcement...";
    public string NothingToCreate { get; init; } = "Nothing to create with the current settings.";

    // Navigation Tabs
    public string TabSettings { get; init; } = "Settings";
    public string TabGeometry { get; init; } = "Geometry";
    public string TabStirrups { get; init; } = "Stirrups";
    public string TabMainBars { get; init; } = "Main Bars";
    public string TabAddTopBars { get; init; } = "Top Add Bars";
    public string TabAddBottomBars { get; init; } = "Bottom Add Bars";
    public string TabSideBars { get; init; } = "Side Skin Bars";
    public string TabSpecialBars { get; init; } = "Secondary Ties";

    // Shared Field Labels
    public string Span { get; init; } = "Span";
    public string Spans { get; init; } = "Spans";
    public string Support { get; init; } = "Support";
    public string Supports { get; init; } = "Supports";
    public string BarType { get; init; } = "Bar Type";
    public string Diameter { get; init; } = "Diameter";
    public string Spacing { get; init; } = "Spacing";
    public string Count { get; init; } = "Count";
    public string Layer { get; init; } = "Layer";
    public string Cover { get; init; } = "Cover";
    public string Width { get; init; } = "Width (b)";
    public string Height { get; init; } = "Height (h)";
    public string Length { get; init; } = "Length (L)";
    public string ClearSpan { get; init; } = "Clear Span (Ln)";
    public string HookLength { get; init; } = "Hook Length";
    public string LapLength { get; init; } = "Lap Length";
    public string Partition { get; init; } = "Partition";
    public string ApplyAll { get; init; } = "Apply to All";

    // Settings Tab
    public string ViewGeneration { get; init; } = "Automated Views";
    public string CreateElevationView { get; init; } = "Create Longitudinal Elevation Detail";
    public string CreateSectionViews { get; init; } = "Create Cross-Section Views";
    public string CreateDimensions { get; init; } = "Create Section & Span Dimensions";
    public string CreateTags { get; init; } = "Create Rebar Tags & Schedule Tables";
    public string ElevationViewName { get; init; } = "Detail View Name";
    public string SectionViewPrefix { get; init; } = "Section View Prefix";

    // Geometry Tab
    public string BeamStackSummary { get; init; } = "Continuous Beam Stack";
    public string SpanIndex { get; init; } = "Span No";
    public string DimensionsMm { get; init; } = "Dimensions (mm)";
    public string Level { get; init; } = "Reference Level";
    public string TopElevation { get; init; } = "Top Elevation";
    public string Cantilever { get; init; } = "Cantilever Overhang";

    // Stirrups Tab
    public string StirrupLayout { get; init; } = "Distribution Layout";
    public string LayoutUniform { get; init; } = "Uniform Spacing Throughout";
    public string Layout3ZoneL4 { get; init; } = "3-Zone: Dense L/4, Midspan L/2";
    public string Layout3ZoneL3 { get; init; } = "3-Zone: Dense L/3, Midspan L/3";
    public string DenseSpacing { get; init; } = "Support Spacing (s1)";
    public string MidspanSpacing { get; init; } = "Midspan Spacing (s2)";
    public string StartOffset { get; init; } = "First Stirrup Offset (c0)";

    // Main Bars Tab
    public string MainTopBars { get; init; } = "Top Longitudinal Bars";
    public string MainBottomBars { get; init; } = "Bottom Longitudinal Bars";
    public string ContinuousBarCount { get; init; } = "Continuous Bar Count";
    public string AnchorageHook { get; init; } = "End Anchorage 90° Hook";
    public string StaggeredSplice { get; init; } = "50% Staggered Lap Splice";
    public string SpliceLength { get; init; } = "Lap Splice Length";

    // Additional Top Bars Tab
    public string AdditionalTopHeader { get; init; } = "Top Negative Bars Over Supports";
    public string SupportNode { get; init; } = "Support Node";
    public string ExtensionRule { get; init; } = "Cutoff Rule";
    public string RuleL3 { get; init; } = "L/3 of Adjacent Clear Span";
    public string RuleL4 { get; init; } = "L/4 of Adjacent Clear Span";

    // Additional Bottom Bars Tab
    public string AdditionalBottomHeader { get; init; } = "Bottom Positive Bars in Midspan";
    public string MidspanOffsetRule { get; init; } = "Start Offset from Support Face";
    public string RuleL7 { get; init; } = "Ln/7 from Support Face";
    public string RuleL8 { get; init; } = "Ln/8 from Support Face";

    // Side Bars Tab
    public string SideBarsHeader { get; init; } = "Side Skin Reinforcement (Web Bars)";
    public string EnableSideBars { get; init; } = "Enable Skin Reinforcement";
    public string AutoDeepBeamRule { get; init; } = "Auto for Beams h >= 700 mm";
    public string MaxVerticalSpacing { get; init; } = "Max Vertical Spacing (<= 300 mm)";
    public string CrossTies { get; init; } = "Transverse Anti-Buckling Cross-Ties";

    // Special Bars Tab
    public string SecondaryFramingHeader { get; init; } = "Secondary Beam Joint Reinforcement";
    public string HangingStirrups { get; init; } = "Hanging Stirrups Cage";
    public string DiagonalTies { get; init; } = "45° Diagonal Ties";
    public string TieCount { get; init; } = "Ties Count";
}
