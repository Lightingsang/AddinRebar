using CommunityToolkit.Mvvm.ComponentModel;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel representing a single story tier row in the column detailing grid.
    /// </summary>
    public sealed partial class ColumnStoryRowViewModel : ObservableObject
    {
        [ObservableProperty] private int _storyIndex;
        [ObservableProperty] private string _storyName = "Tầng 1";
        [ObservableProperty] private double _storyHeightMm = 3600.0;
        [ObservableProperty] private double _beamDepthMm = 600.0;

        [ObservableProperty] private double _widthMm = 400.0;
        [ObservableProperty] private double _depthMm = 400.0;

        [ObservableProperty] private int _cornerDiameter = 20;
        [ObservableProperty] private int _sideCountB = 1;
        [ObservableProperty] private int _sideDiameterB = 18;
        [ObservableProperty] private int _sideCountH = 1;
        [ObservableProperty] private int _sideDiameterH = 18;

        [ObservableProperty] private double _denseSpacing = 100.0;
        [ObservableProperty] private double _sparseSpacing = 200.0;

        public double ClearHeightMm => System.Math.Max(0.0, StoryHeightMm - BeamDepthMm);
    }
}
