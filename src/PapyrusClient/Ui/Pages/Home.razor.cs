using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Ui.Dialogs;
using PapyrusClient.Ui.Panels;
using ResourceKey = PapyrusClient.Resources.Ui.Pages.Home;

namespace PapyrusClient.Ui.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource _cancellationTokenSource = null!;

    private IEnumerable<WorkSchedule> _selectedWorkSchedules = null!;

    private IDialogReference? _dialog = null;

    private volatile bool _processing;

    private volatile bool _disposed;

    private IAsyncDisposable _onCultureChangedSubscription = null!;

    #region Lifecycle overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _cancellationTokenSource = new CancellationTokenSource();

        _selectedWorkSchedules = Array.Empty<WorkSchedule>();

        _dialog = null;

        _processing = false;

        _onCultureChangedSubscription = ClientManager
            .OnCultureChanged((_) => InvokeAsync(StateHasChanged));
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

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        _selectedWorkSchedules = Array.Empty<WorkSchedule>();
        _dialog = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    [Inject]
    public IToastService ToastService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ILogger<Home> Logger { get; set; } = null!;

    [Inject]
    public IClientManager ClientManager { get; set; } = null!;

    [Inject]
    public IWorkScheduleStore WorkScheduleStore { get; set; } = null!;

    [Inject]
    public ITimeSheetWriter TimeSheetWriter { get; set; } = null!;

    #endregion

    private async Task ProcessSelectedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_processing || _dialog != null)
        {
            return;
        }

        _processing = true;

        string? zipFilePath = null;
        try
        {
            if (!_selectedWorkSchedules.Any())
            {
                ToastService.ShowWarning(Localizer[ResourceKey.TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_PROCESS]);
                return;
            }

            if (_selectedWorkSchedules.Any(schedule => schedule.State != WorkScheduleState.Ok))
            {
                ToastService.ShowWarning(Localizer[ResourceKey.TOAST_TEXT_INVALID_SCHEDULES_SELECTED_TO_PROCESS]);
                return;
            }

            zipFilePath = Path.GetTempFileName();

            var cancellationToken = _cancellationTokenSource.Token;

            var selectedWorkSchedules = _selectedWorkSchedules
                .Where(schedule => schedule.State == WorkScheduleState.Ok);

            var holidays = ClientManager.Holidays;

            await using var zipFileStream = File.Open(zipFilePath, FileMode.Open, FileAccess.ReadWrite);

            var zipFileName = await TimeSheetWriter
                .WriteAsZipAsync(selectedWorkSchedules, holidays, zipFileStream, true, cancellationToken);

            await ClientManager.DownloadFileAsync(zipFileName, zipFileStream, true, cancellationToken);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(Localizer[ResourceKey.TOAST_TEXT_ERROR_DURING_PROCESSING_SCHEDULES]);
            Logger.LogError(ex, "Error occurred while processing work schedules");
        }
        finally
        {
            if (zipFilePath != null && File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            _processing = false;
        }
    }

    private async Task RemoveSelectedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_processing || _dialog != null)
        {
            return;
        }

        _processing = true;

        try
        {
            if (!_selectedWorkSchedules.Any())
            {
                ToastService.ShowWarning(Localizer[ResourceKey.TOAST_TEXT_NO_SCHEDULES_SELECTED_TO_REMOVE]);
                return;
            }

            var cancellationToken = _cancellationTokenSource.Token;

            var selectedWorkSchedules = _selectedWorkSchedules;

            await WorkScheduleStore.RemoveBulkAsync(selectedWorkSchedules, cancellationToken);

            _selectedWorkSchedules = Array.Empty<WorkSchedule>();
        }
        finally
        {
            _processing = false;
        }
    }

    private async Task OpenWorkScheduleViewerDialogAsync(WorkSchedule workSchedule)
    {
        if (_processing || _dialog != null)
        {
            return;
        }

        try
        {
            _dialog = await DialogService.ShowDialogAsync<WorkScheduleViewerDialog>(workSchedule,
                new DialogParameters<WorkSchedule>
                {
                    Content = workSchedule,
                    Width = "95%",
                    Height = "95%",
                    PreventDismissOnOverlayClick = true,
                    PreventScroll = true,
                    OnDialogOpened = EventCallback.Factory.Create<DialogInstance>(this, StateHasChanged)
                });

            await _dialog.Result;
        }
        finally
        {
            _dialog = null;
        }
    }

    private async Task OpenDashboardPanelAsync()
    {
        if (_processing || _dialog != null)
        {
            return;
        }

        try
        {
            _dialog = await DialogService.ShowPanelAsync<DashboardPanel>(new DialogParameters
            {
                Alignment = HorizontalAlignment.Left,
                PreventDismissOnOverlayClick = false,
                PreventScroll = true,
                Width = "310px",
                OnDialogOpened = EventCallback.Factory.Create<DialogInstance>(this, StateHasChanged)
            });

            await _dialog.Result;
        }
        finally
        {
            _dialog = null;
        }
    }
}