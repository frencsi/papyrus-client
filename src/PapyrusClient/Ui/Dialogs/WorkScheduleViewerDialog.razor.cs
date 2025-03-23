using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Ui.Dialogs;

public partial class WorkScheduleViewerDialog : ComponentBase, IDialogContentComponent<WorkSchedule>
{
    private string _activeTab = null!;

    #region Lifecycle overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _activeTab = "status";
    }

    #endregion

    #region Properties

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = null!;

    [Parameter]
    public WorkSchedule Content { get; set; } = null!;

    #endregion

    private async Task CloseAsync()
    {
        await Dialog.CloseAsync();
    }
}