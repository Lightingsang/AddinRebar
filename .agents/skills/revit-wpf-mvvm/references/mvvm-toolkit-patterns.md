# MVVM Toolkit Patterns — CommunityToolkit.Mvvm 8.4+

Full catalog các attribute và pattern dùng trong Revit Add-In.

## 1. `[ObservableProperty]`

```csharp
[ObservableProperty]
private string _userName = string.Empty;
```

→ Sinh ra:
- Public property `UserName` (PascalCase từ `_userName`).
- Auto-raise `PropertyChanged`.
- Partial method hooks: `OnUserNameChanging(string oldValue, string newValue)`, `OnUserNameChanged(string value)`.

**Custom hook:**

```csharp
partial void OnUserNameChanged(string value)
{
    _logger.LogInformation("User name changed to {Name}", value);
}
```

## 2. `[NotifyCanExecuteChangedFor(nameof(...))]`

Khi property đổi → command `CanExecute` re-evaluate.

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private bool _hasChanges;

[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }

private bool CanSave() => HasChanges;
```

## 3. `[NotifyPropertyChangedFor(nameof(...))]`

Khi prop A đổi → trigger PropertyChanged cho prop B (computed property).

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName = string.Empty;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _lastName = string.Empty;

public string FullName => $"{FirstName} {LastName}";
```

## 4. `[RelayCommand]`

### Sync command

```csharp
[RelayCommand]
private void Reset() => SearchText = string.Empty;
```

### Async command với CancellationToken

```csharp
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    var data = await _service.FetchAsync(token);
    Items.Clear();
    foreach (var item in data) Items.Add(item);
}
```

### Command with parameter

```csharp
[RelayCommand]
private void Delete(WallInfo wall) => Walls.Remove(wall);
```

XAML:
```xml
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding}"/>
```

### Command với CanExecute

```csharp
[RelayCommand(CanExecute = nameof(CanDelete))]
private void Delete(WallInfo wall) => Walls.Remove(wall);

private bool CanDelete(WallInfo wall) => wall is not null && !wall.IsLocked;
```

### Concurrency control

```csharp
[RelayCommand(IncludeCancelCommand = true,           // Tạo thêm LoadDataCancelCommand
              AllowConcurrentExecutions = false)]    // Block click trùng
private async Task LoadDataAsync(CancellationToken token)
{
    // ...
}
```

## 5. `[ObservableValidator]` — Validation

```csharp
public sealed partial class UserViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name bắt buộc")]
    [MinLength(2, ErrorMessage = "Name ≥ 2 ký tự")]
    private string _name = string.Empty;

    [RelayCommand]
    private void Submit()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        // ...
    }
}
```

XAML hiển thị error qua `Validation.HasError`:

```xml
<TextBox Text="{Binding Name, ValidatesOnNotifyDataErrors=True}"/>
```

## 6. Messenger — Communication giữa ViewModels

```csharp
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

// Message
public sealed record WallSelectedMessage(ElementId WallId);

// Sender
WeakReferenceMessenger.Default.Send(new WallSelectedMessage(wall.Id));

// Receiver
public sealed partial class DetailsViewModel : ObservableObject,
    IRecipient<WallSelectedMessage>
{
    public DetailsViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(WallSelectedMessage message)
    {
        LoadDetails(message.WallId);
    }
}
```

**Quy tắc:**
- Dùng `WeakReferenceMessenger` (default) — auto-cleanup khi receiver bị GC.
- Message là `sealed record` — immutable.
- Tên message: `<Subject><PastTense>Message` (`WallSelectedMessage`, `ReportGeneratedMessage`).
- Register trong constructor, KHÔNG unregister thủ công (weak reference tự lo).

## 7. Async-safe patterns

### IsBusy guard

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(RunCommand))]
private bool _isBusy;

[RelayCommand(CanExecute = nameof(CanRun))]
private async Task RunAsync(CancellationToken token)
{
    IsBusy = true;
    try { /* work */ }
    finally { IsBusy = false; }
}

private bool CanRun() => !IsBusy;
```

### Avoid sync-over-async

❌ `var result = MyAsyncMethod().Result;` (deadlock WPF UI thread)
✅ `var result = await MyAsyncMethod();` trong `async Task` command

### ConfigureAwait

WPF có `SynchronizationContext` — **KHÔNG** dùng `.ConfigureAwait(false)` trong code chạm UI (mất context). Chỉ dùng trong service code thuần.

## 8. Disposable ViewModel

Nếu ViewModel subscribe event:

```csharp
public sealed partial class LiveViewModel : ObservableObject, IDisposable
{
    private readonly IRevitEventBus _bus;
    private bool _disposed;

    public LiveViewModel(IRevitEventBus bus)
    {
        _bus = bus;
        _bus.WallChanged += OnWallChanged;
    }

    private void OnWallChanged(object? sender, WallEventArgs e) { /* ... */ }

    public void Dispose()
    {
        if (_disposed) return;
        _bus.WallChanged -= OnWallChanged;
        _disposed = true;
    }
}
```

Khi window đóng → handle `Window.Closed` → call `(DataContext as IDisposable)?.Dispose()`.
