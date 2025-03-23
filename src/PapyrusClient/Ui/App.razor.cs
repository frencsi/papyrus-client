using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Ui;

public partial class App : ComponentBase, IAsyncDisposable
{
    public const string MessageBarTop = nameof(MessageBarTop);

    private volatile bool _disposed;

    private IAsyncDisposable _onVersionChangedSubscription = null!;
    private IAsyncDisposable _onThemeChangedSubscription = null!;
    private IAsyncDisposable _onCultureChangedSubscription = null!;

    #region Lifecycle overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _onVersionChangedSubscription = ClientManager
            .OnVersionChanged(_ => InvokeAsync(StateHasChanged));

        _onThemeChangedSubscription = ClientManager
            .OnThemeChanged(_ => InvokeAsync(StateHasChanged));

        _onCultureChangedSubscription = ClientManager
            .OnCultureChanged(_ =>
            {
                MessageService.Clear();
                ToastService.ClearAll();

                return InvokeAsync(StateHasChanged);
            });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        await ClientManager.LoadVersionAsync(reload: false);
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

        await _onVersionChangedSubscription.DisposeAsync();
        await _onThemeChangedSubscription.DisposeAsync();
        await _onCultureChangedSubscription.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    [Inject]
    public IClientManager ClientManager { get; set; } = null!;

    [Inject]
    public IMessageService MessageService { get; set; } = null!;

    [Inject]
    public IToastService ToastService { get; set; } = null!;

    #endregion
}