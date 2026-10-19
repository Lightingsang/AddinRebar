using System;
using System.Windows;
using System.Windows.Interop;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for BeamDetailingWindow.xaml
    /// </summary>
    public partial class BeamDetailingWindow : Window
    {
        public BeamDetailingViewModel? ViewModel { get; private set; }

        public BeamDetailingWindow()
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            DataContextChanged += OnDataContextChanged;
        }

        public BeamDetailingWindow(BeamDetailingViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is BeamDetailingViewModel oldVm)
            {
                oldVm.RequestClose -= OnRequestClose;
            }
            if (e.NewValue is BeamDetailingViewModel newVm)
            {
                ViewModel = newVm;
                newVm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose()
        {
            try
            {
                if (ComponentDispatcher.IsThreadModal)
                {
                    DialogResult = ViewModel?.IsConfirmed ?? false;
                }
            }
            catch (InvalidOperationException)
            {
                // Non-modal window
            }
            Close();
        }
    }
}
