namespace HPKata.Core.Shapes
{
    /// <summary>
    /// Metadata definition for a standard rebar shape in the Kata catalog.
    /// </summary>
    public class RebarShapeDefinition
    {
        public RebarShapeType ShapeType { get; set; }
        public string ShapeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RevitShapeEquivalent { get; set; } = string.Empty;
        public int ParameterCount { get; set; }
        public bool HasAngleParameter { get; set; }
        public string Description { get; set; } = string.Empty;

        public RebarShapeDefinition()
        {
        }

        public RebarShapeDefinition(
            RebarShapeType shapeType,
            string shapeCode,
            string name,
            string revitShapeEquivalent,
            int parameterCount,
            bool hasAngleParameter,
            string description)
        {
            ShapeType = shapeType;
            ShapeCode = shapeCode;
            Name = name;
            RevitShapeEquivalent = revitShapeEquivalent;
            ParameterCount = parameterCount;
            HasAngleParameter = hasAngleParameter;
            Description = description;
        }

        public override string ToString() => $"{ShapeCode}: {Name}";
    }
}
