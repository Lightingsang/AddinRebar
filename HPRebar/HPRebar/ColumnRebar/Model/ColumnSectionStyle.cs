namespace HPRebar.ColumnRebar.Model;

/// <summary>What the solid of a picked column turned out to be.</summary>
public enum ColumnSectionStyle
{
    /// <summary>Anything the tool cannot rebar: chamfered, L-shaped, multi-solid and so on.</summary>
    Other,
    Rectangle,
    Circular
}
