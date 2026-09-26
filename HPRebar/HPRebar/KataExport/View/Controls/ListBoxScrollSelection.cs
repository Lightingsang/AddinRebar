using System.Windows;
using System.Windows.Controls;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Scrolls a list box to its selected item whenever the selection changes, including a selection made in code
/// (a column clicked in the elevation) — WPF only does that for selections made in the list itself.
/// </summary>
public static class ListBoxScrollSelection
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ListBoxScrollSelection), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ListBox list) return;
        list.SelectionChanged -= OnSelectionChanged;
        if ((bool)args.NewValue) list.SelectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is ListBox { SelectedItem: { } item } list) list.ScrollIntoView(item);
    }
}
