using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    public partial class SlabDetailingWindow : Window
    {
        public SlabDetailingWindow() : this(new SlabDetailingViewModel())
        {
        }

        public SlabDetailingWindow(SlabDetailingViewModel vm)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            DataContext = vm;
            vm.RequestClose += () => Close();
        }
    }
}
