namespace HPBim.Core.Models;

public enum GridDirection
{
    X = 0, // Fixed X coordinate, line extends along Y
    Y = 1  // Fixed Y coordinate, line extends along X
}

public enum OpeningKind
{
    Door = 0,    // Single leaf door
    Door2 = 1,   // Double leaf door
    Window = 2,  // Window
    Opening = 3  // Cased opening / wall cutout
}

public enum OpeningSwing
{
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

public enum MepCategory
{
    Sanitary = 0,
    Kitchen = 1,
    Furniture = 2,
    Electrical = 3,
    Mechanical = 4
}

public enum PipeSystemType
{
    WaterSupply = 0,
    SoilDrain = 1,
    WasteDrain = 2
}

public enum RebarShapeType
{
    Straight = 0,
    HookEnd = 1,
    HookBoth = 2,
    ClosedStirrup = 3,
    CapStirrup = 4,
    UStirrup = 5
}
