using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    public partial class FoundationDetailingWindow : Window
    {
        public FoundationDetailingWindow() : this(new FoundationDetailingViewModel())
        {
        }

        public FoundationDetailingWindow(FoundationDetailingViewModel vm)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            DataContext = vm;
            vm.RequestClose += () => Close();
        }
    }
}
