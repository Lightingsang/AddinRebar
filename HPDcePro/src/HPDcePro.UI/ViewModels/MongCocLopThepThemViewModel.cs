using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HPDcePro.UI.ViewModels
{
    public sealed class SecondaryRebarLayerItem
    {
        public string LayerName { get; set; } = "Lớp trên";
        public string Direction { get; set; } = "X";
        public int DiameterMm { get; set; } = 14;
        public double SpacingMm { get; set; } = 150.0;
        public string Description { get; set; } = "Lưới thép phân bố trên";
    }

    public sealed partial class MongCocLopThepThemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _capMark = "ĐC-1";

        [ObservableProperty]
        private bool _hasTopRebarMesh = true;

        [ObservableProperty]
        private int _topRebarDiameterMm = 14;

        [ObservableProperty]
        private double _topRebarSpacingMm = 150.0;

        [ObservableProperty]
        private bool _hasSideSkinRebar = true;

        [ObservableProperty]
        private int _sideSkinDiameterMm = 12;

        [ObservableProperty]
        private double _sideSkinSpacingMm = 200.0;

        [ObservableProperty]
        private bool _hasStirrupTiesX = true;

        [ObservableProperty]
        private bool _hasStirrupTiesY = true;

        [ObservableProperty]
        private int _pileStarterBarsCount = 4;

        [ObservableProperty]
        private int _pileStarterDiameterMm = 16;

        [ObservableProperty]
        private double _pileStarterLapLengthMm = 600.0;

        public ObservableCollection<SecondaryRebarLayerItem> Layers { get; } = new();

        public MongCocLopThepThemViewModel()
        {
            Layers.Add(new SecondaryRebarLayerItem { LayerName = "Thép trên X", Direction = "X", DiameterMm = 14, SpacingMm = 150 });
            Layers.Add(new SecondaryRebarLayerItem { LayerName = "Thép trên Y", Direction = "Y", DiameterMm = 14, SpacingMm = 150 });
            Layers.Add(new SecondaryRebarLayerItem { LayerName = "Thép sườn bên", Direction = "Chu vi", DiameterMm = 12, SpacingMm = 200 });
        }

        [RelayCommand]
        public void AddLayer()
        {
            Layers.Add(new SecondaryRebarLayerItem
            {
                LayerName = $"Thép phụ {Layers.Count + 1}",
                Direction = "X",
                DiameterMm = 12,
                SpacingMm = 200
            });
        }

        [RelayCommand]
        public void RemoveLayer(SecondaryRebarLayerItem? item)
        {
            if (item != null)
            {
                Layers.Remove(item);
            }
        }
    }
}
