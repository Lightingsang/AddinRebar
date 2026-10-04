using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
/// The dowel style is typed as a number in the window's splice grid: 0 is one style and every other number the
/// other. These turn the number into the style.
/// </summary>
public static class DowelStyleNumbers
{
    public static TopDowelStyle ToTop(int number) =>
        number == 0 ? TopDowelStyle.BendIntoColumnAbove : TopDowelStyle.StopUnderBeam;

    public static BottomDowelStyle ToBottom(int number) =>
        number == 0 ? BottomDowelStyle.StartAboveBase : BottomDowelStyle.RunPastBase;
}
