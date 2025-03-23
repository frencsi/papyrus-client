using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Ui.Panels;
using ResourceKey = PapyrusClient.Resources.Ui.Pages.Load;

namespace PapyrusClient.Ui.Pages;

public partial class Load : ComponentBase, IAsyncDisposable
{
    private List<WorkSchedule> _tempWorkSchedulesStore = null!;

    private IDialogReference? _dialog = null!;

    private CancellationTokenSource _cancellationTokenSource = null!;

    private int _maximumFileCountPerLoad;

    private int _loadProgressPercent;

    private string _loadProgressFileName = null!;

    private volatile bool _processing;

    private volatile bool _disposed;

    private IAsyncDisposable _onCultureChangedSubscription = null!;

    #region Lifecycle overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _maximumFileCountPerLoad = 256;

        _tempWorkSchedulesStore = new List<WorkSchedule>(_maximumFileCountPerLoad + 1);

        _dialog = null;

        _cancellationTokenSource = new CancellationTokenSource();

        _loadProgressPercent = 0;

        _loadProgressFileName = string.Empty;

        _processing = false;

        _disposed = false;

        _onCultureChangedSubscription = ClientManager
            .OnCultureChanged(_ => InvokeAsync(StateHasChanged));
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

        _tempWorkSchedulesStore.Clear();

        _dialog = null;
        _processing = false;
        _loadProgressPercent = 0;
        _loadProgressFileName = string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    [Inject]
    public IMessageService MessageService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public TimeProvider TimeProvider { get; set; } = null!;

    [Inject]
    public IClientManager ClientManager { get; set; } = null!;

    [Inject]
    public IWorkScheduleReader WorkScheduleReader { get; set; } = null!;

    [Inject]
    public IWorkScheduleStore WorkScheduleStore { get; set; } = null!;

    [Inject]
    public IWorkScheduleValidator WorkScheduleValidator { get; set; } = null!;

    #endregion

    private async Task OnFileLoadedAsync(FluentInputFileEventArgs args)
    {
        Debug.Assert(args.Stream != null, "Stream should not be null");

        _processing = true;
        _dialog = null;

        _loadProgressPercent = args.ProgressPercent;
        _loadProgressFileName = args.Name;

        var cancellationToken = _cancellationTokenSource.Token;

        var fileName = args.Name;
        var fileStream = args.Stream;

        WorkSchedule? workSchedule = null;

        try
        {
            workSchedule = await WorkScheduleReader
                .ReadAsync(fileName, fileStream, WorkScheduleOptions.Default, cancellationToken);

            await WorkScheduleValidator.ValidateAsync(workSchedule, cancellationToken);
        }
        catch (Exception ex)
        {
            if (workSchedule == null)
            {
                workSchedule = new WorkSchedule(
                    Company: Company.Empty,
                    Location: Location.Empty,
                    YearMonth: YearMonth.Default,
                    Type: WorkScheduleType.Unknown,
                    Shifts: Array.Empty<WorkShift>(),
                    Metadata: new WorkScheduleMetadata(fileName, WorkScheduleSource.File, TimeProvider.GetLocalNow()),
                    Rule: null,
                    Exception: ex);
            }
            else
            {
                workSchedule = workSchedule with { Exception = ex };
            }
        }
        finally
        {
            await fileStream.DisposeAsync();
        }

        _tempWorkSchedulesStore.Add(workSchedule);
    }

    private async Task OnLoadCompletedAsync()
    {
        try
        {
            var cancellationToken = _cancellationTokenSource.Token;

            await WorkScheduleStore.AddBulkAsync(_tempWorkSchedulesStore, cancellationToken);

            var allFiles = _tempWorkSchedulesStore.Count;
            var problemFiles = _tempWorkSchedulesStore.Count(schedule => schedule.State != WorkScheduleState.Ok);

            if (problemFiles != 0)
            {
                await MessageService.ShowMessageBarAsync(
                    title: Localizer.GetString(nameof(ResourceKey.MESSAGEBAR_BODY_LOADED_SCHEDULES_WITH_ISSUES),
                        allFiles, problemFiles),
                    intent: MessageIntent.Warning,
                    section: nameof(Load));
            }
            else
            {
                await MessageService.ShowMessageBarAsync(
                    title: Localizer.GetString(nameof(ResourceKey.MESSAGEBAR_BODY_LOADED_SCHEDULES_NO_ISSUES),
                        allFiles),
                    intent: MessageIntent.Success,
                    section: nameof(Load));
            }
        }
        finally
        {
            _tempWorkSchedulesStore.Clear();
            _loadProgressPercent = 0;
            _loadProgressFileName = string.Empty;
            _processing = false;
        }
    }

    private async Task OnFileCountExceededAsync(int count)
    {
        await MessageService.ShowMessageBarAsync(
            title: Localizer.GetString(nameof(ResourceKey.MESSAGEBAR_BODY_MAX_LOAD_PER_BATCH_REACHED),
                _maximumFileCountPerLoad),
            intent: MessageIntent.Warning,
            section: nameof(Load));
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