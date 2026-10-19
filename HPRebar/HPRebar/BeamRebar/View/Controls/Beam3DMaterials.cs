using System.Windows.Media;
using System.Windows.Media.Media3D;
using HPRebar.Core.BeamRebar.Models;

// Alias WPF types against Revit SDK implicit usings
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Material = System.Windows.Media.Media3D.Material;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// Pre-cached, frozen WPF <see cref="Material"/> instances for 3D beam concrete and reinforcement rendering.
/// All materials and brushes are immutable and frozen for optimal DirectX performance.
/// </summary>
public static class Beam3DMaterials
{
    // Concrete Colors
    private static readonly Color ConcreteColor = Color.FromArgb(0x59, 0xB0, 0xC4, 0xDE); // LightSteelBlue alpha ~35%
    private static readonly Color SupportColor = Color.FromArgb(0x80, 0x9E, 0x9E, 0x9E);  // Semi-opaque Grey alpha ~50%

    // Reinforcement Colors per TCVN / Project Standards
    private static readonly Color MainTopColor = Color.FromRgb(0xE5, 0x48, 0x4D);        // Vivid Red / Crimson
    private static readonly Color MainBottomColor = Color.FromRgb(0x1E, 0x88, 0xE5);     // Vivid Blue / DodgerBlue
    private static readonly Color AddTopColor = Color.FromRgb(0xFF, 0x70, 0x43);         // Coral / Salmon
    private static readonly Color AddBottomColor = Color.FromRgb(0x29, 0xB6, 0xF6);      // Cyan / LightBlue
    private static readonly Color SideBarColor = Color.FromRgb(0x43, 0xA0, 0x47);        // Forest Green
    private static readonly Color CrossTieColor = Color.FromRgb(0x26, 0xA6, 0x9A);       // Teal
    private static readonly Color StirrupColor = Color.FromRgb(0xFF, 0xB3, 0x00);        // Amber / Gold
    private static readonly Color SpecialBarColor = Color.FromRgb(0xAB, 0x47, 0xBC);     // Purple / Orchid

    public static Material Concrete { get; } = CreateMaterial(ConcreteColor, specularPower: 40.0);
    public static Material Support { get; } = CreateMaterial(SupportColor, specularPower: 20.0);

    public static Material MainTopBar { get; } = CreateMaterial(MainTopColor, specularPower: 30.0);
    public static Material MainBottomBar { get; } = CreateMaterial(MainBottomColor, specularPower: 30.0);
    public static Material AdditionalTopBar { get; } = CreateMaterial(AddTopColor, specularPower: 30.0);
    public static Material AdditionalBottomBar { get; } = CreateMaterial(AddBottomColor, specularPower: 30.0);
    public static Material AdditionalBar { get; } = AdditionalTopBar;
    public static Material SideBar { get; } = CreateMaterial(SideBarColor, specularPower: 30.0);
    public static Material CrossTie { get; } = CreateMaterial(CrossTieColor, specularPower: 30.0);
    public static Material Stirrup { get; } = CreateMaterial(StirrupColor, specularPower: 30.0);
    public static Material SpecialBar { get; } = CreateMaterial(SpecialBarColor, specularPower: 30.0);

    /// <summary>
    /// Resolves the corresponding frozen material for a given structural rebar role.
    /// </summary>
    public static Material GetMaterialForBarType(BarType barType) => barType switch
    {
        BarType.MainTop => MainTopBar,
        BarType.MainBottom => MainBottomBar,
        BarType.AdditionalTop => AdditionalTopBar,
        BarType.AdditionalBottom => AdditionalBottomBar,
        BarType.SideSkin => SideBar,
        BarType.CrossTie => CrossTie,
        BarType.HangingStirrup => SpecialBar,
        BarType.DiagonalTie => SpecialBar,
        _ => MainTopBar
    };

    private static Material CreateMaterial(Color color, double specularPower)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(brush));

        if (specularPower > 0)
        {
            var specularBrush = new SolidColorBrush(Colors.White);
            specularBrush.Freeze();
            group.Children.Add(new SpecularMaterial(specularBrush, specularPower));
        }

        group.Freeze();
        return group;
    }
}
