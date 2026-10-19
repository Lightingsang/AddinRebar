using CommunityToolkit.Mvvm.ComponentModel;
using HPKata.Core.Configuration;

namespace HPKata.UI.ViewModels
{
    public sealed partial class SetupViewModel
    {
        // Category 1: CAD Layers (Parameters 1-11)
        [ObservableProperty] private string _layerDim = "kata_dim";
        [ObservableProperty] private string _layerNetThay = "kata_net thay";
        [ObservableProperty] private string _layerNetKhung = "kata_net khung";
        [ObservableProperty] private string _layerNetKhuat = "kata_net khuat";
        [ObservableProperty] private string _layerGrid = "kata_grid";
        [ObservableProperty] private string _layerThepChu = "kata_thep chu";
        [ObservableProperty] private string _layerThepDai = "kata_thep dai";
        [ObservableProperty] private string _layerThepDaiPhu = "kata_thep dai";
        [ObservableProperty] private string _layerNetManh = "kata_net manh";
        [ObservableProperty] private string _layerText = "kata_text";
        [ObservableProperty] private string _layerTextTag = "kata_text";

        // Category 2: Dimension & Notation (Parameters 12-15, 29, 30, 42, 44, 64, 69, 70, 79)
        [ObservableProperty] private string _spacingPrefix = "a";
        [ObservableProperty] private string _smallBarPrefix = "Ø";
        [ObservableProperty] private string _deformedBarPrefix = "Ø";
        [ObservableProperty] private string _dimStyleName = "kata_dim_iso";
        [ObservableProperty] private int _dimStyleOverride;
        [ObservableProperty] private int _tagStyle;
        [ObservableProperty] private double _annotationTextH = 5.0;
        [ObservableProperty] private string _tagSeparator = "-";
        [ObservableProperty] private int _autoDimRebar;
        [ObservableProperty] private int _drawLeaderArrow = 1;
        [ObservableProperty] private int _leaderArrowType;
        [ObservableProperty] private double _tagCircleRadius = 10.0;

        // Category 3: Drawing & Scales (Parameters 18, 51, 62, 65, 85)
        [ObservableProperty] private double _detailDrawingScale = 0.04;
        [ObservableProperty] private string _languageUiSched = "Tiếng Việt";
        [ObservableProperty] private int _titleScaleMode;
        [ObservableProperty] private string _lengthUnitDisplay = "mm";
        [ObservableProperty] private int _realtimeSync;

        private void LoadDrawingLayers(KataConfig config)
        {
            LayerDim = config.LayerDim;
            LayerNetThay = config.LayerNetThay;
            LayerNetKhung = config.LayerNetKhung;
            LayerNetKhuat = config.LayerNetKhuat;
            LayerGrid = config.LayerGrid;
            LayerThepChu = config.LayerThepChu;
            LayerThepDai = config.LayerThepDai;
            LayerThepDaiPhu = config.LayerThepDaiPhu;
            LayerNetManh = config.LayerNetManh;
            LayerText = config.LayerText;
            LayerTextTag = config.LayerTextTag;

            SpacingPrefix = config.SpacingPrefix;
            SmallBarPrefix = config.SmallBarPrefix;
            DeformedBarPrefix = config.DeformedBarPrefix;
            DimStyleName = config.DimStyleName;
            DimStyleOverride = config.DimStyleOverride;
            TagStyle = config.TagStyle;
            AnnotationTextH = config.AnnotationTextH;
            TagSeparator = config.TagSeparator;
            AutoDimRebar = config.AutoDimRebar;
            DrawLeaderArrow = config.DrawLeaderArrow;
            LeaderArrowType = config.LeaderArrowType;
            TagCircleRadius = config.TagCircleRadius;

            DetailDrawingScale = config.DetailDrawingScale;
            LanguageUiSched = config.LanguageUiSched;
            TitleScaleMode = config.TitleScaleMode;
            LengthUnitDisplay = config.LengthUnitDisplay;
            RealtimeSync = config.RealtimeSync;
        }

        private void SaveDrawingLayers(KataConfig config)
        {
            config.LayerDim = LayerDim;
            config.LayerNetThay = LayerNetThay;
            config.LayerNetKhung = LayerNetKhung;
            config.LayerNetKhuat = LayerNetKhuat;
            config.LayerGrid = LayerGrid;
            config.LayerThepChu = LayerThepChu;
            config.LayerThepDai = LayerThepDai;
            config.LayerThepDaiPhu = LayerThepDaiPhu;
            config.LayerNetManh = LayerNetManh;
            config.LayerText = LayerText;
            config.LayerTextTag = LayerTextTag;

            config.SpacingPrefix = SpacingPrefix;
            config.SmallBarPrefix = SmallBarPrefix;
            config.DeformedBarPrefix = DeformedBarPrefix;
            config.DimStyleName = DimStyleName;
            config.DimStyleOverride = DimStyleOverride;
            config.TagStyle = TagStyle;
            config.AnnotationTextH = AnnotationTextH;
            config.TagSeparator = TagSeparator;
            config.AutoDimRebar = AutoDimRebar;
            config.DrawLeaderArrow = DrawLeaderArrow;
            config.LeaderArrowType = LeaderArrowType;
            config.TagCircleRadius = TagCircleRadius;

            config.DetailDrawingScale = DetailDrawingScale;
            config.LanguageUiSched = LanguageUiSched;
            config.TitleScaleMode = TitleScaleMode;
            config.LengthUnitDisplay = LengthUnitDisplay;
            config.RealtimeSync = RealtimeSync;
        }
    }
}
