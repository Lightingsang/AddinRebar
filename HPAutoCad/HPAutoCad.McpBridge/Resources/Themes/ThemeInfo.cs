using System.Windows;

// The MaterialDesign toolkit is ILRepack-merged into this assembly by the RepackMaterialDesign target, so its Themes/Generic.xaml (default styles of
// PackIcon, Card, ColorZone, ...) becomes this assembly's generic dictionary. WPF only consults it when the assembly
// says where it lives; the Nice3point template declares nothing, and ILRepack keeps only the primary's attributes.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
