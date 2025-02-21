using Microsoft.AspNetCore.Components;
using PapyrusClient.Services.SettingsManager;

namespace PapyrusClient.Ui;

public partial class App : ComponentBase, IAsyncDisposable
{
    private volatile bool _disposed;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SettingsManager.ThemeChanged += OnThemeChanged;

        SettingsManager.CultureChanged += OnCultureChanged;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.ThemeChanged -= OnThemeChanged;

        SettingsManager.CultureChanged -= OnCultureChanged;

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        // TODO: Toast notifications are not dynamically localized, revisit later.
        ToastService.ClearAll();

        StateHasChanged();
    }
}