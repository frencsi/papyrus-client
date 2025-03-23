using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Ui.Panels;

public partial class DashboardPanel : ComponentBase, IDialogContentComponent, IAsyncDisposable
{
    private Guid _selectedCultureId;
    private Guid _selectedThemeId;

    private CultureInfo _selectedCulture = null!;
    private DesignThemeModes _selectedTheme;

    private Holidays _initialHolidays = null!;
    private SortedSet<DateOnly> _holidaysStore = null!;
    private IEnumerable<DateTime> _selectedHolidays = null!;

    private IAsyncDisposable _onCultureChangedSubscription = null!;
    private IAsyncDisposable _onThemeChangedSubscription = null!;

    private volatile bool _disposed;

    #region Lifecycle overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _selectedCultureId = Guid.CreateVersion7();
        _selectedThemeId = Guid.CreateVersion7();

        _selectedCulture = ClientManager.Culture;
        _selectedTheme = ClientManager.Theme;

        _initialHolidays = ClientManager.Holidays;

        _holidaysStore = new SortedSet<DateOnly>(_initialHolidays.Dates);

        _selectedHolidays = _holidaysStore
            .Select(x => x.ToDateTime(TimeOnly.MinValue))
            .ToArray();

        _onCultureChangedSubscription = ClientManager.OnCultureChanged(args =>
        {
            _selectedCultureId = Guid.CreateVersion7();
            _selectedCulture = args.NewCulture;

            return InvokeAsync(StateHasChanged);
        });

        _onThemeChangedSubscription = ClientManager.OnThemeChanged(args =>
        {
            _selectedThemeId = Guid.CreateVersion7();
            _selectedTheme = args.NewTheme;

            return InvokeAsync(StateHasChanged);
        });
    }

    #endregion

    #region IAsyncDisposable implementation

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _onCultureChangedSubscription.DisposeAsync();
        await _onThemeChangedSubscription.DisposeAsync();

        if (!ClientManager.Holidays.Dates.SetEquals(_holidaysStore))
        {
            await ClientManager.ChangeHolidaysAsync(new Holidays(_holidaysStore));
        }

        _holidaysStore.Clear();
        _selectedHolidays = Array.Empty<DateTime>();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = null!;

    [Inject]
    public IStringLocalizer<DashboardPanel> Localizer { get; set; } = null!;

    [Inject]
    public IClientManager ClientManager { get; set; } = null!;

    #endregion

    private async Task SelectedCultureChangedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        await ClientManager.ChangeCultureAsync(_selectedCulture);
    }

    private async Task SelectedThemeChangedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        await ClientManager.ChangeThemeAsync(_selectedTheme);
    }

    private void SelectedHolidaysChanged()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        _holidaysStore.Clear();

        foreach (var date in _selectedHolidays.Select(DateOnly.FromDateTime))
        {
            _holidaysStore.Add(date);
        }
    }

    private void RemoveHoliday(DateOnly date)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        _holidaysStore.Remove(date);

        _selectedHolidays = _holidaysStore
            .Select(x => x.ToDateTime(TimeOnly.MinValue))
            .ToArray();
    }

    private void ResetHolidays()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        _holidaysStore.Clear();

        _holidaysStore.UnionWith(_initialHolidays.Dates);

        _selectedHolidays = _holidaysStore
            .Select(x => x.ToDateTime(TimeOnly.MinValue))
            .ToArray();
    }

    private void RemoveHolidays()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(DashboardPanel));

        _holidaysStore.Clear();

        _selectedHolidays = Array.Empty<DateTime>();
    }

    private async Task ClosePanelAsync()
    {
        await Dialog.CloseAsync();
    }
}