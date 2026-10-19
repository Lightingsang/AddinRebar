using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for SetupView.xaml
    /// </summary>
    public partial class SetupView : Window
    {
        public SetupViewModel ViewModel { get; }

        public SetupView(SetupViewModel viewModel)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            ViewModel = viewModel;
            DataContext = viewModel;
            ViewModel.RequestClose += OnRequestClose;
        }

        public SetupView() : this(new SetupViewModel())
        {
        }

        private void OnRequestClose()
        {
            DialogResult = true;
            Close();
        }
    }
}
