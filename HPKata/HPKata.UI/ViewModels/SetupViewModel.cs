using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Configuration;

namespace HPKata.UI.ViewModels
{
    /// <summary>
    /// ViewModel for Feature 12 (Setup Dialog), configuring the 86 KataPro parameters.
    /// </summary>
    public sealed partial class SetupViewModel : ObservableObject
    {
        private bool _isInitializing;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isDirty;

        public event Action? RequestClose;

        public SetupViewModel()
        {
            LoadFromConfig(KataConfigDefaults.CreateDefault());
        }

        public SetupViewModel(KataConfig config)
        {
            LoadFromConfig(config);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (!_isInitializing && e.PropertyName != nameof(IsDirty) && e.PropertyName != nameof(StatusMessage))
            {
                IsDirty = true;
            }
        }

        [RelayCommand]
        public void ResetToDefaults()
        {
            LoadFromConfig(KataConfigDefaults.CreateDefault());
            StatusMessage = "Reset to default configuration.";
            IsDirty = true;
        }

        [RelayCommand]
        public void LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                StatusMessage = "Config file not found.";
                return;
            }

            try
            {
                string text = File.ReadAllText(filePath);
                var config = KataConfigParser.ParseText(text);
                LoadFromConfig(config);
                StatusMessage = $"Loaded configuration from {Path.GetFileName(filePath)}.";
                IsDirty = false;
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: file I/O or parsing error logging
                StatusMessage = $"Error loading config: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "Invalid file path.";
                return;
            }

            try
            {
                var config = ToConfig();
                string text = KataConfigParser.SerializeToText(config);
                File.WriteAllText(filePath, text);
                StatusMessage = $"Saved configuration to {Path.GetFileName(filePath)}.";
                IsDirty = false;
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: file I/O error logging
                StatusMessage = $"Error saving config: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ApplyAndClose()
        {
            StatusMessage = "Configuration applied.";
            IsDirty = false;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        public void Cancel()
        {
            RequestClose?.Invoke();
        }
    }
}
