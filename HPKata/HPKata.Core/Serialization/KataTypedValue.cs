namespace HPKata.Core.Serialization
{
    /// <summary>
    /// Host-neutral representation of a DXF/XData typed value pair (DxfCode, Value).
    /// Maps directly to AutoCAD TypedValue without referencing host assemblies.
    /// </summary>
    public readonly record struct KataTypedValue(int DxfCode, object? Value)
    {
        public override string ToString() => $"[{DxfCode}: {Value}]";
    }
}
