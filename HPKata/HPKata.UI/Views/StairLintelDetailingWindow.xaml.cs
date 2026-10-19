using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    public partial class StairLintelDetailingWindow : Window
    {
        public StairLintelDetailingWindow() : this(new StairLintelDetailingViewModel())
        {
        }

        public StairLintelDetailingWindow(StairLintelDetailingViewModel vm)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            DataContext = vm;
            vm.RequestClose += () => Close();
        }
    }
}
