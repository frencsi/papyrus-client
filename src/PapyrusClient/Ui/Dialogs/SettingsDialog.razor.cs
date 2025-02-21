using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Ui.Dialogs;

public partial class SettingsDialog : ComponentBase
{
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
    }

    private void SetParameters()
    {
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
        _holidaysStore.Clear();

        foreach (var date in _selectedDays
                     .Select(DateOnly.FromDateTime))
        {
            _holidaysStore.Add(date);
        }
    }

    private async Task SelectedCultureChangedAsync()
    {
        await SettingsManager.UpdateCultureAsync(_selectedCulture);
    }

    private async Task SelectedThemeChangedAsync()
    {
        await SettingsManager.UpdateThemeAsync(_selectedTheme);
    }

    private void ResetHolidaysChanges()
    {
        _holidaysStore = new SortedSet<DateOnly>(SettingsManager.Holidays);

        _selectedDays = _holidaysStore
            .Select(date => date.ToDateTime(TimeOnly.MinValue))
            .ToList();
    }

    private void ClearAllHolidays()
    {
        _holidaysStore.Clear();

        _selectedDays = Array.Empty<DateTime>();
    }

    private void ClearSelected(DateOnly date)
    {
        _holidaysStore.Remove(date);

        _selectedDays = _holidaysStore
            .Select(x => x.ToDateTime(TimeOnly.MinValue))
            .ToList();
    }

    private async Task RefreshSettingsAsync()
    {
        await SettingsManager.LoadSettingsAsync();

        SetParameters();
    }

    private async Task SaveAsync()
    {
        if (!_holidaysStore.SetEquals(SettingsManager.Holidays))
        {
            await SettingsManager.UpdateHolidaysAsync(_holidaysStore);
        }

        await Dialog.CloseAsync();
    }

    private async Task DiscardAsync()
    {
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