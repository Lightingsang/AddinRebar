namespace HPRebar.KataExport.ViewModel;

/// <summary>Column selection shared by the elevation and the table, and the span-by-span navigation.</summary>
public sealed partial class KataExportViewModel
{
    [ObservableProperty] private KataViewFocus _focusRequest = new(null, 0);
    [ObservableProperty] private string _selectionText = string.Empty;

    /// <summary>Kata column selected in the elevation and the table (0 = column C), -1 for none.</summary>
    [ObservableProperty] private int _selectedColumnIndex = -1;

    partial void OnSelectedColumnIndexChanged(int value) => SelectionText = KataPreviewBuilder.Describe(Elevation, value);

    [RelayCommand]
    private void PreviousSpan() => StepSpan(-1);

    [RelayCommand]
    private void NextSpan() => StepSpan(+1);

    [RelayCommand]
    private void FrameAll() => FocusRequest = new KataViewFocus(null, FocusRequest.Serial + 1);

    /// <summary>Selects the span before or after the selected column and frames it with its two supports.</summary>
    private void StepSpan(int direction)
    {
        if (Elevation is not { } elevation) return;

        int? current = SelectedColumnIndex >= 0 ? SelectedColumnIndex : null;
        if (elevation.SpanStep(current, direction) is not { } next) return;

        SelectedColumnIndex = next;
        FocusRequest = new KataViewFocus(next, FocusRequest.Serial + 1);
    }
}
