using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Services.SettingsManager;

namespace PapyrusClient.Ui.Dialogs;

public partial class SettingsDialog : ComponentBase, IAsyncDisposable
{
    private volatile bool _disposed;

    /// <summary>
    /// Forces a re-render of the FluentSelect component when the culture changes. Used in the @key directive.
    /// </summary>
    private Guid _themeSelectKey = Guid.NewGuid();

    private SortedSet<DateOnly> _holidaysStore = null!;

    private CultureInfo _initialCulture = null!;

    private CultureInfo _selectedCulture = null!;

    private DesignThemeModes _initialTheme;

    private DesignThemeModes _selectedTheme;

    private IEnumerable<DateTime> _selectedDays = null!;

    [CascadingParameter] public FluentDialog Dialog { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SetParameters();

        SettingsManager.CultureChanged += OnCultureChanged;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.CultureChanged -= OnCultureChanged;

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        _themeSelectKey = Guid.NewGuid();
    }

    private void SetParameters()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        _holidaysStore = new SortedSet<DateOnly>(SettingsManager.Holidays);

        _initialCulture = SettingsManager.Culture;

        _selectedCulture = SettingsManager.Culture;

        _initialTheme = SettingsManager.Theme;

        _selectedTheme = SettingsManager.Theme;

        _selectedDays = _holidaysStore
            .Select(date => date.ToDateTime(TimeOnly.MinValue))
            .ToList();
    }


    private void SelectedDatesChangedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        _holidaysStore.Clear();

        foreach (var date in _selectedDays
                     .Select(DateOnly.FromDateTime))
        {
            _holidaysStore.Add(date);
        }
    }

    private async Task SelectedCultureChangedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        await SettingsManager.UpdateCultureAsync(_selectedCulture);
    }

    private async Task SelectedThemeChangedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        await SettingsManager.UpdateThemeAsync(_selectedTheme);
    }

    private void ResetHolidaysChanges()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        _holidaysStore = new SortedSet<DateOnly>(SettingsManager.Holidays);

        _selectedDays = _holidaysStore
            .Select(date => date.ToDateTime(TimeOnly.MinValue))
            .ToList();
    }

    private void ClearAllHolidays()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        _holidaysStore.Clear();

        _selectedDays = Array.Empty<DateTime>();
    }

    private void ClearSelected(DateOnly date)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        _holidaysStore.Remove(date);

        _selectedDays = _holidaysStore
            .Select(x => x.ToDateTime(TimeOnly.MinValue))
            .ToList();
    }

    private async Task RefreshSettingsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        await SettingsManager.LoadSettingsAsync();

        SetParameters();
    }

    private async Task SaveAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        if (!_holidaysStore.SetEquals(SettingsManager.Holidays))
        {
            await SettingsManager.UpdateHolidaysAsync(_holidaysStore);
        }

        await Dialog.CloseAsync();
    }

    private async Task DiscardAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsDialog));

        if (_initialCulture.Name != _selectedCulture.Name)
        {
            await SettingsManager.UpdateCultureAsync(_initialCulture);
        }

        if (_initialTheme != _selectedTheme)
        {
            await SettingsManager.UpdateThemeAsync(_initialTheme);
        }

        await Dialog.CancelAsync();
    }
}