using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Models;
using PapyrusClient.Services.SettingsManager;

namespace PapyrusClient.Ui.Dialogs;

public partial class FileDetailsDialog : ComponentBase, IAsyncDisposable
{
    private enum ActiveTabType : byte
    {
        Status = 0,
        Options = 1,
        Details = 2
    }

    private volatile bool _disposed;

    private string _activeTab = GetActiveTabAsString(ActiveTabType.Status);

    [CascadingParameter] public FluentDialog Dialog { get; set; } = null!;

    [Parameter] public WorkScheduleFile Content { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitializedAsync();

        SettingsManager.CultureChanged += OnCultureChanged;

        SettingsManager.ThemeChanged += OnThemeChanged;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.CultureChanged -= OnCultureChanged;

        SettingsManager.ThemeChanged -= OnThemeChanged;

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        StateHasChanged();
    }

    private async Task CloseAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(FileDetailsDialog));

        await Dialog.CloseAsync();
    }

    private string GetMessage()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(FileDetailsDialog));

        return Content.Status.State switch
        {
            WorkScheduleFileState.Ok => "File has been processed successfully.",
            WorkScheduleFileState.ReadError => "File could not be read.",
            WorkScheduleFileState.ValidateError => "File could not be validated.",
            WorkScheduleFileState.GeneralError => "File could not be processed.",
            _ => "File could not be processed."
        };
    }

    private static string GetActiveTabAsString(ActiveTabType type)
    {
        return type switch
        {
            ActiveTabType.Status => nameof(ActiveTabType.Status),
            ActiveTabType.Options => nameof(ActiveTabType.Options),
            ActiveTabType.Details => nameof(ActiveTabType.Details),
            _ => "Unknown"
        };
    }
}