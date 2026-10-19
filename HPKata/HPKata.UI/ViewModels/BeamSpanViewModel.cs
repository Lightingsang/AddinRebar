using CommunityToolkit.Mvvm.ComponentModel;
using HPKata.Core.Beams.Models;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// Observable ViewModel for a single span row in BeamDetailingView.
    /// </summary>
    public sealed partial class BeamSpanViewModel : ObservableObject
    {
        [ObservableProperty] private int _spanIndex;
        [ObservableProperty] private string _spanName = string.Empty;
        [ObservableProperty] private double _clearLengthMm = 5000.0;
        [ObservableProperty] private double _widthMm = 300.0;
        [ObservableProperty] private double _heightMm = 600.0;
        [ObservableProperty] private double _leftSupportWidthMm = 400.0;
        [ObservableProperty] private double _rightSupportWidthMm = 400.0;
        [ObservableProperty] private bool _isCantileverLeft;
        [ObservableProperty] private bool _isCantileverRight;

        public BeamSpanViewModel() { }

        public BeamSpanViewModel(BeamSpanModel model)
        {
            SpanIndex = model.SpanIndex;
            SpanName = model.SpanName;
            ClearLengthMm = model.ClearLengthMm;
            WidthMm = model.WidthMm;
            HeightMm = model.HeightMm;
            LeftSupportWidthMm = model.LeftSupportWidthMm;
            RightSupportWidthMm = model.RightSupportWidthMm;
            IsCantileverLeft = model.IsCantileverLeft;
            IsCantileverRight = model.IsCantileverRight;
        }

        public BeamSpanModel ToModel()
        {
            return new BeamSpanModel
            {
                SpanIndex = SpanIndex,
                SpanName = SpanName,
                ClearLengthMm = ClearLengthMm,
                WidthMm = WidthMm,
                HeightMm = HeightMm,
                LeftSupportWidthMm = LeftSupportWidthMm,
                RightSupportWidthMm = RightSupportWidthMm,
                IsCantileverLeft = IsCantileverLeft,
                IsCantileverRight = IsCantileverRight
            };
        }
    }
}
