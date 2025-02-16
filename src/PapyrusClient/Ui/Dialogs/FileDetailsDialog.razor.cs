using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Models;

namespace PapyrusClient.Ui.Dialogs;

public partial class FileDetailsDialog : ComponentBase
{
    [CascadingParameter] public FluentDialog Dialog { get; set; } = null!;

    [Parameter] public WorkScheduleFile Content { get; set; } = null!;

    private string? ActiveId { get; set; } = "tab-1";

    private string GetMessage()
    {
        return Content.Status.State switch
        {
            WorkScheduleFileState.Ok => "File has been processed successfully.",
            WorkScheduleFileState.ReadError => "File could not be read.",
            WorkScheduleFileState.ValidateError => "File could not be validated.",
            WorkScheduleFileState.GeneralError => "File could not be processed.",
            _ => "File could not be processed."
        };
    }

    private async Task CloseAsync()
    {
        await Dialog.CloseAsync();
    }
}