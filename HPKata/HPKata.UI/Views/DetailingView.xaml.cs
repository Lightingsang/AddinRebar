using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for DetailingView.xaml
    /// </summary>
    public partial class DetailingView : Window
    {
        public DetailingViewModel ViewModel { get; }

        public DetailingView(DetailingViewModel viewModel)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            ViewModel = viewModel;
            DataContext = viewModel;
            ViewModel.RequestClose += OnRequestClose;
        }

        public DetailingView() : this(new DetailingViewModel())
        {
        }

        private void OnRequestClose()
        {
            DialogResult = ViewModel.ResultItem != null;
            Close();
        }
    }
}
