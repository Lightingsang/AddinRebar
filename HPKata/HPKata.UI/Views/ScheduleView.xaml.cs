using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for ScheduleView.xaml
    /// </summary>
    public partial class ScheduleView : Window
    {
        public ScheduleViewModel ViewModel { get; }

        public ScheduleView(ScheduleViewModel viewModel)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            ViewModel = viewModel;
            DataContext = viewModel;
            ViewModel.RequestOpenCuttingStock += OnOpenCuttingStock;
        }

        public ScheduleView() : this(new ScheduleViewModel())
        {
        }

        private void OnOpenCuttingStock(System.Collections.Generic.IEnumerable<HPKata.Core.Models.RebarItem> items)
        {
            var cuttingVm = new CuttingStockViewModel();
            cuttingVm.LoadFromRebarItems(items);
            var cuttingView = new CuttingStockView(cuttingVm)
            {
                Owner = this
            };
            cuttingView.ShowDialog();
        }
    }
}
