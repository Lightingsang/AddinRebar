using CommunityToolkit.Mvvm.ComponentModel;
using HPKata.Core.Beams.Etabs;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// Observable ViewModel for an imported ETABS beam row.
    /// </summary>
    public sealed partial class EtabsBeamRowViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isSelected = true;
        [ObservableProperty] private string _storyName = string.Empty;
        [ObservableProperty] private string _runName = string.Empty;
        [ObservableProperty] private string _dimensions = "300x600";
        [ObservableProperty] private double _lengthM = 5.0;
        [ObservableProperty] private string _topRebarText = string.Empty;
        [ObservableProperty] private string _botRebarText = string.Empty;
        [ObservableProperty] private string _stirrupText = "Ø8a100/a200";
        [ObservableProperty] private string _statusText = "Đạt";
        [ObservableProperty] private bool _isCompliant = true;

        public EtabsFrameDesign? FrameDesign { get; set; }
    }
}
