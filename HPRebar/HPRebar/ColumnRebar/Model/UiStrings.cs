namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     Every label the Column Rebar window shows, in one language. Swapping the whole record at once is what
///     makes the language toggle instant: bindings go through the service that holds it, so one property
///     change refreshes the entire window.
/// </summary>
public sealed record UiStrings
{
    // Window chrome
    public string WindowTitle { get; init; } = "Column Rebar";
    public string Ok { get; init; } = "OK";
    public string Cancel { get; init; } = "Cancel";
    public string Column { get; init; } = "Columns";
    public string LanguageToggle { get; init; } = "VN";

    // Navigation
    public string TabSetting { get; init; } = "Setting";
    public string TabGeometry { get; init; } = "Geometry";
    public string TabStirrups { get; init; } = "Stirrups";
    public string TabAdditionalStirrups { get; init; } = "Additional Stirrups";
    public string TabBars { get; init; } = "Bars";
    public string TabTopDowels { get; init; } = "Top Dowels";
    public string TabBottomDowels { get; init; } = "Bottom Dowels";
    public string TabBarsDivision { get; init; } = "Bars Division";

    // Shared field labels
    public string NameBar { get; init; } = "Name Bar";
    public string LayerBar { get; init; } = "Layer";
    public string Bar { get; init; } = "Bar";
    public string Type { get; init; } = "Type";
    public string Distance { get; init; } = "Distance";
    public string NumberBar { get; init; } = "No";
    public string BarNumber { get; init; } = "Bar No";
    public string HookLength { get; init; } = "HookLength";
    public string HookType { get; init; } = "Hook Type";
    public string ColumnsNumber { get; init; } = "Column No";
    public string Apply { get; init; } = "Apply";
    public string Modify { get; init; } = "Modify";
    public string Thickness { get; init; } = "Thickness";
    public string Length { get; init; } = "Length";
    public string Cover { get; init; } = "Cover";
    public string Diameter { get; init; } = "Diameter";

    // Setting tab
    public string RebarShapeHook { get; init; } = "Rebar Shape and Hook";
    public string StirrupShape { get; init; } = "Stirrup Shape";
    public string AntiShape { get; init; } = "Anti Shape";
    public string ParameterColumns { get; init; } = "Parameter Columns";
    public string ColumnsName { get; init; } = "Columns Name";
    public string DetailViewName { get; init; } = "Detail View Name";
    public string SectionViewName { get; init; } = "Section View Name";
    public string PrefixLevel { get; init; } = "Prefix Level";
    public string PrefixSection { get; init; } = "Prefix Section";
    public string ReinforcementStructural { get; init; } = "Reinforcement Structural";
    public string UseRealRebar { get; init; } = "Model real rebar";
    public string UseRealRebarLocked { get; init; } = "Detail Item mode is not supported yet.";

    // Geometry tab
    public string Identification { get; init; } = "Identification";
    public string FamilyName { get; init; } = "Family Name";
    public string TypeName { get; init; } = "Type Name";
    public string Style { get; init; } = "Style";
    public string ColumnsDimention { get; init; } = "Columns Dimension";
    public string ColumnsProperty { get; init; } = "Columns Property";
    public string Width { get; init; } = "Width";
    public string Depth { get; init; } = "Depth";
    public string Height { get; init; } = "Height";
    public string BeamDepth { get; init; } = "Beam depth";
    public string BeamDrop { get; init; } = "Beam drop";

    // Stirrups tab
    public string StirrupsProperty { get; init; } = "Stirrups Property";
    public string ColumnsNo { get; init; } = "Columns No";
    public string ApplyAllColumns { get; init; } = "Apply All Columns";
    public string StirrupsParameter { get; init; } = "Stirrups Parameter";
    public string StirrupsDistribute { get; init; } = "Stirrups Distribute";
    public string TiesUpToBeams { get; init; } = "Ties Up To Beams";
    public string SpacingEven { get; init; } = "Spacing";
    public string SpacingDense { get; init; } = "Spacing (ends)";
    public string SpacingSparse { get; init; } = "Spacing (middle)";
    public string RunLength { get; init; } = "Run length";

    // Additional stirrups tab
    public string AdditionalProperty { get; init; } = "Additional Property";
    public string AdditionalHorizontal { get; init; } = "Additional Horizontal";
    public string AdditionalVertical { get; init; } = "Additional Vertical";
    public string Horizontal { get; init; } = "Horizontal";
    public string Vertical { get; init; } = "Vertical";
    public string LegLength { get; init; } = "Leg length";
    public string LegCount { get; init; } = "Leg count";

    // Bars tab
    public string BarsProperty { get; init; } = "Bars Property";
    public string SplitOverlap { get; init; } = "Split Overlap";
    public string Overlap { get; init; } = "Overlap";
    public string BarsInformation { get; init; } = "Bars Information";
    public string BarsAlongWidth { get; init; } = "Bars along width";
    public string BarsAlongDepth { get; init; } = "Bars along depth";
    public string BarsAround { get; init; } = "Bars around";
    public string BarCount { get; init; } = "Bar count";

    // Dowels tabs
    public string TopDowelsProperty { get; init; } = "Top Dowels Property";
    public string BottomDowelsProperty { get; init; } = "Bottom Dowels Property";
    public string ApplyAllBar { get; init; } = "Apply All Bar";
    public string Top { get; init; } = "Top";
    public string Bottom { get; init; } = "Bottom";
    public string TopDowels { get; init; } = "Top Dowels";
    public string BottomDowels { get; init; } = "Bottom Dowels";
    public string DowelsOn { get; init; } = "Dowels";
    public string DowelStyle { get; init; } = "Style";
    public string HookLengthLa { get; init; } = "Hook (La)";
    public string AnchorLengthLb { get; init; } = "Anchor (Lb)";
    public string StartHeightLc { get; init; } = "Start height (Lc)";

    // Bars division tab
    public string DivisionProperty { get; init; } = "Division Property";
    public string MainBarsDivision { get; init; } = "Main Bars";
    public string Shape { get; init; } = "Shape";
    public string CutLength { get; init; } = "Cut length";
    public string IdenticalColumns { get; init; } = "Identical columns";

    // Messages
    public string NothingToCreate { get; init; } = "Nothing to create with the current settings.";
    public string Working { get; init; } = "Creating reinforcement…";
}
