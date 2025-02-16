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
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        StateHasChanged();
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.ThemeChanged -= OnThemeChanged;

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}