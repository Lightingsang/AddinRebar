using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.ViewModel.Tabs;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport.ViewModel;

/// <summary>
/// The Kata settings dialog, three tabs as Kata's "Cài đặt thông số Kata": Detail thép, Thông số đặc thù, Thép mặc định.
/// Accept saves the file for every later run; the caller re-plans the loaded sheet.
/// </summary>
public sealed partial class KataSettingsViewModel : ObservableObject
{
    private readonly Action<bool> _close;

    [ObservableProperty] private string _message = string.Empty;

    /// <summary>The settings were accepted but the file could not be written (they still apply to this session).</summary>
    public bool SaveFailed { get; private set; }

    public KataDetailSettingsTabViewModel Detail { get; } = new();

    public KataSpecialSettingsTabViewModel Special { get; } = new();

    public KataJointDefaultsTabViewModel Joints { get; } = new();

    /// <param name="close">Called with true after the settings were saved, false on cancel.</param>
    public KataSettingsViewModel(KataSettingsFile initial, Action<bool> close)
    {
        _close = close ?? throw new ArgumentNullException(nameof(close));
        Import(initial ?? KataSettingsFile.Default);
    }

    /// <summary>The settings as the tabs show them; null with <see cref="Message"/> set when a tab holds a wrong value.</summary>
    public KataSettingsFile? Collect()
    {
        string? error = Detail.Validate() ?? Special.Validate() ?? Joints.Validate();
        if (error is not null)
        {
            Message = error;
            return null;
        }

        var drawing = Joints.ExportDrawing(Detail.ExportDrawing(KataSettings.Default));
        var shop = Special.ExportShop(Detail.ExportShop(KataShopSettings.Default));
        return new KataSettingsFile(drawing, Detail.ExportPending(), shop);
    }

    [RelayCommand]
    private void Accept()
    {
        if (Collect() is not { } file) return;
        SaveFailed = !KataSettingsStore.SaveFile(file);
        _close(true);
    }

    [RelayCommand]
    private void Cancel() => _close(false);

    [RelayCommand]
    private void ResetDefault() => Import(KataSettingsFile.Default);

    private void Import(KataSettingsFile file)
    {
        Detail.Import(file);
        Special.Import(file.Shop);
        Joints.Import(file.Drawing);
        Message = string.Empty;
    }
}
