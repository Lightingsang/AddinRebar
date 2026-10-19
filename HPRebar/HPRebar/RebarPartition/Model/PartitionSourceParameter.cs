namespace HPRebar.RebarPartition.Model;

/// <summary>
/// Source parameter on the host element from which to derive the rebar partition name.
/// </summary>
public enum PartitionSourceParameter
{
    /// <summary>Custom or shared parameter 'Element Name' (as in original Dynamo script).</summary>
    ElementName,

    /// <summary>Standard Revit instance parameter Mark (ALL_MODEL_MARK).</summary>
    Mark,

    /// <summary>Symbol or Family Type Name of the host element.</summary>
    TypeName,

    /// <summary>Standard Comments parameter (ALL_MODEL_INSTANCE_COMMENTS).</summary>
    Comments,

    /// <summary>Host Level Name + Mark (e.g. L1_C1).</summary>
    LevelAndMark,

    /// <summary>User-specified parameter name.</summary>
    Custom
}
