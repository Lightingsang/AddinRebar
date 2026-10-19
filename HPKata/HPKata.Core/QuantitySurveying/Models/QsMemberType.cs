namespace HPKata.Core.QuantitySurveying.Models
{
    /// <summary>
    /// Structural member classification for Quantity Surveying (QS) takeoff and deduction hierarchy.
    /// Hierarchy order: Footing > Column > Wall > Beam > Slab > Stair.
    /// </summary>
    public enum QsMemberType
    {
        Footing = 0,
        Column = 1,
        Wall = 2,
        Beam = 3,
        Slab = 4,
        Stair = 5
    }
}
